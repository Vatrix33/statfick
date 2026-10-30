using System.Text;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;

namespace Statventure.Graphics;

public enum BlendMode
{
    /// <summary>No blending: the shader's colour replaces what is there.</summary>
    Opaque,
    /// <summary>Normal "over" blending of premultiplied colour (the engine's default).</summary>
    Premultiplied,
    /// <summary>Straight (non-premultiplied) alpha.</summary>
    Alpha,
    Additive,
    /// <summary>Darkens: result = source × destination (premultiplied).</summary>
    Multiply,
    /// <summary>Lightens: result = source + destination − source × destination.</summary>
    Screen,
    /// <summary>LCD text: per-channel coverage through dual-source blending (needs dualSrcBlend).</summary>
    DualSourceLcd,
}

/// <summary>
/// Everything that makes a graphics pipeline. Shaders pull their vertices from buffers by device
/// address, so there is no vertex input state.
/// </summary>
public sealed record GraphicsPipelineDesc
{
    public required string VertexShader { get; init; }
    public required string FragmentShader { get; init; }
    public VkPrimitiveTopology Topology { get; init; } = VkPrimitiveTopology.TriangleList;
    public BlendMode Blend { get; init; } = BlendMode.Premultiplied;
    public VkFormat[] ColorFormats { get; init; } = [VkFormat.R8G8B8A8UNorm];
    public VkFormat DepthFormat { get; init; } = VkFormat.Undefined;
    public bool DepthTest { get; init; }
    public bool DepthWrite { get; init; }
    public VkCompareOp DepthCompare { get; init; } = VkCompareOp.LessOrEqual;
    public VkCullModeFlags Cull { get; init; } = VkCullModeFlags.None;
    public VkFrontFace FrontFace { get; init; } = VkFrontFace.CounterClockwise;
    public VkSampleCountFlags Samples { get; init; } = VkSampleCountFlags._1Bit;
    public bool Wireframe { get; init; }
    public bool ColorWrite { get; init; } = true;

    internal string Key()
    {
        var key = new StringBuilder();
        key.Append(VertexShader).Append('|').Append(FragmentShader).Append('|').Append(Topology).Append('|').Append(Blend);
        foreach (var format in ColorFormats)
            key.Append('|').Append(format);
        key.Append('|').Append(DepthFormat).Append(DepthTest).Append(DepthWrite).Append(DepthCompare).Append(Cull).Append(FrontFace).Append(Samples).Append(Wireframe).Append(ColorWrite);
        return key.ToString();
    }
}

public sealed class Pipeline
{
    public VkPipeline Handle { get; }
    public bool IsCompute { get; }
    public string Name { get; }

    internal Pipeline(VkPipeline handle, bool compute, string name)
    {
        Handle = handle;
        IsCompute = compute;
        Name = name;
    }
}

/// <summary>
/// Builds and keeps pipelines. They all share one layout: the bindless set (set 0) and
/// <see cref="PushConstantSize"/> bytes of push constants visible to every stage.
/// </summary>
public sealed unsafe class PipelineCache : IDisposable
{
    public const int PushConstantSize = 128;
    public const VkShaderStageFlags PushStages = VkShaderStageFlags.VertexBit | VkShaderStageFlags.FragmentBit | VkShaderStageFlags.ComputeBit;

    readonly GpuDevice _device;
    readonly Dictionary<string, Pipeline> _pipelines = [];

    public VkPipelineLayout Layout { get; }

    internal PipelineCache(GpuDevice device)
    {
        _device = device;
        var push = new VkPushConstantRange { StageFlags = PushStages, Offset = ByteSize.Zero, Size = ByteSize.Of(PushConstantSize) };
        Layout = device.Device.CreatePipelineLayout([device.Bindless.Layout], [push], null);
    }

    public int Count => _pipelines.Count;

    public Pipeline Get(GraphicsPipelineDesc desc)
    {
        var key = desc.Key();
        lock (_pipelines)
        {
            if (_pipelines.TryGetValue(key, out var existing))
                return existing;
            var pipeline = new Pipeline(Build(desc), false, desc.VertexShader + "+" + desc.FragmentShader);
            _pipelines[key] = pipeline;
            return pipeline;
        }
    }

    public Pipeline GetCompute(string shader)
    {
        var key = "compute|" + shader;
        lock (_pipelines)
        {
            if (_pipelines.TryGetValue(key, out var existing))
                return existing;
            var entry = "main"u8;
            VkPipeline handle;
            _device.Device.CreateComputePipelines(default,
                [new VkComputePipelineCreateInfo
                {
                    Stage = new VkPipelineShaderStageCreateInfo { Stage = VkShaderStageFlags.ComputeBit, Module = _device.Shaders.Get(shader), Name = entry.AsPointer() },
                    Layout = Layout,
                }], null, new ReadOnlySpan<VkPipeline>(&handle, 1));
            var pipeline = new Pipeline(handle, true, shader);
            _pipelines[key] = pipeline;
            return pipeline;
        }
    }

    VkPipeline Build(GraphicsPipelineDesc desc)
    {
        var entry = "main"u8;
        var stages = stackalloc VkPipelineShaderStageCreateInfo[2];
        stages[0] = new VkPipelineShaderStageCreateInfo { Stage = VkShaderStageFlags.VertexBit, Module = _device.Shaders.Get(desc.VertexShader), Name = entry.AsPointer() };
        stages[1] = new VkPipelineShaderStageCreateInfo { Stage = VkShaderStageFlags.FragmentBit, Module = _device.Shaders.Get(desc.FragmentShader), Name = entry.AsPointer() };
        var vertexInput = new VkPipelineVertexInputStateCreateInfo();
        var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo { Topology = desc.Topology };
        var viewport = new VkPipelineViewportStateCreateInfo { ViewportCount = 1, ScissorCount = 1 };
        var raster = new VkPipelineRasterizationStateCreateInfo
        {
            PolygonMode = desc.Wireframe ? VkPolygonMode.Line : VkPolygonMode.Fill,
            CullMode = desc.Cull,
            FrontFace = desc.FrontFace,
            LineWidth = 1f,
        };
        var multisample = new VkPipelineMultisampleStateCreateInfo { RasterizationSamples = desc.Samples };
        var depth = new VkPipelineDepthStencilStateCreateInfo
        {
            DepthTestEnable = desc.DepthTest,
            DepthWriteEnable = desc.DepthWrite,
            DepthCompareOp = desc.DepthCompare,
        };
        var colorCount = desc.ColorFormats.Length;
        var blends = stackalloc VkPipelineColorBlendAttachmentState[Math.Max(1, colorCount)];
        for (var i = 0; i < colorCount; i++)
            blends[i] = BlendState(desc.Blend, desc.ColorWrite);
        var blend = new VkPipelineColorBlendStateCreateInfo { AttachmentCount = colorCount, Attachments = blends };
        var dynamicStates = stackalloc VkDynamicState[2];
        dynamicStates[0] = VkDynamicState.Viewport;
        dynamicStates[1] = VkDynamicState.Scissor;
        var dynamic = new VkPipelineDynamicStateCreateInfo { DynamicStateCount = 2, DynamicStates = dynamicStates };
        var formats = stackalloc VkFormat[Math.Max(1, colorCount)];
        for (var i = 0; i < colorCount; i++)
            formats[i] = desc.ColorFormats[i];
        var rendering = new VkPipelineRenderingCreateInfo
        {
            ColorAttachmentCount = colorCount,
            ColorAttachmentFormats = formats,
            DepthAttachmentFormat = desc.DepthFormat,
        };
        VkPipeline pipeline;
        _device.Device.CreateGraphicsPipelines(default,
            [new VkGraphicsPipelineCreateInfo
            {
                Next = &rendering,
                StageCount = 2,
                Stages = stages,
                VertexInputState = &vertexInput,
                InputAssemblyState = &inputAssembly,
                ViewportState = &viewport,
                RasterizationState = &raster,
                MultisampleState = &multisample,
                DepthStencilState = &depth,
                ColorBlendState = &blend,
                DynamicState = &dynamic,
                Layout = Layout,
            }], null, new ReadOnlySpan<VkPipeline>(&pipeline, 1));
        return pipeline;
    }

    static VkPipelineColorBlendAttachmentState BlendState(BlendMode mode, bool write)
    {
        var mask = write ? VkColorComponentFlags.RBit | VkColorComponentFlags.GBit | VkColorComponentFlags.BBit | VkColorComponentFlags.ABit : 0;
        VkPipelineColorBlendAttachmentState State(VkBlendFactor srcColor, VkBlendFactor dstColor, VkBlendFactor srcAlpha, VkBlendFactor dstAlpha) => new()
        {
            BlendEnable = true,
            SrcColorBlendFactor = srcColor,
            DstColorBlendFactor = dstColor,
            ColorBlendOp = VkBlendOp.Add,
            SrcAlphaBlendFactor = srcAlpha,
            DstAlphaBlendFactor = dstAlpha,
            AlphaBlendOp = VkBlendOp.Add,
            ColorWriteMask = mask,
        };
        return mode switch
        {
            BlendMode.Opaque => new VkPipelineColorBlendAttachmentState { BlendEnable = false, ColorWriteMask = mask },
            BlendMode.Alpha => State(VkBlendFactor.SrcAlpha, VkBlendFactor.OneMinusSrcAlpha, VkBlendFactor.One, VkBlendFactor.OneMinusSrcAlpha),
            BlendMode.Additive => State(VkBlendFactor.One, VkBlendFactor.One, VkBlendFactor.Zero, VkBlendFactor.One),
            BlendMode.Multiply => State(VkBlendFactor.DstColor, VkBlendFactor.OneMinusSrcAlpha, VkBlendFactor.Zero, VkBlendFactor.One),
            BlendMode.Screen => State(VkBlendFactor.One, VkBlendFactor.OneMinusSrcColor, VkBlendFactor.Zero, VkBlendFactor.One),
            BlendMode.DualSourceLcd => State(VkBlendFactor.One, VkBlendFactor.OneMinusSrc1Color, VkBlendFactor.One, VkBlendFactor.OneMinusSrc1Alpha),
            _ => State(VkBlendFactor.One, VkBlendFactor.OneMinusSrcAlpha, VkBlendFactor.One, VkBlendFactor.OneMinusSrcAlpha),
        };
    }

    public void Dispose()
    {
        foreach (var pipeline in _pipelines.Values)
            _device.Device.DestroyPipeline(pipeline.Handle, null);
        _pipelines.Clear();
        _device.Device.DestroyPipelineLayout(Layout, null);
    }
}
