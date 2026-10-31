using System.Runtime.InteropServices;
using System.Text;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Brutal.VulkanApi.Vma;
using Statventure.Core;
using Statventure.Platform;

namespace Statventure.Graphics;

public sealed record GpuDeviceOptions
{
    public string ApplicationName { get; init; } = EngineInfo.Name;
    /// <summary>No window system: for tests and offline rendering.</summary>
    public bool Headless { get; init; }
    /// <summary>Use the Khronos validation layer when it is installed (or STATVENTURE_VALIDATION=1).</summary>
    public bool Validation { get; init; } = Environment.GetEnvironmentVariable("STATVENTURE_VALIDATION") == "1";
    public int FramesInFlight { get; init; } = 2;
}

/// <summary>
/// The GPU: a Vulkan 1.3 device with one universal queue, VMA memory, the bindless texture table, the
/// shader library and pipeline cache, frames in flight, and deferred destruction of resources the GPU
/// may still be using. Every submission signals one timeline semaphore; a value tells when work ended.
/// </summary>
public sealed unsafe partial class GpuDevice : IDisposable
{
    public const string Category = "Graphics";

    public Instance Instance { get; }
    public PhysicalDevice PhysicalDevice { get; }
    public Device Device { get; }
    public Queue Queue { get; }
    public int QueueFamily { get; }
    public VmaAllocator Allocator { get; }
    public string Name { get; }
    /// <summary>A CPU implementation of Vulkan (Mesa's lavapipe, SwiftShader): slower, and its float math rounds a little differently.</summary>
    public bool IsSoftware { get; }
    public VkPhysicalDeviceLimits Limits { get; }
    public GpuDeviceOptions Options { get; }
    public Bindless Bindless { get; }
    public ShaderLibrary Shaders { get; }
    public PipelineCache Pipelines { get; }
    /// <summary>Dual-source blending is available (needed for LCD subpixel text).</summary>
    public bool SupportsDualSourceBlend { get; }

    readonly VkSemaphore _timeline;
    ulong _submitted;
    readonly object _submitGate = new();
    readonly List<(ulong Value, Action Destroy)> _graveyard = [];
    // Releases made while commands are being recorded wait for that recording's submission.
    readonly Stack<List<Action>> _scopes = new();
    readonly VkCommandPool _immediatePool;
    readonly FrameSlot[] _slots;
    long _frameNumber;
    bool _disposed;

    GpuDevice(GpuDeviceOptions options, Instance instance, PhysicalDevice physicalDevice, Device device, int queueFamily, bool dualSourceBlend)
    {
        SupportsDualSourceBlend = dualSourceBlend;
        Options = options;
        Instance = instance;
        PhysicalDevice = physicalDevice;
        Device = device;
        QueueFamily = queueFamily;
        Queue = device.GetQueue(queueFamily, 0);
        var properties = physicalDevice.GetProperties();
        Name = Utf8Z(properties.DeviceName, 256);
        IsSoftware = properties.DeviceType == VkPhysicalDeviceType.Cpu;
        Limits = properties.Limits;

        Allocator = device.CreateVmaAllocator(new VmaAllocatorCreateInfo
        {
            Flags = VmaAllocatorCreateFlags.BufferDeviceAddressBit,
            PhysicalDevice = physicalDevice.Handle,
            Device = device.Handle,
            VulkanApiVersion = VulkanApi13,
        });

        var timelineType = new VkSemaphoreTypeCreateInfo { SemaphoreType = VkSemaphoreType.Timeline, InitialValue = 0 };
        _timeline = device.CreateSemaphore(new VkSemaphoreCreateInfo { Next = &timelineType }, null);
        _immediatePool = device.CreateCommandPool(new VkCommandPoolCreateInfo { QueueFamilyIndex = queueFamily, Flags = VkCommandPoolCreateFlags.TransientBit }, null);

        Bindless = new Bindless(this);
        Shaders = new ShaderLibrary(this);
        Pipelines = new PipelineCache(this);
        _slots = new FrameSlot[Math.Max(1, options.FramesInFlight)];
        for (var i = 0; i < _slots.Length; i++)
            _slots[i] = new FrameSlot(this, i);
        Bindless.CreateDefaults();
        Log.Info(Category, $"GPU {Name}, Vulkan {properties.ApiVersion >> 22}.{(properties.ApiVersion >> 12) & 0x3ff}, {(options.Headless ? "headless" : "windowed")}");
    }

    public const int VulkanApi13 = (1 << 22) | (3 << 12);

    public static GpuDevice Create(GpuDeviceOptions? options = null)
    {
        options ??= new GpuDeviceOptions();
        NativeLibraries.Install();

        var extensions = new HashSet<string>();
        if (!options.Headless)
            foreach (var name in Window.RequiredVulkanExtensions())
                extensions.Add(name);
        var layers = new HashSet<string>();
        if (options.Validation)
        {
            if (InstanceLayers().Contains("VK_LAYER_KHRONOS_validation"))
                layers.Add("VK_LAYER_KHRONOS_validation");
            else
                Log.Warning(Category, "validation requested, but the Khronos validation layer is not installed");
        }

        var appName = Encoding.UTF8.GetBytes(options.ApplicationName + "\0");
        var engineName = "Statventure\0"u8;
        Instance instance;
        fixed (byte* appNamePointer = appName)
            instance = Vulkan.CreateInstance(new Instance.CreateInfo
            {
                AppInfo = new VkApplicationInfo
                {
                    ApplicationName = appNamePointer,
                    EngineName = engineName.AsPointer(),
                    EngineVersion = (EngineInfo.Version.Major << 22) | (EngineInfo.Version.Minor << 12) | EngineInfo.Version.Build,
                    ApiVersion = VulkanApi13,
                },
                EnabledExtensions = extensions,
                EnabledLayers = layers,
            });

        // A discrete GPU with Vulkan 1.3, a graphics queue and (for windows) the swapchain extension.
        PhysicalDevice? chosen = null;
        var family = -1;
        foreach (var candidate in instance.PhysicalDevices.OrderByDescending(d => d.GetProperties().DeviceType == VkPhysicalDeviceType.DiscreteGpu))
        {
            var properties = candidate.GetProperties();
            if (properties.ApiVersion < VulkanApi13)
                continue;
            if (!options.Headless && !candidate.SupportsExtension("VK_KHR_swapchain"u8))
                continue;
            var queueFamily = candidate.GetFirstQueueFamilyWithFlag(VkQueueFlags.GraphicsBit | VkQueueFlags.ComputeBit);
            if (queueFamily < 0 || !SupportsRequiredFeatures(candidate))
                continue;
            chosen = candidate;
            family = queueFamily;
            break;
        }
        if (chosen is null)
        {
            instance.Dispose();
            throw new InvalidOperationException("No GPU with Vulkan 1.3 and the features Statventure needs was found");
        }

        var priority = 1f;
        var queueInfo = new VkDeviceQueueCreateInfo { QueueFamilyIndex = family, QueueCount = 1, QueuePriorities = &priority };
        var v13 = new VkPhysicalDeviceVulkan13Features
        {
            DynamicRendering = true,
            Synchronization2 = true,
            Maintenance4 = true,
        };
        var v12 = new VkPhysicalDeviceVulkan12Features
        {
            Next = &v13,
            DescriptorIndexing = true,
            RuntimeDescriptorArray = true,
            DescriptorBindingPartiallyBound = true,
            DescriptorBindingSampledImageUpdateAfterBind = true,
            DescriptorBindingStorageImageUpdateAfterBind = true,
            DescriptorBindingUpdateUnusedWhilePending = true,
            ShaderSampledImageArrayNonUniformIndexing = true,
            BufferDeviceAddress = true,
            ScalarBlockLayout = true,
            TimelineSemaphore = true,
        };
        var dualSource = (bool)chosen.GetFeatures().DualSrcBlend;
        var features2 = new VkPhysicalDeviceFeatures2
        {
            Next = &v12,
            Features = new VkPhysicalDeviceFeatures { ShaderInt64 = true, SamplerAnisotropy = true, FillModeNonSolid = true, IndependentBlend = true, DualSrcBlend = dualSource },
        };
        var deviceExtensions = new HashSet<string>();
        if (!options.Headless)
            deviceExtensions.Add("VK_KHR_swapchain");
        var device = chosen.CreateDevice(new Device.CreateInfo
        {
            Extensions = deviceExtensions,
            QueueCreateInfos = new ReadOnlySpan<VkDeviceQueueCreateInfo>(&queueInfo, 1),
            Next = &features2,
        });
        return new GpuDevice(options, instance, chosen, device, family, dualSource);
    }

    static bool SupportsRequiredFeatures(PhysicalDevice device)
    {
        var v13 = new VkPhysicalDeviceVulkan13Features();
        var v12 = new VkPhysicalDeviceVulkan12Features { Next = &v13 };
        var features2 = new VkPhysicalDeviceFeatures2 { Next = &v12 };
        device.GetFeatures2(ref features2);
        return v13.DynamicRendering && v13.Synchronization2 && v12.DescriptorIndexing && v12.RuntimeDescriptorArray
            && v12.DescriptorBindingPartiallyBound && v12.DescriptorBindingSampledImageUpdateAfterBind
            && v12.ShaderSampledImageArrayNonUniformIndexing && v12.BufferDeviceAddress && v12.ScalarBlockLayout
            && v12.TimelineSemaphore && features2.Features.ShaderInt64;
    }

    static HashSet<string> InstanceLayers()
    {
        var count = Vulkan.EnumerateInstanceLayerProperties_Count();
        var layers = new VkLayerProperties[count];
        Vulkan.EnumerateInstanceLayerProperties_Write(layers);
        var names = new HashSet<string>();
        foreach (var layer in layers)
            names.Add(Utf8Z(layer.LayerName, 256));
        return names;
    }

    internal static string Utf8Z(byte* text, int capacity)
    {
        var span = new ReadOnlySpan<byte>(text, capacity);
        var end = span.IndexOf((byte)0);
        return Encoding.UTF8.GetString(end < 0 ? span : span[..end]);
    }

    // ---------- Timeline ----------

    /// <summary>The value the most recent submission signals when it finishes.</summary>
    public ulong SubmittedValue => _submitted;

    /// <summary>The highest submission value the GPU has finished.</summary>
    public ulong CompletedValue => (ulong)Device.GetSemaphoreCounterValue(_timeline);

    public void WaitFor(ulong value)
    {
        if (value == 0 || CompletedValue >= value)
            return;
        var semaphore = _timeline;
        var target = (nint)value;
        Device.WaitSemaphores(new VkSemaphoreWaitInfo { SemaphoreCount = 1, Semaphores = &semaphore, Values = &target }, -1);
    }

    /// <summary>Waits until the GPU has finished everything submitted, then destroys released resources.</summary>
    public void WaitIdle()
    {
        WaitFor(_submitted);
        Device.WaitIdle();
        Collect();
    }

    /// <summary>
    /// Destroys something once the GPU can no longer be using it. During a frame (or an immediate
    /// recording) that is after the recording's submission has finished; otherwise after everything
    /// submitted so far has.
    /// </summary>
    public void Release(Action destroy)
    {
        lock (_graveyard)
        {
            if (_scopes.Count > 0)
                _scopes.Peek().Add(destroy);
            else
                _graveyard.Add((_submitted, destroy));
        }
    }

    void OpenScope()
    {
        lock (_graveyard)
            _scopes.Push([]);
    }

    void CloseScope(ulong value)
    {
        lock (_graveyard)
            foreach (var destroy in _scopes.Pop())
                _graveyard.Add((value, destroy));
    }

    /// <summary>Released resources not destroyed yet (for tests and diagnostics).</summary>
    internal int PendingReleaseCount
    {
        get
        {
            lock (_graveyard)
                return _graveyard.Count + _scopes.Sum(scope => scope.Count);
        }
    }

    /// <summary>Destroys released resources whose submissions have finished.</summary>
    public void Collect()
    {
        var completed = CompletedValue;
        List<Action>? ready = null;
        lock (_graveyard)
        {
            for (var i = _graveyard.Count - 1; i >= 0; i--)
                if (_graveyard[i].Value <= completed)
                {
                    (ready ??= []).Add(_graveyard[i].Destroy);
                    _graveyard.RemoveAt(i);
                }
        }
        if (ready is null)
            return;
        ready.Reverse();
        foreach (var destroy in ready)
            destroy();
    }

    /// <summary>Submits command buffers; the returned timeline value is signalled when they finish.</summary>
    internal ulong Submit(ReadOnlySpan<VkCommandBuffer> commands, ReadOnlySpan<VkSemaphoreSubmitInfo> waits, ReadOnlySpan<VkSemaphoreSubmitInfo> extraSignals)
    {
        lock (_submitGate)
        {
            var value = ++_submitted;
            var commandInfos = stackalloc VkCommandBufferSubmitInfo[commands.Length];
            for (var i = 0; i < commands.Length; i++)
                commandInfos[i] = new VkCommandBufferSubmitInfo { CommandBuffer = commands[i] };
            var signals = stackalloc VkSemaphoreSubmitInfo[extraSignals.Length + 1];
            signals[0] = new VkSemaphoreSubmitInfo { Semaphore = _timeline, Value = (nint)value, StageMask = VkPipelineStageFlags2.AllCommandsBit };
            for (var i = 0; i < extraSignals.Length; i++)
                signals[i + 1] = extraSignals[i];
            fixed (VkSemaphoreSubmitInfo* waitPointer = waits)
            {
                Span<VkSubmitInfo2> submit =
                [
                    new VkSubmitInfo2
                    {
                        WaitSemaphoreInfoCount = waits.Length,
                        WaitSemaphoreInfos = waitPointer,
                        CommandBufferInfoCount = commands.Length,
                        CommandBufferInfos = commandInfos,
                        SignalSemaphoreInfoCount = extraSignals.Length + 1,
                        SignalSemaphoreInfos = signals,
                    },
                ];
                Queue.Submit2(submit, default);
            }
            return value;
        }
    }

    /// <summary>Records commands, submits them and waits for them to finish. For loading, not for frames.</summary>
    public void Immediate(Action<CommandList> record)
    {
        var buffer = Device.AllocateCommandBuffer(new VkCommandBufferAllocateInfo { CommandPool = _immediatePool, Level = VkCommandBufferLevel.Primary });
        var list = new CommandList(this, buffer);
        OpenScope();
        ulong value;
        try
        {
            buffer.Begin(new VkCommandBufferBeginInfo { Flags = VkCommandBufferUsageFlags.OneTimeSubmitBit });
            record(list);
            buffer.End();
            var handle = buffer.Handle;
            value = Submit(new ReadOnlySpan<VkCommandBuffer>(&handle, 1), [], []);
        }
        catch
        {
            CloseScope(_submitted);
            throw;
        }
        CloseScope(value);
        WaitFor(value);
        Device.ResetCommandPool(_immediatePool, VkCommandPoolResetFlags.None);
        Collect();
    }

    // ---------- Frames ----------

    public long FrameNumber => _frameNumber;
    public int FramesInFlight => _slots.Length;

    /// <summary>
    /// Starts a frame: waits until the GPU is done with this frame slot's previous use, destroys
    /// released resources, and hands out a command list (already begun) for the frame's work.
    /// </summary>
    public Frame BeginFrame()
    {
        var slot = _slots[_frameNumber % _slots.Length];
        WaitFor(slot.LastValue);
        Collect();
        slot.Reset();
        OpenScope();
        return new Frame(this, slot, _frameNumber);
    }

    /// <summary>Ends and submits a frame; with a swapchain image, waits for it and presents it after.</summary>
    public void EndFrame(Frame frame, SwapchainImage? present = null)
    {
        var commands = frame.Commands;
        if (present is { } image)
            commands.Transition(image.Image, VkImageLayout.PresentSrcKHR);
        commands.Buffer.End();
        var handle = commands.Buffer.Handle;
        if (present is { } target)
        {
            var wait = new VkSemaphoreSubmitInfo { Semaphore = target.Acquired, StageMask = VkPipelineStageFlags2.ColorAttachmentOutputBit | VkPipelineStageFlags2.TransferBit };
            var signal = new VkSemaphoreSubmitInfo { Semaphore = target.RenderDone, StageMask = VkPipelineStageFlags2.AllCommandsBit };
            frame.Slot.LastValue = Submit(new ReadOnlySpan<VkCommandBuffer>(&handle, 1), new ReadOnlySpan<VkSemaphoreSubmitInfo>(&wait, 1), new ReadOnlySpan<VkSemaphoreSubmitInfo>(&signal, 1));
            target.Swapchain.Present(target);
        }
        else
            frame.Slot.LastValue = Submit(new ReadOnlySpan<VkCommandBuffer>(&handle, 1), [], []);
        frame.SubmittedValue = frame.Slot.LastValue;
        CloseScope(frame.Slot.LastValue);
        _frameNumber++;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Device.WaitIdle();
        Collect();
        foreach (var slot in _slots)
            slot.Dispose();
        Collect();
        Pipelines.Dispose();
        Shaders.Dispose();
        Bindless.Dispose();
        Collect();
        lock (_graveyard)
        {
            foreach (var (_, destroy) in _graveyard)
                destroy();
            _graveyard.Clear();
        }
        Device.DestroyCommandPool(_immediatePool, null);
        Device.DestroySemaphore(_timeline, null);
        Device.DestroyVmaAllocator(Allocator);
        Device.Dispose();
        Instance.Dispose();
    }
}
