using System.Numerics;
using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Statventure.Core;
using Statventure.Graphics;

namespace Statventure.Canvas;

public sealed partial class Canvas
{
    const VkFormat LayerFormat = VkFormat.R8G8B8A8UNorm;
    const int Bucket = 32;

    [StructLayout(LayoutKind.Sequential)]
    struct CanvasPush
    {
        public ulong Primitives, Gradients, Matrices;
        public Vector2 TargetSize, TargetOrigin;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BlurPush
    {
        public Vector4 SourceRect;
        public Vector2 TexelStep;
        public float Sigma;
        public uint Texture;
        public uint AlphaOnly;
        public uint MirrorEdges;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FilterPush
    {
        public Vector4 SourceRect;
        public Matrix4x4 Matrix;
        public Vector4 OffsetOrColor;
        public Vector2 ShadowShift;
        public uint Texture;
        public uint ShadowTexture;
        public uint Mode;
    }

    readonly Dictionary<(int, int), Stack<GpuImage>> _pool = [];
    readonly Dictionary<GpuImage, long> _lastUse = [];
    readonly List<GpuImage> _inUse = [];
    readonly Dictionary<GpuImage, (int Width, int Height)> _regions = [];
    CanvasPush _push;
    long _renders;

    /// <summary>Gets a pooled layer image at least this big (the used region is remembered).</summary>
    GpuImage AcquireTarget(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var key = ((width + Bucket - 1) / Bucket * Bucket, (height + Bucket - 1) / Bucket * Bucket);
        GpuImage? image = null;
        lock (_pool)
            if (_pool.TryGetValue(key, out var stack) && stack.Count > 0)
                image = stack.Pop();
        image ??= GpuImage.CreateRenderTarget(_device, key.Item1, key.Item2, LayerFormat);
        _inUse.Add(image);
        _regions[image] = (width, height);
        return image;
    }

    /// <summary>
    /// Gets a pooled image of exactly this size (a tile: a repeat sampler wraps the whole image, so
    /// it can't be a region of a bigger one); pooled and released as the others.
    /// </summary>
    GpuImage AcquireExactTarget(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        GpuImage? image = null;
        lock (_pool)
            if (_pool.TryGetValue((width, height), out var stack) && stack.Count > 0)
                image = stack.Pop();
        image ??= GpuImage.CreateRenderTarget(_device, width, height, LayerFormat);
        _inUse.Add(image);
        _regions[image] = (width, height);
        return image;
    }

    /// <summary>Layer images of a recording that was never rendered go straight back to the pool (no GPU work used them).</summary>
    void ReturnUnrendered()
    {
        if (_inUse.Count == 0)
            return;
        lock (_pool)
            foreach (var image in _inUse)
            {
                var key = (image.Width, image.Height);
                if (!_pool.TryGetValue(key, out var stack))
                    _pool[key] = stack = new Stack<GpuImage>();
                stack.Push(image);
            }
        _inUse.Clear();
        _regions.Clear();
    }

    (int Width, int Height) Region(GpuImage image) => _regions.TryGetValue(image, out var r) ? r : (image.Width, image.Height);

    Vector4 RegionUv(GpuImage image)
    {
        var (w, h) = Region(image);
        return new Vector4(0, 0, w / (float)image.Width, h / (float)image.Height);
    }

    /// <summary>
    /// Draws everything recorded since <see cref="Begin"/> into <paramref name="target"/> (which must
    /// be <see cref="PixelWidth"/>×<see cref="PixelHeight"/>), within the frame's command list.
    /// </summary>
    public void Render(Frame frame, GpuImage target, LoadAction load = LoadAction.Clear, Color? clear = null)
    {
        if (_layers.Count > 0)
            throw new InvalidOperationException("PushLayer without PopLayer");
        _recording = false;
        _renders++;

        _atlas?.Upload(frame);
        _lcdAtlas?.Upload(frame);
        _push.Primitives = frame.Upload<CanvasPrimitive>(_primitives.Count > 0 ? CollectionsMarshal.AsSpan(_primitives) : [default]);
        _push.Gradients = frame.Upload<GpuGradient>(_gradients.Count > 0 ? CollectionsMarshal.AsSpan(_gradients) : [default]);
        _push.Matrices = frame.Upload<GpuColorMatrix>(_matrices.Count > 0 ? CollectionsMarshal.AsSpan(_matrices) : [default]);

        var c = clear ?? Color.Transparent;
        RenderLayer(frame.Commands, _root, target, Vector2.Zero, load, c.Premultiplied);

        // Layer images go back to the pool once this frame's GPU work is done.
        foreach (var image in _inUse)
        {
            var released = image;
            _lastUse[released] = _renders;
            _device.Release(() =>
            {
                lock (_pool)
                {
                    var key = (released.Width, released.Height);
                    if (!_pool.TryGetValue(key, out var stack))
                        _pool[key] = stack = new Stack<GpuImage>();
                    stack.Push(released);
                }
            });
        }
        _inUse.Clear();
        _regions.Clear();
        TrimPool();
    }

    void TrimPool()
    {
        lock (_pool)
            foreach (var stack in _pool.Values)
            {
                if (stack.Count == 0)
                    continue;
                var keep = stack.Where(image => _renders - _lastUse.GetValueOrDefault(image) < 300).ToList();
                foreach (var image in stack.Except(keep))
                {
                    _lastUse.Remove(image);
                    image.Dispose();
                }
                stack.Clear();
                foreach (var image in Enumerable.Reverse(keep))
                    stack.Push(image);
            }
    }

    Pipeline CanvasPipeline(VkFormat format, BlendMode blend) => _device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "canvas.vert",
        FragmentShader = blend == BlendMode.DualSourceLcd ? "canvas_lcd.frag" : "canvas.frag",
        Topology = VkPrimitiveTopology.TriangleStrip,
        Blend = blend,
        ColorFormats = [format],
    });

    Pipeline PassPipeline(string fragment) => _device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "fullscreen.vert",
        FragmentShader = fragment,
        Blend = BlendMode.Opaque,
        ColorFormats = [LayerFormat],
    });

    void RenderLayer(CommandList commands, Layer layer, GpuImage target, Vector2 origin, LoadAction load, Vector4 clear)
    {
        // Children first: each is drawn into its own image before this layer's pass begins (those
        // that start from what's under them are drawn where they come in the pass).
        foreach (var command in layer.Commands)
            if (command is { Type: CommandType.Child, Child: { Options.StartsFromBackdrop: false } child })
            {
                RenderLayer(commands, child, child.Content!, child.PixelBounds.Position, LoadAction.Clear, Vector4.Zero);
                ApplyFilters(commands, child);
            }

        commands.BeginRendering(new ColorAttachment(target, load, clear));
        foreach (var command in layer.Commands)
        {
            if (command.Type == CommandType.Draw)
            {
                commands.BindPipeline(CanvasPipeline(target.Format, command.Blend));
                _push.TargetSize = new Vector2(target.Width, target.Height);
                _push.TargetOrigin = origin;
                commands.Push(_push);
                commands.Draw(4, command.Count, 0, command.Start);
            }
            else if (command is { Type: CommandType.Backdrop, Backdrop: { } backdrop })
            {
                commands.EndRendering();
                BlurBackdrop(commands, target, origin, backdrop);
                commands.BeginRendering(new ColorAttachment(target, LoadAction.Load));
            }
            else if (command is { Type: CommandType.Child, Child: { Options.StartsFromBackdrop: true } child })
            {
                commands.EndRendering();
                CopyBackdrop(commands, target, origin, child);
                RenderLayer(commands, child, child.Content!, child.PixelBounds.Position, LoadAction.Load, Vector4.Zero);
                ApplyFilters(commands, child);
                commands.BeginRendering(new ColorAttachment(target, LoadAction.Load));
            }
        }
        commands.EndRendering();
    }

    /// <summary>Starts a layer as a copy of what has been drawn under it so far (transparent where its target doesn't reach).</summary>
    void CopyBackdrop(CommandList commands, GpuImage target, Vector2 origin, Layer child)
    {
        var content = child.Content!;
        commands.BeginRendering(new ColorAttachment(content, LoadAction.Clear, Vector4.Zero));
        commands.EndRendering();
        var (targetWidth, targetHeight) = Region(target);
        var bounds = child.PixelBounds;
        var x0 = Math.Max((int)(bounds.X - origin.X), 0);
        var y0 = Math.Max((int)(bounds.Y - origin.Y), 0);
        var x1 = Math.Min((int)(bounds.Right - origin.X), targetWidth);
        var y1 = Math.Min((int)(bounds.Bottom - origin.Y), targetHeight);
        if (x1 <= x0 || y1 <= y0)
            return;
        commands.Transition(target, VkImageLayout.TransferSrcOptimal);
        commands.Transition(content, VkImageLayout.TransferDstOptimal);
        commands.CopyImage(target, content, x0, y0, x1 - x0, y1 - y0, x0 - (int)(bounds.X - origin.X), y0 - (int)(bounds.Y - origin.Y));
    }

    void BlurBackdrop(CommandList commands, GpuImage target, Vector2 origin, Backdrop backdrop)
    {
        var bounds = backdrop.PixelBounds;
        var x = (int)(bounds.X - origin.X);
        var y = (int)(bounds.Y - origin.Y);
        var w = Math.Min((int)bounds.Width, target.Width - x);
        var h = Math.Min((int)bounds.Height, target.Height - y);
        var copy = AcquireTarget(w, h);
        commands.Transition(target, VkImageLayout.TransferSrcOptimal);
        commands.Transition(copy, VkImageLayout.TransferDstOptimal);
        commands.CopyImage(target, copy, x, y, w, h);
        var temp = AcquireTarget(w, h);
        // Chromium mirrors the backdrop at its edges, so they don't darken.
        Blur(commands, copy, temp, backdrop.Result, backdrop.Sigma, alphaOnly: false, mirror: true);
        commands.Transition(backdrop.Result, VkImageLayout.ShaderReadOnlyOptimal);
    }

    /// <summary>A separable Gaussian blur: source → temp (horizontal) → destination (vertical).</summary>
    void Blur(CommandList commands, GpuImage source, GpuImage temp, GpuImage destination, float sigma, bool alphaOnly, bool mirror = false)
    {
        if (sigma < 0.01f)
        {
            Pass(commands, source, destination, new FilterPush { Mode = 0 });
            return;
        }
        var (w, h) = Region(source);
        _regions[temp] = (w, h);
        _regions[destination] = (w, h);
        BlurPass(commands, source, temp, new Vector2(1f / source.Width, 0), sigma, alphaOnly, mirror);
        BlurPass(commands, temp, destination, new Vector2(0, 1f / temp.Height), sigma, alphaOnly, mirror);
    }

    void BlurPass(CommandList commands, GpuImage source, GpuImage destination, Vector2 step, float sigma, bool alphaOnly, bool mirror)
    {
        commands.Transition(source, VkImageLayout.ShaderReadOnlyOptimal);
        var (w, h) = Region(source);
        commands.BeginRendering(new ColorAttachment(destination, LoadAction.Clear));
        commands.SetViewport(0, 0, w, h);
        commands.SetScissor(0, 0, w, h);
        commands.BindPipeline(PassPipeline("blur.frag"));
        commands.Push(new BlurPush
        {
            SourceRect = RegionUv(source),
            TexelStep = step,
            Sigma = MathF.Min(sigma, 40f),
            Texture = (uint)source.BindlessIndex,
            AlphaOnly = alphaOnly ? 1u : 0u,
            MirrorEdges = mirror ? 1u : 0u,
        });
        commands.Draw(3);
        commands.EndRendering();
    }

    void Pass(CommandList commands, GpuImage source, GpuImage destination, FilterPush push)
    {
        commands.Transition(source, VkImageLayout.ShaderReadOnlyOptimal);
        var (w, h) = Region(source);
        _regions[destination] = (w, h);
        commands.BeginRendering(new ColorAttachment(destination, LoadAction.Clear));
        commands.SetViewport(0, 0, w, h);
        commands.SetScissor(0, 0, w, h);
        commands.BindPipeline(PassPipeline("filter.frag"));
        push.SourceRect = RegionUv(source);
        push.Texture = (uint)source.BindlessIndex;
        commands.Push(push);
        commands.Draw(3);
        commands.EndRendering();
    }

    void ApplyFilters(CommandList commands, Layer layer)
    {
        var current = layer.Content!;
        for (var i = 0; i < layer.PassFilters.Length; i++)
        {
            var filter = layer.PassFilters[i];
            var (w, h) = Region(current);
            var destination = i == layer.PassFilters.Length - 1 ? layer.Output! : AcquireTarget(w, h);
            switch (filter.Kind)
            {
                case FilterKind.Blur:
                    Blur(commands, current, AcquireTarget(w, h), destination, filter.Amount * Scale, alphaOnly: false);
                    break;
                case FilterKind.DropShadow:
                    var shadow = AcquireTarget(w, h);
                    // The drop-shadow blur length is the standard deviation (unlike box-shadow's, which is twice it).
                    Blur(commands, current, AcquireTarget(w, h), shadow, filter.Amount * Scale, alphaOnly: true);
                    commands.Transition(shadow, VkImageLayout.ShaderReadOnlyOptimal);
                    Pass(commands, current, destination, new FilterPush
                    {
                        Mode = 2,
                        ShadowTexture = (uint)shadow.BindlessIndex,
                        OffsetOrColor = filter.Color.Premultiplied,
                        ShadowShift = filter.Offset * Scale / new Vector2(current.Width, current.Height),
                    });
                    break;
                default:
                    var matrix = ColorMatrix.For(filter);
                    Pass(commands, current, destination, new FilterPush { Mode = 1, Matrix = matrix.M, OffsetOrColor = matrix.Offset });
                    break;
            }
            current = destination;
        }
        commands.Transition(layer.Output!, VkImageLayout.ShaderReadOnlyOptimal);
    }

    public void Dispose()
    {
        _device.WaitIdle();
        _atlas?.Dispose();
        _lcdAtlas?.Dispose();
        lock (_pool)
        {
            foreach (var stack in _pool.Values)
                foreach (var image in stack)
                    image.Dispose();
            _pool.Clear();
        }
    }
}
