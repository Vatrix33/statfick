using System.Numerics;
using System.Runtime.InteropServices;
using Statventure.Core;
using Statventure.Graphics;

namespace Statventure.Canvas;

[StructLayout(LayoutKind.Sequential)]
internal struct CanvasPrimitive
{
    public Vector2 X0, X1, X2;
    public Vector2 Size;
    public Vector4 RadiiX, RadiiY;
    public Vector4 Border;
    public Vector4 Color;
    public Vector4 BorderColor;
    public Vector4 Uv;
    public Vector4 Clip;
    public Vector4 ClipRadii;
    public Vector4 Params;
    public uint Kind, Texture, Sampler, Brush;
    public uint ColorMatrix, Flags, Pad0, Pad1;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GpuGradient
{
    public uint Kind, Count, Repeating, Pad;
    public Vector4 Geometry;
    public fixed float Colors[64];
    public fixed float Offsets[16];
}

[StructLayout(LayoutKind.Sequential)]
internal struct GpuColorMatrix
{
    public Matrix4x4 M;
    public Vector4 Offset;
}

/// <summary>
/// The engine's 2D renderer: rounded boxes with borders and gradients, box shadows, images, glyph
/// and path coverage, clips, transforms, and layers with opacity, filters and blend modes (the CSS
/// painting model). Drawing is recorded in logical units between <see cref="Begin"/> and
/// <see cref="Render"/>; everything goes to the GPU in a few instanced draws.
/// </summary>
public sealed partial class Canvas : IDisposable
{
    internal const uint NoIndex = uint.MaxValue;
    internal const uint KindBox = 0, KindShadow = 1, KindInsetShadow = 2, KindImage = 3, KindCoverage = 4, KindComposite = 5, KindCompositeAlpha = 6;
    const uint FlagDashed = 1;

    readonly GpuDevice _device;
    readonly List<CanvasPrimitive> _primitives = [];
    readonly List<GpuGradient> _gradients = [];
    readonly List<GpuColorMatrix> _matrices = [];
    readonly Dictionary<Brush, uint> _gradientIndex = [];
    readonly Stack<Matrix3x2> _transforms = new();
    readonly Stack<(Vector4 Rect, Vector4 Radii)> _clips = new();
    readonly Stack<Layer> _layers = new();
    readonly Stack<bool> _lcdTextStates = new();
    /// <summary>Text drawn now may use LCD antialiasing (the root, or layers that start from their backdrop).</summary>
    bool _lcdText = true;
    bool? _textLcd;

    /// <summary>
    /// Whether text drawn from here on uses LCD antialiasing, decided by the caller (the UI painter
    /// follows Chromium's layerization); null leaves it to the layers (LCD at the root and in layers
    /// that start from their backdrop).
    /// </summary>
    public bool? TextLcd
    {
        get => _textLcd;
        set => _textLcd = value;
    }
    readonly List<Layer> _allLayers = [];
    Matrix3x2 _transform = Matrix3x2.Identity;
    Vector4 _clip;
    Vector4 _clipRadii;
    Layer _root = null!;
    Layer _current = null!;
    bool _recording;

    public int PixelWidth { get; private set; }
    public int PixelHeight { get; private set; }
    /// <summary>Pixels per logical unit.</summary>
    public float Scale { get; private set; } = 1f;
    /// <summary>Blend mode for the next draws (mix-blend-mode on single shapes).</summary>
    public BlendMode Blend { get; set; } = BlendMode.Premultiplied;
    /// <summary>A colour filter applied to each following draw (cheap CSS filters such as brightness).</summary>
    public ColorMatrix? ColorFilter { get; set; }
    public Matrix3x2 Transform => _transform;
    public int PrimitiveCount => _primitives.Count;

    public Canvas(GpuDevice device)
    {
        _device = device;
    }

    internal enum CommandType
    {
        Draw,
        Child,
        Backdrop,
    }

    internal sealed class Command
    {
        public CommandType Type;
        public int Start, Count;
        public BlendMode Blend;
        public Layer? Child;
        public Backdrop? Backdrop;
    }

    internal sealed class Backdrop
    {
        public required RectF PixelBounds;
        public required float Sigma;
        public required GpuImage Result;
    }

    internal sealed class Layer
    {
        public LayerOptions Options = new();
        public RectF PixelBounds;
        public readonly List<Command> Commands = [];
        public GpuImage? Content;
        public GpuImage? Output;
        public Filter[] PassFilters = [];
    }

    /// <summary>Starts recording a frame for a target of this many pixels, at this many pixels per logical unit.</summary>
    public void Begin(int pixelWidth, int pixelHeight, float scale = 1f)
    {
        ReturnUnrendered();
        _primitives.Clear();
        _gradients.Clear();
        _matrices.Clear();
        _gradientIndex.Clear();
        _transforms.Clear();
        _clips.Clear();
        _layers.Clear();
        _lcdTextStates.Clear();
        _lcdText = true;
        _textLcd = null;
        _allLayers.Clear();
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        Scale = scale;
        _transform = Matrix3x2.Identity;
        _clip = new Vector4(0, 0, pixelWidth, pixelHeight);
        _clipRadii = Vector4.Zero;
        Blend = BlendMode.Premultiplied;
        ColorFilter = null;
        _root = new Layer { PixelBounds = new RectF(0, 0, pixelWidth, pixelHeight) };
        _current = _root;
        _recording = true;
    }

    // ---------- State ----------

    public void PushTransform(Matrix3x2 transform)
    {
        _transforms.Push(_transform);
        _transform = transform * _transform;
    }

    public void PushTranslation(float x, float y) => PushTransform(Matrix3x2.CreateTranslation(x, y));

    public void PopTransform() => _transform = _transforms.Pop();

    /// <summary>Clips the following draws to a (rounded) rectangle, intersected with the current clip.</summary>
    public void PushClip(RectF rect, CornerRadii radii = default)
    {
        _clips.Push((_clip, _clipRadii));
        var pixels = rect.Transform(_transform * Matrix3x2.CreateScale(Scale));
        var current = RectF.FromEdges(_clip.X, _clip.Y, _clip.Z, _clip.W);
        var clipped = current.Intersect(pixels);
        _clip = new Vector4(clipped.Left, clipped.Top, clipped.Right, Math.Max(clipped.Bottom, clipped.Top));
        if (!radii.IsZero)
        {
            var fitted = radii.Fit(rect.Size).Scale(Scale * TransformScale());
            _clipRadii = new Vector4(fitted.TopLeft.X, fitted.TopRight.X, fitted.BottomRight.X, fitted.BottomLeft.X);
        }
        else if (clipped != current)
            _clipRadii = Vector4.Zero; // a square clip inside a rounded one: the corners of the smaller rectangle are square
    }

    public void PopClip() => (_clip, _clipRadii) = _clips.Pop();

    float TransformScale() => MathF.Sqrt(MathF.Abs(_transform.M11 * _transform.M22 - _transform.M12 * _transform.M21));

    /// <summary>Starts a group drawn into its own image (opacity, filters, blend mode); end it with <see cref="PopLayer"/>.</summary>
    public void PushLayer(LayerOptions options)
    {
        var bounds = options.Bounds is { } b ? b.Transform(_transform * Matrix3x2.CreateScale(Scale)) : new RectF(0, 0, PixelWidth, PixelHeight);
        // Room for what the filters spread out.
        float left = 0, top = 0, right = 0, bottom = 0;
        foreach (var filter in options.Filters)
        {
            if (filter.Kind == FilterKind.Blur)
            {
                var reach = filter.Amount * 3f * Scale;
                left += reach; top += reach; right += reach; bottom += reach;
            }
            else if (filter.Kind == FilterKind.DropShadow)
            {
                var reach = filter.Amount * 3f * Scale;
                left += reach + MathF.Max(0, -filter.Offset.X * Scale);
                right += reach + MathF.Max(0, filter.Offset.X * Scale);
                top += reach + MathF.Max(0, -filter.Offset.Y * Scale);
                bottom += reach + MathF.Max(0, filter.Offset.Y * Scale);
            }
        }
        bounds = bounds.Inflate(left, top, right, bottom).Intersect(new RectF(0, 0, PixelWidth, PixelHeight));
        bounds = RectF.FromEdges(MathF.Floor(bounds.Left), MathF.Floor(bounds.Top), MathF.Ceiling(bounds.Right), MathF.Ceiling(bounds.Bottom));
        var layer = new Layer { Options = options, PixelBounds = bounds };
        _lcdTextStates.Push(_lcdText);
        _lcdText &= options.StartsFromBackdrop;
        _layers.Push(_current);
        _current = layer;
        _allLayers.Add(layer);
        _transforms.Push(_transform);
        _clips.Push((_clip, _clipRadii));
    }

    readonly Stack<(ColorMatrix? Filter, BlendMode Blend)> _tileStates = new();

    /// <summary>
    /// Starts drawing a tile into a texture of its own, exactly <paramref name="pixelWidth"/> by
    /// <paramref name="pixelHeight"/> pixels, so a repeat sampler wraps it (a background layer's
    /// pattern): the draws until <see cref="PopTile"/> land in it, in its pixels (0,0 at its
    /// corner), with no colour filter or blend mode, grayscale text, nothing drawn on the target.
    /// </summary>
    public void PushTile(int pixelWidth, int pixelHeight)
    {
        var layer = new Layer { PixelBounds = new RectF(0, 0, Math.Max(1, pixelWidth), Math.Max(1, pixelHeight)) };
        _lcdTextStates.Push(_lcdText);
        _lcdText = false;
        _tileStates.Push((ColorFilter, Blend));
        ColorFilter = null;
        Blend = BlendMode.Premultiplied;
        _layers.Push(_current);
        _current = layer;
        _allLayers.Add(layer);
        _transforms.Push(_transform);
        _clips.Push((_clip, _clipRadii));
        _transform = Matrix3x2.CreateScale(1f / Scale);
        _clip = new Vector4(0, 0, layer.PixelBounds.Width, layer.PixelBounds.Height);
        _clipRadii = Vector4.Zero;
    }

    /// <summary>
    /// Ends a tile: its texture (premultiplied; draw it with <c>premultiplied: true</c>), drawn before
    /// whatever samples it later in this recording, and back to its pool once the frame is done.
    /// </summary>
    public GpuImage PopTile()
    {
        var layer = _current;
        _current = _layers.Pop();
        _lcdText = _lcdTextStates.Pop();
        _transform = _transforms.Pop();
        (_clip, _clipRadii) = _clips.Pop();
        (ColorFilter, Blend) = _tileStates.Pop();
        layer.Content = layer.Output = AcquireExactTarget((int)layer.PixelBounds.Width, (int)layer.PixelBounds.Height);
        _current.Commands.Add(new Command { Type = CommandType.Child, Child = layer });
        return layer.Content;
    }

    public void PopLayer()
    {
        var layer = _current;
        _current = _layers.Pop();
        _lcdText = _lcdTextStates.Pop();
        _transform = _transforms.Pop();
        (_clip, _clipRadii) = _clips.Pop();
        if (layer.PixelBounds.IsEmpty)
            return;

        // Filters that need passes run on the layer's image; trailing colour filters and the opacity
        // fold into the composite.
        var filters = layer.Options.Filters;
        var trailing = filters.Length;
        while (trailing > 0 && filters[trailing - 1].IsColorMatrix)
            trailing--;
        layer.PassFilters = filters[..trailing];
        var matrix = ColorMatrix.Identity;
        var hasMatrix = false;
        for (var i = trailing; i < filters.Length; i++)
        {
            matrix = ColorMatrix.Then(matrix, ColorMatrix.For(filters[i]));
            hasMatrix = true;
        }

        var width = (int)layer.PixelBounds.Width;
        var height = (int)layer.PixelBounds.Height;
        layer.Content = AcquireTarget(width, height);
        layer.Output = layer.PassFilters.Length > 0 ? AcquireTarget(width, height) : layer.Content;
        _current.Commands.Add(new Command { Type = CommandType.Child, Child = layer });

        var previousBlend = Blend;
        Blend = layer.Options.Blend;
        var primitive = PixelPrimitive(layer.PixelBounds, KindComposite);
        primitive.Texture = (uint)layer.Output.BindlessIndex;
        primitive.Uv = RegionUv(layer.Output);
        primitive.Color = new Vector4(1, 1, 1, layer.Options.Opacity);
        primitive.ColorMatrix = hasMatrix ? AddMatrix(ColorFilter is { } outer ? ColorMatrix.Then(matrix, outer) : matrix) : CurrentMatrix();
        Add(primitive);
        Blend = previousBlend;
    }

    /// <summary>
    /// Text drawn until <see cref="PopGrayscaleText"/> is antialiased in grayscale: it goes into
    /// a group Chromium draws as a transparent layer without a layer here (an isolation group of
    /// opacity 1 while something keeps opacity layers from LCD text).
    /// </summary>
    public void PushGrayscaleText()
    {
        _lcdTextStates.Push(_lcdText);
        _lcdText = false;
    }

    public void PopGrayscaleText() => _lcdText = _lcdTextStates.Pop();

    /// <summary>Uploads an RGBA8 image (straight alpha) as a texture the caller owns; draw it with its <see cref="GpuImage.BindlessIndex"/>.</summary>
    public GpuImage CreateTexture(int width, int height, ReadOnlySpan<byte> rgba) => GpuImage.CreateTexture(_device, width, height, rgba);

    // ---------- Drawing ----------

    public void FillRect(RectF rect, Color color) => DrawBox(rect, Brush.Solid(color), CornerRadii.Zero, Border.None);

    /// <summary>A box with a background (solid or gradient), rounded corners and a border, as CSS paints one.</summary>
    public void DrawBox(RectF rect, Brush? background, CornerRadii radii, Border border)
    {
        if (rect.IsEmpty || (background is null || (!background.IsGradient && background.Color.A <= 0)) && !border.IsVisible)
            return;
        var primitive = LocalPrimitive(rect, KindBox);
        SetRadii(ref primitive, radii.Fit(rect.Size));
        if (background is null)
            primitive.Color = Vector4.Zero;
        else if (background.IsGradient)
        {
            primitive.Brush = AddGradient(background);
            primitive.Color = Vector4.One;
        }
        else
            primitive.Color = background.Color.Premultiplied;
        if (border.IsVisible)
        {
            primitive.Border = border.Widths;
            primitive.BorderColor = border.Color.Premultiplied;
            if (border.Style == BorderStyle.Dashed)
            {
                var width = MathF.Max(MathF.Max(border.Top, border.Bottom), MathF.Max(border.Left, border.Right));
                primitive.Flags |= FlagDashed;
                primitive.Params = new Vector4(DashLength(width), DashGap(width), 0, 0);
            }
        }
        Add(primitive);
    }

    /// <summary>A box whose border sides differ in colour or style.</summary>
    public unsafe void DrawBox(RectF rect, Brush? background, CornerRadii radii, BorderSides sides)
    {
        if (sides.Uniform is { } uniform)
        {
            DrawBox(rect, background, radii, uniform);
            return;
        }
        if (rect.IsEmpty)
            return;
        var primitive = LocalPrimitive(rect, KindBox);
        SetRadii(ref primitive, radii.Fit(rect.Size));
        if (background is null)
            primitive.Color = Vector4.Zero;
        else if (background.IsGradient)
        {
            primitive.Brush = AddGradient(background);
            primitive.Color = Vector4.One;
        }
        else
            primitive.Color = background.Color.Premultiplied;
        float W(BorderSide side) => side.IsVisible ? side.Width : 0;
        primitive.Border = new Vector4(W(sides.Top), W(sides.Right), W(sides.Bottom), W(sides.Left));
        primitive.BorderColor = Vector4.One;
        // The side colours ride in the gradient buffer (kind 2): colours 0-3 are top, right, bottom,
        // left; the pad field has a bit per dashed side.
        var gradient = new GpuGradient { Kind = 2, Count = 4 };
        var dashed = 0u;
        var all = new[] { sides.Top, sides.Right, sides.Bottom, sides.Left };
        var widest = 0f;
        for (var i = 0; i < 4; i++)
        {
            var color = all[i].IsVisible ? all[i].Color.Premultiplied : Vector4.Zero;
            gradient.Colors[i * 4] = color.X;
            gradient.Colors[i * 4 + 1] = color.Y;
            gradient.Colors[i * 4 + 2] = color.Z;
            gradient.Colors[i * 4 + 3] = color.W;
            if (all[i].Style == BorderStyle.Dashed)
                dashed |= 1u << i;
            widest = MathF.Max(widest, W(all[i]));
        }
        gradient.Pad = dashed;
        _gradients.Add(gradient);
        primitive.Pad0 = (uint)(_gradients.Count - 1);
        if (dashed != 0)
            primitive.Params = new Vector4(DashLength(widest), DashGap(widest), 0, 0);
        Add(primitive);
    }

    /// <summary>The dash and gap of a dashed border of this width (as Chromium draws them).</summary>
    public static float DashLength(float width) => MathF.Max(width * 2f, 1f);
    public static float DashGap(float width) => MathF.Max(width, 1f);

    /// <summary>
    /// A CSS box-shadow for the border box <paramref name="rect"/>. Outer shadows are cut out where the
    /// box is; for inset shadows pass the padding box (inside the border) and its radii. Offset and
    /// spread are in the box's units; the blur goes to the target as Skia's blur mask filter takes it
    /// (Blink's shadows respect the transform): one sigma on the target for the whole transform, the
    /// sigma times SkMatrix::mapRadius's factor (at most 128 px), the same in every direction there
    /// (canvas_common.glsl, shadowBlur).
    /// </summary>
    public void DrawBoxShadow(RectF rect, CornerRadii radii, BoxShadow shadow)
    {
        if (shadow.Color.A <= 0 || rect.IsEmpty)
            return;
        var primitive = LocalPrimitive(rect, shadow.Inset ? KindInsetShadow : KindShadow);
        SetRadii(ref primitive, radii.Fit(rect.Size));
        primitive.Color = shadow.Color.Premultiplied;
        primitive.Params = new Vector4(shadow.Offset.X, shadow.Offset.Y, shadow.Spread, MathF.Max(shadow.Blur / 2f, 0f));
        Add(primitive);
    }

    /// <summary>
    /// Draws (part of) a texture into a rectangle, optionally rounded and tinted: with straight alpha,
    /// or <paramref name="premultiplied"/> (a tile from <see cref="PopTile"/>; the tint's alpha alone
    /// applies). A uv range beyond 0..1 with a repeat sampler repeats it.
    /// </summary>
    public void DrawImage(int textureIndex, RectF rect, RectF? uv = null, CornerRadii radii = default, Color? tint = null, SamplerKind sampler = SamplerKind.LinearClamp, bool premultiplied = false)
    {
        if (rect.IsEmpty)
            return;
        var primitive = LocalPrimitive(rect, premultiplied ? KindComposite : KindImage);
        SetRadii(ref primitive, radii.Fit(rect.Size));
        primitive.Texture = (uint)textureIndex;
        primitive.Sampler = (uint)sampler;
        var u = uv ?? new RectF(0, 0, 1, 1);
        primitive.Uv = new Vector4(u.Left, u.Top, u.Right, u.Bottom);
        primitive.Color = premultiplied ? new Vector4(1, 1, 1, (tint ?? Color.White).A) : (tint ?? Color.White).Premultiplied;
        Add(primitive);
    }

    public void DrawImage(GpuImage image, RectF rect, RectF? uv = null, CornerRadii radii = default, Color? tint = null, SamplerKind sampler = SamplerKind.LinearClamp, bool premultiplied = false) =>
        DrawImage(image.BindlessIndex, rect, uv, radii, tint, sampler, premultiplied);

    /// <summary>Paints a coverage mask (the red channel of a texture: glyphs, paths) with a brush.</summary>
    public void DrawCoverage(int textureIndex, RectF rect, RectF uv, Brush brush, SamplerKind sampler = SamplerKind.LinearClamp)
    {
        if (rect.IsEmpty)
            return;
        var primitive = LocalPrimitive(rect, KindCoverage);
        primitive.Texture = (uint)textureIndex;
        primitive.Sampler = (uint)sampler;
        primitive.Uv = new Vector4(uv.Left, uv.Top, uv.Right, uv.Bottom);
        if (brush.IsGradient)
        {
            primitive.Brush = AddGradient(brush);
            primitive.Color = Vector4.One;
        }
        else
            primitive.Color = brush.Color.Premultiplied;
        Add(primitive);
    }

    /// <summary>
    /// CSS backdrop-filter: blur: blurs what has been drawn so far under a (rounded) rectangle, then
    /// draws the blurred copy back inside it.
    /// </summary>
    public void DrawBackdropBlur(RectF rect, CornerRadii radii, float blurRadius)
    {
        if (rect.IsEmpty || blurRadius <= 0)
            return;
        var pixels = rect.Transform(_transform * Matrix3x2.CreateScale(Scale)).Intersect(_current.PixelBounds);
        pixels = RectF.FromEdges(MathF.Floor(pixels.Left), MathF.Floor(pixels.Top), MathF.Ceiling(pixels.Right), MathF.Ceiling(pixels.Bottom));
        if (pixels.IsEmpty)
            return;
        var result = AcquireTarget((int)pixels.Width, (int)pixels.Height);
        _current.Commands.Add(new Command { Type = CommandType.Backdrop, Backdrop = new Backdrop { PixelBounds = pixels, Sigma = blurRadius * Scale, Result = result } });
        var primitive = PixelPrimitive(pixels, KindComposite);
        primitive.Texture = (uint)result.BindlessIndex;
        primitive.Uv = RegionUv(result);
        primitive.Color = Vector4.One;
        // Round the copy with the element's shape: express the radii in the pixel box.
        var fitted = radii.Fit(rect.Size).Scale(Scale * TransformScale());
        SetRadii(ref primitive, fitted);
        Add(primitive);
    }

    // ---------- Recording helpers ----------

    CanvasPrimitive LocalPrimitive(RectF rect, uint kind)
    {
        var m = Matrix3x2.CreateTranslation(rect.X, rect.Y) * _transform * Matrix3x2.CreateScale(Scale);
        return new CanvasPrimitive
        {
            X0 = new Vector2(m.M11, m.M12),
            X1 = new Vector2(m.M21, m.M22),
            X2 = new Vector2(m.M31, m.M32),
            Size = rect.Size,
            Kind = kind,
            Brush = NoIndex,
            Pad0 = NoIndex,
            Texture = 0,
            Sampler = (uint)SamplerKind.LinearClamp,
            Clip = _clip,
            ClipRadii = _clipRadii,
            ColorMatrix = CurrentMatrix(),
            Uv = new Vector4(0, 0, 1, 1),
        };
    }

    CanvasPrimitive PixelPrimitive(RectF pixels, uint kind) => new()
    {
        X0 = new Vector2(1, 0),
        X1 = new Vector2(0, 1),
        X2 = pixels.Position,
        Size = pixels.Size,
        Kind = kind,
        Brush = NoIndex,
        Pad0 = NoIndex,
        Sampler = (uint)SamplerKind.LinearClamp,
        Clip = _clip,
        ClipRadii = _clipRadii,
        ColorMatrix = NoIndex,
        Uv = new Vector4(0, 0, 1, 1),
    };

    static void SetRadii(ref CanvasPrimitive primitive, CornerRadii radii)
    {
        primitive.RadiiX = new Vector4(radii.TopLeft.X, radii.TopRight.X, radii.BottomRight.X, radii.BottomLeft.X);
        primitive.RadiiY = new Vector4(radii.TopLeft.Y, radii.TopRight.Y, radii.BottomRight.Y, radii.BottomLeft.Y);
    }

    uint CurrentMatrix() => ColorFilter is { } filter ? AddMatrix(filter) : NoIndex;

    uint AddMatrix(ColorMatrix matrix)
    {
        _matrices.Add(new GpuColorMatrix { M = matrix.M, Offset = matrix.Offset });
        return (uint)(_matrices.Count - 1);
    }

    unsafe uint AddGradient(Brush brush)
    {
        if (_gradientIndex.TryGetValue(brush, out var existing))
            return existing;
        var stops = brush.Stops!;
        var gradient = new GpuGradient
        {
            Kind = brush.Kind == GradientKind.Radial ? 1u : 0u,
            Count = (uint)Math.Min(stops.Length, 16),
            Repeating = brush.Repeat ? 1u : 0u,
            Geometry = brush.Kind == GradientKind.Radial
                ? new Vector4(brush.Start.X, brush.Start.Y, brush.Radii.X, brush.Radii.Y)
                : new Vector4(brush.Start.X, brush.Start.Y, brush.End.X, brush.End.Y),
        };
        for (var i = 0; i < gradient.Count; i++)
        {
            var color = stops[i].Color.Premultiplied;
            gradient.Colors[i * 4] = color.X;
            gradient.Colors[i * 4 + 1] = color.Y;
            gradient.Colors[i * 4 + 2] = color.Z;
            gradient.Colors[i * 4 + 3] = color.W;
            gradient.Offsets[i] = stops[i].Offset;
        }
        _gradients.Add(gradient);
        var index = (uint)(_gradients.Count - 1);
        _gradientIndex[brush] = index;
        return index;
    }

    void Add(in CanvasPrimitive primitive)
    {
        if (!_recording)
            throw new InvalidOperationException("call Begin before drawing");
        var index = _primitives.Count;
        _primitives.Add(primitive);
        var commands = _current.Commands;
        if (commands.Count > 0 && commands[^1] is { Type: CommandType.Draw } last && last.Blend == Blend && last.Start + last.Count == index)
            last.Count++;
        else
            commands.Add(new Command { Type = CommandType.Draw, Start = index, Count = 1, Blend = Blend });
    }
}
