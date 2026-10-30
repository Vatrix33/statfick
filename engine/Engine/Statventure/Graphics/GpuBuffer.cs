using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Vma;

namespace Statventure.Graphics;

public enum MemoryKind
{
    /// <summary>GPU memory; filled through uploads.</summary>
    Device,
    /// <summary>CPU-writable, mapped; for data written each frame or staged for upload.</summary>
    Upload,
    /// <summary>CPU-readable, mapped; for reading results back.</summary>
    Readback,
}

/// <summary>
/// A Vulkan buffer in VMA memory. Every buffer has a device address, so shaders can read it through
/// buffer references (the engine's shaders pull their vertices that way).
/// </summary>
public sealed unsafe class GpuBuffer : IDisposable
{
    readonly GpuDevice _device;
    VmaAllocation _allocation;
    bool _disposed;

    public VkBuffer Handle { get; }
    public ulong Size { get; }
    public ulong Address { get; }
    public MemoryKind Kind { get; }
    /// <summary>Where the buffer is mapped in CPU memory (Upload and Readback buffers), else null.</summary>
    public byte* Mapped { get; }

    GpuBuffer(GpuDevice device, VkBuffer handle, VmaAllocation allocation, ulong size, MemoryKind kind, byte* mapped, ulong address)
    {
        _device = device;
        Handle = handle;
        _allocation = allocation;
        Size = size;
        Kind = kind;
        Mapped = mapped;
        Address = address;
    }

    public static GpuBuffer Create(GpuDevice device, ulong size, VkBufferUsageFlags usage, MemoryKind kind)
    {
        size = Math.Max(size, 16);
        usage |= VkBufferUsageFlags.ShaderDeviceAddressBit | VkBufferUsageFlags.TransferDstBit | VkBufferUsageFlags.TransferSrcBit;
        var allocation = new VmaAllocationCreateInfo
        {
            Usage = kind == MemoryKind.Device ? VmaMemoryUsage.AutoPreferDevice : VmaMemoryUsage.Auto,
            Flags = kind switch
            {
                MemoryKind.Upload => VmaAllocationCreateFlags.HostAccessSequentialWriteBit | VmaAllocationCreateFlags.MappedBit,
                MemoryKind.Readback => VmaAllocationCreateFlags.HostAccessRandomBit | VmaAllocationCreateFlags.MappedBit,
                _ => VmaAllocationCreateFlags.None,
            },
        };
        var info = new VkBufferCreateInfo { Size = ByteSize.Of((long)size), Usage = usage, SharingMode = VkSharingMode.Exclusive };
        var result = device.Allocator.CreateBuffer(info, allocation, out var buffer, out var vmaAllocation, out var allocationInfo);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"buffer of {size} bytes could not be created: {result}");
        var address = device.Device.GetBufferDeviceAddress(new VkBufferDeviceAddressInfo { Buffer = buffer }).Value;
        return new GpuBuffer(device, buffer, vmaAllocation, size, kind, (byte*)allocationInfo.PMappedData.Pointer, address);
    }

    /// <summary>A GPU buffer holding the given data (staged through an upload buffer).</summary>
    public static GpuBuffer CreateWithData<T>(GpuDevice device, ReadOnlySpan<T> data, VkBufferUsageFlags usage) where T : unmanaged
    {
        var length = (ulong)MemoryMarshal.AsBytes(data).Length;
        var buffer = Create(device, length, usage, MemoryKind.Device);
        using var staging = Create(device, length, 0, MemoryKind.Upload);
        staging.Write(data);
        device.Immediate(commands => commands.CopyBuffer(staging, buffer, length));
        return buffer;
    }

    public Span<byte> MappedBytes => Mapped == null ? throw new InvalidOperationException("buffer is not mapped") : new Span<byte>(Mapped, (int)Size);

    public void Write<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(data);
        if (offset + (ulong)bytes.Length > Size)
            throw new ArgumentOutOfRangeException(nameof(data), "write past the end of the buffer");
        bytes.CopyTo(MappedBytes[(int)offset..]);
    }

    public void Read<T>(Span<T> destination, ulong offset = 0) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(destination);
        MappedBytes.Slice((int)offset, bytes.Length).CopyTo(bytes);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        var (allocator, handle, allocation) = (_device.Allocator, Handle, _allocation);
        _device.Release(() => allocator.DestroyBuffer(handle, allocation));
    }
}
