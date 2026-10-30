using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;

namespace Statventure.Graphics;

/// <summary>A slice of a frame's upload buffer: CPU-writable now, read by the GPU through its address.</summary>
public readonly unsafe ref struct Transient<T> where T : unmanaged
{
    public readonly Span<T> Data;
    public readonly ulong Address;
    public readonly GpuBuffer Buffer;
    public readonly ulong Offset;

    internal Transient(Span<T> data, ulong address, GpuBuffer buffer, ulong offset)
    {
        Data = data;
        Address = address;
        Buffer = buffer;
        Offset = offset;
    }
}

/// <summary>
/// The resources of one frame in flight: a command pool and a linear upload buffer for per-frame
/// data (vertices, instance data). Both are reused once the GPU has finished the slot's last frame.
/// </summary>
internal sealed class FrameSlot : IDisposable
{
    const ulong InitialUploadSize = 4 << 20;
    const ulong Alignment = 256;

    readonly GpuDevice _device;
    readonly VkCommandPool _pool;
    GpuBuffer _upload;
    ulong _used;

    public int Index { get; }
    public ulong LastValue { get; set; }
    public VkSemaphore AcquireSemaphore { get; }
    public CommandBuffer Commands { get; }

    public unsafe FrameSlot(GpuDevice device, int index)
    {
        _device = device;
        Index = index;
        _pool = device.Device.CreateCommandPool(new VkCommandPoolCreateInfo { QueueFamilyIndex = device.QueueFamily }, null);
        Commands = device.Device.AllocateCommandBuffer(new VkCommandBufferAllocateInfo { CommandPool = _pool, Level = VkCommandBufferLevel.Primary });
        AcquireSemaphore = device.Device.CreateSemaphore(new VkSemaphoreCreateInfo(), null);
        _upload = GpuBuffer.Create(device, InitialUploadSize, VkBufferUsageFlags.StorageBufferBit | VkBufferUsageFlags.IndexBufferBit | VkBufferUsageFlags.VertexBufferBit, MemoryKind.Upload);
    }

    public void Reset()
    {
        _device.Device.ResetCommandPool(_pool, VkCommandPoolResetFlags.None);
        _used = 0;
    }

    public unsafe Transient<T> Allocate<T>(int count) where T : unmanaged
    {
        var size = (ulong)(count * sizeof(T));
        var offset = (_used + Alignment - 1) / Alignment * Alignment;
        if (offset + size > _upload.Size)
        {
            // Grow: the old buffer stays alive until this frame is done (deferred release).
            var newSize = Math.Max(_upload.Size * 2, size + Alignment);
            _upload.Dispose();
            _upload = GpuBuffer.Create(_device, newSize, VkBufferUsageFlags.StorageBufferBit | VkBufferUsageFlags.IndexBufferBit | VkBufferUsageFlags.VertexBufferBit, MemoryKind.Upload);
            offset = 0;
        }
        _used = offset + size;
        var span = new Span<T>(_upload.Mapped + offset, count);
        return new Transient<T>(span, _upload.Address + offset, _upload, offset);
    }

    public void Dispose()
    {
        _upload.Dispose();
        _device.Device.DestroySemaphore(AcquireSemaphore, null);
        _device.Device.DestroyCommandPool(_pool, null);
    }
}

/// <summary>
/// One frame of GPU work: begun by <see cref="GpuDevice.BeginFrame"/>, recorded through
/// <see cref="Commands"/>, submitted by <see cref="GpuDevice.EndFrame"/>.
/// </summary>
public sealed class Frame
{
    internal FrameSlot Slot { get; }
    public GpuDevice Device { get; }
    public CommandList Commands { get; }
    public long Number { get; }

    internal Frame(GpuDevice device, FrameSlot slot, long number)
    {
        Device = device;
        Slot = slot;
        Number = number;
        Commands = new CommandList(device, slot.Commands);
        slot.Commands.Begin(new VkCommandBufferBeginInfo { Flags = VkCommandBufferUsageFlags.OneTimeSubmitBit });
    }

    /// <summary>Space in this frame's upload buffer for <paramref name="count"/> items (valid until the frame is submitted).</summary>
    public Transient<T> Allocate<T>(int count) where T : unmanaged => Slot.Allocate<T>(count);

    /// <summary>Copies items into this frame's upload buffer and returns their GPU address.</summary>
    public ulong Upload<T>(ReadOnlySpan<T> items) where T : unmanaged
    {
        var slice = Allocate<T>(items.Length);
        items.CopyTo(slice.Data);
        return slice.Address;
    }
}
