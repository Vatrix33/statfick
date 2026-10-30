using Brutal.GlfwApi;
using Brutal.VulkanApi;
using Statventure.Core;
using Statventure.Platform;

namespace Statventure.Graphics;

/// <summary>An acquired swapchain image: draw into <see cref="Image"/>, then end the frame with it.</summary>
public readonly record struct SwapchainImage(Swapchain Swapchain, GpuImage Image, int Index, VkSemaphore Acquired, VkSemaphore RenderDone);

/// <summary>
/// The images a window shows. It rebuilds itself when the window changes size or the surface goes
/// out of date. The colour format is 8-bit UNORM (B8G8R8A8 or R8G8B8A8), so colours and blending
/// stay in sRGB space, as in a browser.
/// </summary>
public sealed unsafe class Swapchain : IDisposable
{
    readonly GpuDevice _device;
    readonly Window _window;
    readonly VkSurfaceKHR _surface;
    VkSwapchainKHR _handle;
    GpuImage[] _images = [];
    VkSemaphore[] _renderDone = [];
    bool _rebuild = true;
    bool _vsync;

    public VkFormat Format { get; private set; }
    public VkColorSpaceKHR ColorSpace { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int ImageCount => _images.Length;

    public bool VSync
    {
        get => _vsync;
        set
        {
            if (_vsync == value)
                return;
            _vsync = value;
            _rebuild = true;
        }
    }

    public Swapchain(GpuDevice device, Window window, bool vsync = true)
    {
        if (device.Options.Headless)
            throw new InvalidOperationException("a headless GPU device cannot present to a window");
        _device = device;
        _window = window;
        _vsync = vsync;
        if (Glfw.CreateWindowSurface(device.Instance.VkHandle, window.Native, out var surface) != 0)
            throw new InvalidOperationException("the window surface could not be created");
        _surface = new VkSurfaceKHR((nint)surface);
        if (!device.PhysicalDevice.GetSurfaceSupportKHR(device.QueueFamily, _surface))
            throw new InvalidOperationException("the GPU's queue cannot present to this window");
        ChooseFormat();
    }

    void ChooseFormat()
    {
        var formats = new VkSurfaceFormatKHR[_device.PhysicalDevice.GetSurfaceFormatsKHR_Count(_surface)];
        _device.PhysicalDevice.GetSurfaceFormatsKHR_Write(_surface, formats);
        var chosen = formats.FirstOrDefault(f => f.Format == VkFormat.B8G8R8A8UNorm && f.ColorSpace == VkColorSpaceKHR.SRGBNonlinearKHR,
            formats.FirstOrDefault(f => f.Format == VkFormat.R8G8B8A8UNorm, formats[0]));
        Format = chosen.Format;
        ColorSpace = chosen.ColorSpace;
    }

    VkPresentModeKHR ChoosePresentMode()
    {
        if (_vsync)
            return VkPresentModeKHR.FifoKHR;
        var modes = new VkPresentModeKHR[_device.PhysicalDevice.GetSurfacePresentModesKHR_Count(_surface)];
        _device.PhysicalDevice.GetSurfacePresentModesKHR_Write(_surface, modes);
        return modes.Contains(VkPresentModeKHR.MailboxKHR) ? VkPresentModeKHR.MailboxKHR
            : modes.Contains(VkPresentModeKHR.ImmediateKHR) ? VkPresentModeKHR.ImmediateKHR
            : VkPresentModeKHR.FifoKHR;
    }

    void Rebuild()
    {
        _device.WaitIdle();
        DestroyImages();
        var capabilities = _device.PhysicalDevice.GetSurfaceCapabilitiesKHR(_surface);
        var extent = capabilities.CurrentExtent.Width == -1
            ? new VkExtent2D { Width = _window.FramebufferWidth, Height = _window.FramebufferHeight }
            : capabilities.CurrentExtent;
        var count = Math.Max(capabilities.MinImageCount + 1, 3);
        if (capabilities.MaxImageCount != 0)
            count = Math.Min(count, capabilities.MaxImageCount);
        var usage = VkImageUsageFlags.ColorAttachmentBit | VkImageUsageFlags.TransferDstBit
            | (capabilities.SupportedUsageFlags & VkImageUsageFlags.TransferSrcBit);
        var old = _handle;
        _handle = _device.Device.CreateSwapchainKHR(new VkSwapchainCreateInfoKHR
        {
            Surface = _surface,
            MinImageCount = count,
            ImageFormat = Format,
            ImageColorSpace = ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = usage,
            ImageSharingMode = VkSharingMode.Exclusive,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueBitKHR,
            PresentMode = ChoosePresentMode(),
            Clipped = true,
            OldSwapchain = old,
        }, null);
        if (old.IsNotNull())
            _device.Device.DestroySwapchainKHR(old, null);
        Width = extent.Width;
        Height = extent.Height;
        var handles = new VkImage[_device.Device.GetSwapchainImagesKHR_Count(_handle)];
        _device.Device.GetSwapchainImagesKHR_Write(_handle, handles);
        _images = handles.Select(image =>
        {
            var view = _device.Device.CreateImageView(new VkImageViewCreateInfo
            {
                Image = image,
                ViewType = VkImageViewType._2D,
                Format = Format,
                SubresourceRange = new VkImageSubresourceRange { AspectMask = VkImageAspectFlags.ColorBit, LevelCount = 1, LayerCount = 1 },
            }, null);
            return GpuImage.Wrap(_device, image, view, Format, Width, Height, usage);
        }).ToArray();
        _renderDone = handles.Select(_ => _device.Device.CreateSemaphore(new VkSemaphoreCreateInfo(), null)).ToArray();
        _rebuild = false;
        Log.Debug(GpuDevice.Category, $"swapchain {Width}x{Height} {Format}, {_images.Length} images, {(VSync ? "vsync" : "no vsync")}");
    }

    void DestroyImages()
    {
        foreach (var image in _images)
            _device.Device.DestroyImageView(image.View, null);
        foreach (var semaphore in _renderDone)
            _device.Device.DestroySemaphore(semaphore, null);
        _images = [];
        _renderDone = [];
    }

    /// <summary>
    /// Gets the next image to draw, rebuilding first if the window changed. Returns null when there
    /// is nothing to draw into (minimized window) or the surface just went out of date.
    /// </summary>
    public SwapchainImage? Acquire(Frame frame)
    {
        if (_window.IsMinimized)
            return null;
        if (_rebuild || _window.FramebufferWidth != Width || _window.FramebufferHeight != Height)
            Rebuild();
        var semaphore = frame.Slot.AcquireSemaphore;
        var result = _device.Device.AcquireNextImageKHR(_handle, -1, semaphore, default, out var index);
        if (result == VkResult.ErrorOutOfDateKHR)
        {
            _rebuild = true;
            return null;
        }
        if (result == VkResult.SuboptimalKHR)
            _rebuild = true;
        else if (result != VkResult.Success)
            throw new InvalidOperationException("swapchain image could not be acquired: " + result);
        var image = _images[index];
        image.Layout = VkImageLayout.Undefined;
        return new SwapchainImage(this, image, index, semaphore, _renderDone[index]);
    }

    internal void Present(SwapchainImage image)
    {
        var handle = _handle;
        var index = image.Index;
        var wait = image.RenderDone;
        var result = _device.Queue.PresentKHR(new VkPresentInfoKHR
        {
            WaitSemaphoreCount = 1,
            WaitSemaphores = &wait,
            SwapchainCount = 1,
            Swapchains = &handle,
            ImageIndices = &index,
        });
        if (result is VkResult.ErrorOutOfDateKHR or VkResult.SuboptimalKHR)
            _rebuild = true;
    }

    public void Dispose()
    {
        _device.WaitIdle();
        DestroyImages();
        if (_handle.IsNotNull())
            _device.Device.DestroySwapchainKHR(_handle, null);
        _device.Instance.DestroySurfaceKHR(_surface, null);
    }
}
