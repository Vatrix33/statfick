using System.Numerics;
using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Platform;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Graphics;

[Collection("GPU")]
public class GpuDeviceTests(GpuFixture gpu)
{
    readonly GpuDevice _device = gpu.Device;

    [StructLayout(LayoutKind.Sequential)]
    struct ColorVertex(float x, float y, float r, float g, float b, float a)
    {
        public Vector2 Position = new(x, y);
        public Vector4 Color = new(r, g, b, a);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TexturePush
    {
        public uint Texture;
        public uint Sampler;
        public float Lod;
    }

    [Fact]
    public void TheDeviceStartsWithTheBindlessWhiteTexture()
    {
        Assert.False(string.IsNullOrWhiteSpace(_device.Name));
        Assert.Equal(0, _device.Bindless.White!.BindlessIndex);
        Assert.Equal([255, 255, 255, 255], _device.Bindless.White.ReadPixels());
    }

    [Fact]
    public void DataSurvivesAnUploadAndACopyBack()
    {
        var data = Enumerable.Range(0, 10_000).Select(i => i * 7).ToArray();
        using var gpuBuffer = GpuBuffer.CreateWithData<int>(_device, data, VkBufferUsageFlags.StorageBufferBit);
        using var readback = GpuBuffer.Create(_device, gpuBuffer.Size, 0, MemoryKind.Readback);
        _device.Immediate(commands => commands.CopyBuffer(gpuBuffer, readback, gpuBuffer.Size));
        var result = new int[data.Length];
        readback.Read<int>(result);
        Assert.Equal(data, result);
        Assert.NotEqual(0ul, gpuBuffer.Address);
    }

    [Fact]
    public void ARenderTargetClearsToTheGivenColour()
    {
        using var target = GpuImage.CreateRenderTarget(_device, 16, 8);
        _device.Immediate(commands =>
        {
            commands.BeginRendering(new ColorAttachment(target, LoadAction.Clear, new Vector4(1f, 0.5f, 0f, 1f)));
            commands.EndRendering();
        });
        var pixels = target.ReadPixels();
        for (var i = 0; i < pixels.Length; i += 4)
        {
            Assert.Equal(255, pixels[i]);
            Assert.InRange(pixels[i + 1], 127, 128);
            Assert.Equal(0, pixels[i + 2]);
            Assert.Equal(255, pixels[i + 3]);
        }
    }

    [Fact]
    public void ATriangleDrawnFromPulledVerticesMatchesItsReference()
    {
        using var target = GpuImage.CreateRenderTarget(_device, 64, 64);
        var pipeline = _device.Pipelines.Get(new GraphicsPipelineDesc { VertexShader = "test_color.vert", FragmentShader = "test_color.frag", Blend = BlendMode.Opaque });
        ColorVertex[] vertices =
        [
            new(0f, -0.8f, 1f, 0f, 0f, 1f),
            new(0.8f, 0.8f, 0f, 1f, 0f, 1f),
            new(-0.8f, 0.8f, 0f, 0f, 1f, 1f),
        ];
        using var buffer = GpuBuffer.CreateWithData<ColorVertex>(_device, vertices, VkBufferUsageFlags.StorageBufferBit);
        _device.Immediate(commands =>
        {
            commands.BeginRendering(new ColorAttachment(target, LoadAction.Clear, new Vector4(0f, 0f, 0f, 1f)));
            commands.BindPipeline(pipeline);
            commands.Push(buffer.Address);
            commands.Draw(3);
            commands.EndRendering();
        });
        Golden.Match("gpu-triangle", new RgbaImage(64, 64, target.ReadPixels()));
    }

    [Fact]
    public void TexturesAreSampledThroughTheBindlessTable()
    {
        // A 2x2 texture of four colours, drawn over 4x4 pixels with nearest sampling.
        byte[] pixels = [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255];
        using var texture = GpuImage.CreateTexture(_device, 2, 2, pixels);
        using var target = GpuImage.CreateRenderTarget(_device, 4, 4);
        var pipeline = _device.Pipelines.Get(new GraphicsPipelineDesc { VertexShader = "fullscreen.vert", FragmentShader = "test_texture.frag", Blend = BlendMode.Opaque });
        _device.Immediate(commands =>
        {
            commands.BeginRendering(new ColorAttachment(target, LoadAction.Clear));
            commands.BindPipeline(pipeline);
            commands.Push(new TexturePush { Texture = (uint)texture.BindlessIndex, Sampler = (uint)SamplerKind.NearestClamp, Lod = -1f });
            commands.Draw(3);
            commands.EndRendering();
        });
        var image = new RgbaImage(4, 4, target.ReadPixels());
        Assert.Equal((255, 0, 0, 255), image[0, 0]);
        Assert.Equal((0, 255, 0, 255), image[3, 0]);
        Assert.Equal((0, 0, 255, 255), image[0, 3]);
        Assert.Equal((255, 255, 255, 255), image[3, 3]);
        Assert.NotEqual(0, texture.BindlessIndex);
    }

    [Fact]
    public void MipmapsAreGeneratedByBlitting()
    {
        // Black and white stripes average to grey in the smallest mip level.
        var pixels = new byte[16 * 16 * 4];
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
            {
                var value = (byte)(x % 2 == 0 ? 255 : 0);
                var i = (y * 16 + x) * 4;
                (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = (value, value, value, 255);
            }
        using var texture = GpuImage.CreateTexture(_device, 16, 16, pixels, mipmaps: true);
        Assert.Equal(5, texture.MipLevels);
        using var target = GpuImage.CreateRenderTarget(_device, 2, 2);
        var pipeline = _device.Pipelines.Get(new GraphicsPipelineDesc { VertexShader = "fullscreen.vert", FragmentShader = "test_texture.frag", Blend = BlendMode.Opaque });
        _device.Immediate(commands =>
        {
            commands.BeginRendering(new ColorAttachment(target, LoadAction.Clear));
            commands.BindPipeline(pipeline);
            commands.Push(new TexturePush { Texture = (uint)texture.BindlessIndex, Sampler = (uint)SamplerKind.LinearClamp, Lod = 4f });
            commands.Draw(3);
            commands.EndRendering();
        });
        var image = new RgbaImage(2, 2, target.ReadPixels());
        Assert.InRange(image[0, 0].R, 120, 135);
    }

    [Fact]
    public void FramesUseTransientMemoryAndDestroyReleasedResourcesWhenTheGpuIsDone()
    {
        using var target = GpuImage.CreateRenderTarget(_device, 32, 32);
        var pipeline = _device.Pipelines.Get(new GraphicsPipelineDesc { VertexShader = "test_color.vert", FragmentShader = "test_color.frag", Blend = BlendMode.Opaque });
        for (var i = 0; i < 6; i++)
        {
            var frame = _device.BeginFrame();
            ReadOnlySpan<ColorVertex> vertices =
            [
                new(-1f, -1f, i / 6f, 0f, 0f, 1f),
                new(3f, -1f, i / 6f, 0f, 0f, 1f),
                new(-1f, 3f, i / 6f, 0f, 0f, 1f),
            ];
            var address = frame.Upload(vertices);
            // A resource released while the frame records must outlive the frame's submission.
            var scratch = GpuBuffer.Create(_device, 1024, VkBufferUsageFlags.StorageBufferBit, MemoryKind.Device);
            scratch.Dispose();
            Assert.True(_device.PendingReleaseCount > 0);
            frame.Commands.BeginRendering(new ColorAttachment(target, LoadAction.Clear));
            frame.Commands.BindPipeline(pipeline);
            frame.Commands.Push(address);
            frame.Commands.Draw(3);
            frame.Commands.EndRendering();
            _device.EndFrame(frame);
        }
        _device.WaitIdle();
        Assert.Equal(0, _device.PendingReleaseCount);
        var image = new RgbaImage(32, 32, target.ReadPixels());
        Assert.InRange(image[16, 16].R, 211, 214); // the last frame: 5/6
    }

    [Fact]
    public void PipelinesAreCachedByDescription()
    {
        var desc = new GraphicsPipelineDesc { VertexShader = "test_color.vert", FragmentShader = "test_color.frag", Blend = BlendMode.Additive };
        Assert.Same(_device.Pipelines.Get(desc), _device.Pipelines.Get(desc with { }));
        Assert.NotSame(_device.Pipelines.Get(desc), _device.Pipelines.Get(desc with { Blend = BlendMode.Screen }));
    }
}

/// <summary>
/// Tests that need a display: a real window and swapchain. In the "Window" collection, alone: GLFW is one for the
/// process, and a hidden window presenting beside this one stalled its frames under wlroots (Docs/Editor.md, "How it
/// works", step 3).
/// </summary>
[Collection("Window")]
public class SwapchainTests
{
    static bool HasDisplay => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));

    [Fact]
    public void AWindowPresentsFramesThroughItsSwapchain()
    {
        if (!HasDisplay)
            return;
        // The one test window left visible (Docs/Editor.md, "How it works", step 3): it presents five frames with vsync,
        // and a hidden window's surface, which no compositor shows, isn't paced by all of them (wlroots never lets the
        // second frame through: the test hangs), so hidden it would check a compositor's leniency, not the swapchain.
        using var window = Window.Create(new WindowOptions { Title = "Statventure swapchain test", Width = 320, Height = 200 });
        using var device = GpuDevice.Create(new GpuDeviceOptions { ApplicationName = "Statventure Tests" });
        using var swapchain = new Swapchain(device, window);
        var presented = 0;
        for (var i = 0; i < 20 && presented < 5; i++)
        {
            Window.PollEvents();
            var frame = device.BeginFrame();
            var image = swapchain.Acquire(frame);
            if (image is { } target)
            {
                frame.Commands.BeginRendering(new ColorAttachment(target.Image, LoadAction.Clear, new Vector4(0.2f, 0.4f, 0.6f, 1f)));
                frame.Commands.EndRendering();
                presented++;
            }
            device.EndFrame(frame, image);
        }
        device.WaitIdle();
        Assert.Equal(5, presented);
        Assert.True(swapchain.Width > 0 && swapchain.Height > 0);
        Assert.Equal(VkFormat.B8G8R8A8UNorm, swapchain.Format);
    }
}
