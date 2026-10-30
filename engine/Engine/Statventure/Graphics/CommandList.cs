using System.Numerics;
using System.Runtime.CompilerServices;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;

namespace Statventure.Graphics;

public enum LoadAction
{
    Load,
    Clear,
    DontCare,
}

public readonly record struct ColorAttachment(GpuImage Target, LoadAction Load = LoadAction.Load, Vector4 Clear = default, GpuImage? Resolve = null, bool Store = true);

public readonly record struct DepthAttachment(GpuImage Target, LoadAction Load = LoadAction.Clear, float Clear = 1f, bool Store = false);

/// <summary>
/// Records GPU commands. Images know their layouts, so <see cref="Transition"/> picks the barrier;
/// rendering uses dynamic rendering (no render pass objects); every pipeline shares one layout
/// (the bindless set and 128 bytes of push constants).
/// </summary>
public sealed unsafe class CommandList
{
    public GpuDevice Device { get; }
    public CommandBuffer Buffer { get; }
    Pipeline? _bound;

    internal CommandList(GpuDevice device, CommandBuffer buffer)
    {
        Device = device;
        Buffer = buffer;
    }

    // ---------- Barriers ----------

    static (VkPipelineStageFlags2 Stage, VkAccessFlags2 Access) Usage(VkImageLayout layout) => layout switch
    {
        VkImageLayout.Undefined => (VkPipelineStageFlags2.TopOfPipeBit, VkAccessFlags2.None),
        VkImageLayout.ColorAttachmentOptimal => (VkPipelineStageFlags2.ColorAttachmentOutputBit, VkAccessFlags2.ColorAttachmentReadBit | VkAccessFlags2.ColorAttachmentWriteBit),
        VkImageLayout.DepthAttachmentOptimal or VkImageLayout.DepthStencilAttachmentOptimal =>
            (VkPipelineStageFlags2.EarlyFragmentTestsBit | VkPipelineStageFlags2.LateFragmentTestsBit, VkAccessFlags2.DepthStencilAttachmentReadBit | VkAccessFlags2.DepthStencilAttachmentWriteBit),
        VkImageLayout.ShaderReadOnlyOptimal or VkImageLayout.DepthReadOnlyOptimal =>
            (VkPipelineStageFlags2.VertexShaderBit | VkPipelineStageFlags2.FragmentShaderBit | VkPipelineStageFlags2.ComputeShaderBit, VkAccessFlags2.ShaderSampledReadBit),
        VkImageLayout.TransferSrcOptimal => (VkPipelineStageFlags2.AllTransferBit, VkAccessFlags2.TransferReadBit),
        VkImageLayout.TransferDstOptimal => (VkPipelineStageFlags2.AllTransferBit, VkAccessFlags2.TransferWriteBit),
        VkImageLayout.PresentSrcKHR => (VkPipelineStageFlags2.BottomOfPipeBit, VkAccessFlags2.None),
        _ => (VkPipelineStageFlags2.AllCommandsBit, VkAccessFlags2.MemoryReadBit | VkAccessFlags2.MemoryWriteBit),
    };

    /// <summary>Moves every subresource of an image into a layout, waiting for its previous use.</summary>
    public void Transition(GpuImage image, VkImageLayout layout)
    {
        if (image.Layout == layout && layout is not (VkImageLayout.ColorAttachmentOptimal or VkImageLayout.DepthAttachmentOptimal or VkImageLayout.General or VkImageLayout.TransferDstOptimal))
            return;
        Barrier(image, image.Layout, layout, 0, image.MipLevels);
        image.Layout = layout;
    }

    /// <summary>A barrier on some mip levels, from one layout to another (the image's tracked layout is not changed).</summary>
    public void Barrier(GpuImage image, VkImageLayout from, VkImageLayout to, int baseMip, int levels)
    {
        var (srcStage, srcAccess) = Usage(from);
        var (dstStage, dstAccess) = Usage(to);
        var barrier = new VkImageMemoryBarrier2
        {
            SrcStageMask = srcStage,
            SrcAccessMask = srcAccess,
            DstStageMask = dstStage,
            DstAccessMask = dstAccess,
            OldLayout = from,
            NewLayout = to,
            SrcQueueFamilyIndex = VK.QUEUE_FAMILY_IGNORED,
            DstQueueFamilyIndex = VK.QUEUE_FAMILY_IGNORED,
            Image = image.Handle,
            SubresourceRange = new VkImageSubresourceRange { AspectMask = image.Aspect, BaseMipLevel = baseMip, LevelCount = levels, LayerCount = 1 },
        };
        Buffer.PipelineBarrier2(new VkDependencyInfo { ImageMemoryBarrierCount = 1, ImageMemoryBarriers = &barrier });
    }

    /// <summary>Makes earlier writes (transfers, shaders) visible to all later reads.</summary>
    public void MemoryBarrier()
    {
        var barrier = new VkMemoryBarrier2
        {
            SrcStageMask = VkPipelineStageFlags2.AllCommandsBit,
            SrcAccessMask = VkAccessFlags2.MemoryWriteBit,
            DstStageMask = VkPipelineStageFlags2.AllCommandsBit,
            DstAccessMask = VkAccessFlags2.MemoryReadBit | VkAccessFlags2.MemoryWriteBit,
        };
        Buffer.PipelineBarrier2(new VkDependencyInfo { MemoryBarrierCount = 1, MemoryBarriers = &barrier });
    }

    // ---------- Rendering ----------

    static VkAttachmentLoadOp LoadOp(LoadAction action) => action switch
    {
        LoadAction.Clear => VkAttachmentLoadOp.Clear,
        LoadAction.DontCare => VkAttachmentLoadOp.DontCare,
        _ => VkAttachmentLoadOp.Load,
    };

    /// <summary>Starts drawing into colour targets (and a depth target); sets the viewport and scissor to the full target.</summary>
    public void BeginRendering(ReadOnlySpan<ColorAttachment> colors, DepthAttachment? depth = null)
    {
        var width = colors.Length > 0 ? colors[0].Target.Width : depth!.Value.Target.Width;
        var height = colors.Length > 0 ? colors[0].Target.Height : depth!.Value.Target.Height;
        var attachments = stackalloc VkRenderingAttachmentInfo[colors.Length];
        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            Transition(color.Target, VkImageLayout.ColorAttachmentOptimal);
            if (color.Resolve is not null)
                Transition(color.Resolve, VkImageLayout.ColorAttachmentOptimal);
            var clear = new VkClearValue();
            clear.Color = new VkClearColorValue { Float32 = new float4(color.Clear.X, color.Clear.Y, color.Clear.Z, color.Clear.W) };
            attachments[i] = new VkRenderingAttachmentInfo
            {
                ImageView = color.Target.View,
                ImageLayout = VkImageLayout.ColorAttachmentOptimal,
                LoadOp = LoadOp(color.Load),
                StoreOp = color.Store ? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare,
                ClearValue = clear,
                // Integer formats can't be averaged: their resolve takes sample 0.
                ResolveMode = color.Resolve is null ? VkResolveModeFlags.None
                    : Formats.IsInteger(color.Target.Format) ? VkResolveModeFlags.SampleZeroBit : VkResolveModeFlags.AverageBit,
                ResolveImageView = color.Resolve?.View ?? default,
                ResolveImageLayout = color.Resolve is null ? VkImageLayout.Undefined : VkImageLayout.ColorAttachmentOptimal,
            };
        }
        VkRenderingAttachmentInfo depthInfo = default;
        if (depth is { } d)
        {
            Transition(d.Target, VkImageLayout.DepthAttachmentOptimal);
            var clear = new VkClearValue();
            clear.DepthStencil = new VkClearDepthStencilValue { Depth = d.Clear };
            depthInfo = new VkRenderingAttachmentInfo
            {
                ImageView = d.Target.View,
                ImageLayout = VkImageLayout.DepthAttachmentOptimal,
                LoadOp = LoadOp(d.Load),
                StoreOp = d.Store ? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare,
                ClearValue = clear,
            };
        }
        Buffer.BeginRendering(new VkRenderingInfo
        {
            RenderArea = new VkRect2D { Extent = new VkExtent2D { Width = width, Height = height } },
            LayerCount = 1,
            ColorAttachmentCount = colors.Length,
            ColorAttachments = attachments,
            DepthAttachment = depth is null ? null : &depthInfo,
        });
        SetViewport(0, 0, width, height);
        SetScissor(0, 0, width, height);
        _bound = null;
    }

    public void BeginRendering(ColorAttachment color, DepthAttachment? depth = null) =>
        BeginRendering(new ReadOnlySpan<ColorAttachment>(ref color), depth);

    public void EndRendering() => Buffer.EndRendering();

    public void SetViewport(float x, float y, float width, float height, float minDepth = 0f, float maxDepth = 1f) =>
        Buffer.SetViewport(0, [new VkViewport { X = x, Y = y, Width = width, Height = height, MinDepth = minDepth, MaxDepth = maxDepth }]);

    public void SetScissor(int x, int y, int width, int height) =>
        Buffer.SetScissor(0, [new VkRect2D { Offset = new VkOffset2D { X = Math.Max(0, x), Y = Math.Max(0, y) }, Extent = new VkExtent2D { Width = Math.Max(0, width), Height = Math.Max(0, height) } }]);

    public void BindPipeline(Pipeline pipeline)
    {
        if (ReferenceEquals(_bound, pipeline))
            return;
        var bindPoint = pipeline.IsCompute ? VkPipelineBindPoint.Compute : VkPipelineBindPoint.Graphics;
        Buffer.BindPipeline(bindPoint, pipeline.Handle);
        if (_bound is null || _bound.IsCompute != pipeline.IsCompute)
            Buffer.BindDescriptorSets(bindPoint, Device.Pipelines.Layout, 0, [Device.Bindless.Set], []);
        _bound = pipeline;
    }

    /// <summary>Sets the push constants (up to 128 bytes) for every shader stage.</summary>
    public void Push<T>(in T data) where T : unmanaged
    {
        if (sizeof(T) > PipelineCache.PushConstantSize)
            throw new ArgumentException($"{typeof(T).Name} is {sizeof(T)} bytes; push constants hold {PipelineCache.PushConstantSize}");
        fixed (T* pointer = &data)
            Buffer.PushConstants(Device.Pipelines.Layout, PipelineCache.PushStages, 0, new ReadOnlySpan<byte>(pointer, sizeof(T)));
    }

    public void Draw(int vertexCount, int instanceCount = 1, int firstVertex = 0, int firstInstance = 0) =>
        Buffer.Draw(vertexCount, instanceCount, firstVertex, firstInstance);

    public void DrawIndexed(int indexCount, int instanceCount = 1, int firstIndex = 0, int vertexOffset = 0, int firstInstance = 0) =>
        Buffer.DrawIndexed(indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

    public void BindIndexBuffer(GpuBuffer buffer, ulong offset = 0, bool wide = true) =>
        Buffer.BindIndexBuffer(buffer.Handle, new ByteSize64(offset), wide ? VkIndexType.UInt32 : VkIndexType.UInt16);

    public void Dispatch(int x, int y = 1, int z = 1) => Buffer.Dispatch(x, y, z);

    // ---------- Transfers ----------

    public void CopyBuffer(GpuBuffer source, GpuBuffer destination, ulong size, ulong sourceOffset = 0, ulong destinationOffset = 0) =>
        Buffer.CopyBuffer(source.Handle, destination.Handle, [new VkBufferCopy { SrcOffset = new ByteSize64(sourceOffset), DstOffset = new ByteSize64(destinationOffset), Size = new ByteSize64(size) }]);

    /// <summary>Copies tightly packed pixels into one mip level (the image must be in TransferDstOptimal).</summary>
    public void CopyBufferToImage(GpuBuffer source, GpuImage image, int mip, ulong offset = 0)
    {
        int w = Math.Max(1, image.Width >> mip), h = Math.Max(1, image.Height >> mip);
        Buffer.CopyBufferToImage(source.Handle, image.Handle, VkImageLayout.TransferDstOptimal,
            [new VkBufferImageCopy
            {
                BufferOffset = new ByteSize64(offset),
                ImageSubresource = new VkImageSubresourceLayers { AspectMask = image.Aspect, MipLevel = mip, LayerCount = 1 },
                ImageExtent = new VkExtent3D { Width = w, Height = h, Depth = 1 },
            }]);
    }

    /// <summary>Copies tightly packed pixels into a region of mip level 0 (the image must be in TransferDstOptimal).</summary>
    public void CopyBufferToImageRegion(GpuBuffer source, GpuImage image, ulong offset, int x, int y, int width, int height)
    {
        Buffer.CopyBufferToImage(source.Handle, image.Handle, VkImageLayout.TransferDstOptimal,
            [new VkBufferImageCopy
            {
                BufferOffset = new ByteSize64(offset),
                ImageSubresource = new VkImageSubresourceLayers { AspectMask = image.Aspect, LayerCount = 1 },
                ImageOffset = new VkOffset3D { X = x, Y = y },
                ImageExtent = new VkExtent3D { Width = width, Height = height, Depth = 1 },
            }]);
    }

    /// <summary>Copies one mip level into a buffer (the image must be in TransferSrcOptimal).</summary>
    public void CopyImageToBuffer(GpuImage image, GpuBuffer destination, int mip, ulong offset = 0)
    {
        int w = Math.Max(1, image.Width >> mip), h = Math.Max(1, image.Height >> mip);
        Buffer.CopyImageToBuffer(image.Handle, VkImageLayout.TransferSrcOptimal, destination.Handle,
            [new VkBufferImageCopy
            {
                BufferOffset = new ByteSize64(offset),
                ImageSubresource = new VkImageSubresourceLayers { AspectMask = image.Aspect, MipLevel = mip, LayerCount = 1 },
                ImageExtent = new VkExtent3D { Width = w, Height = h, Depth = 1 },
            }]);
    }

    /// <summary>Copies a pixel region between images (source in TransferSrcOptimal, destination in TransferDstOptimal).</summary>
    public void CopyImage(GpuImage source, GpuImage destination, int sourceX, int sourceY, int width, int height, int destinationX = 0, int destinationY = 0)
    {
        Buffer.CopyImage(source.Handle, VkImageLayout.TransferSrcOptimal, destination.Handle, VkImageLayout.TransferDstOptimal,
            [new VkImageCopy
            {
                SrcSubresource = new VkImageSubresourceLayers { AspectMask = source.Aspect, LayerCount = 1 },
                SrcOffset = new VkOffset3D { X = sourceX, Y = sourceY },
                DstSubresource = new VkImageSubresourceLayers { AspectMask = destination.Aspect, LayerCount = 1 },
                DstOffset = new VkOffset3D { X = destinationX, Y = destinationY },
                Extent = new VkExtent3D { Width = width, Height = height, Depth = 1 },
            }]);
    }

    /// <summary>Fills mip levels 1.. from level 0 by halving blits; the image must be in TransferDstOptimal, and ends there.</summary>
    public void GenerateMipmaps(GpuImage image)
    {
        for (var mip = 1; mip < image.MipLevels; mip++)
        {
            Barrier(image, VkImageLayout.TransferDstOptimal, VkImageLayout.TransferSrcOptimal, mip - 1, 1);
            int sw = Math.Max(1, image.Width >> (mip - 1)), sh = Math.Max(1, image.Height >> (mip - 1));
            int dw = Math.Max(1, image.Width >> mip), dh = Math.Max(1, image.Height >> mip);
            var blit = new VkImageBlit
            {
                SrcSubresource = new VkImageSubresourceLayers { AspectMask = image.Aspect, MipLevel = mip - 1, LayerCount = 1 },
                DstSubresource = new VkImageSubresourceLayers { AspectMask = image.Aspect, MipLevel = mip, LayerCount = 1 },
            };
            blit.SrcOffsets[1] = new VkOffset3D { X = sw, Y = sh, Z = 1 };
            blit.DstOffsets[1] = new VkOffset3D { X = dw, Y = dh, Z = 1 };
            Buffer.BlitImage(image.Handle, VkImageLayout.TransferSrcOptimal, image.Handle, VkImageLayout.TransferDstOptimal, [blit], VkFilter.Linear);
            Barrier(image, VkImageLayout.TransferSrcOptimal, VkImageLayout.TransferDstOptimal, mip - 1, 1);
        }
    }

    /// <summary>Clears a whole colour image (moved to TransferDstOptimal).</summary>
    public void ClearColor(GpuImage image, Vector4 color)
    {
        Transition(image, VkImageLayout.TransferDstOptimal);
        var value = new VkClearColorValue { Float32 = new float4(color.X, color.Y, color.Z, color.W) };
        var range = new VkImageSubresourceRange { AspectMask = image.Aspect, LevelCount = image.MipLevels, LayerCount = 1 };
        Buffer.ClearColorImage(image.Handle, VkImageLayout.TransferDstOptimal, &value, [range]);
    }
}
