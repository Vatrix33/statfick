using System.Numerics;
using Statventure.Graphics;
using Brutal.VulkanApi;

namespace Statventure.Render3D;

/// <summary>
/// What a 3D view shows at a point: the model there (the group its items share, which the game gives
/// <see cref="Model.Draw"/> or <see cref="Terrain.Draw"/>, or the item itself when it has none), the
/// part (the item's <see cref="DrawItem.Part"/>: a model's node + 1) and how far it is (view depth).
/// </summary>
public readonly record struct Pick3D(object Owner, int Part, float Depth);

/// <summary>
/// A 3D view (Docs/Renderer-3D.md, "3D views in the UI"): a scene rendered into a picture the UI
/// shows in a <c>surface</c> element (<see cref="UI.SurfaceViews"/>). It renders only when something
/// changed since its last picture: one of its properties was set, its camera's settings differ, its
/// size or device scale changed, or <see cref="Invalidate"/> was called (for changes it can't see,
/// such as a list or a palette changed in place). Its background is a colour or, when
/// <see cref="Background"/> is null, transparent: edges, ink lines and the glow's halo then blend
/// over whatever the UI has behind it. It can say what's at a point of its last picture
/// (<see cref="Pick"/>).
/// </summary>
public sealed class View3D(GpuDevice device) : IDisposable
{
    readonly Renderer3D _renderer = new(device);
    RenderTarget3D? _target;
    GpuImage? _picture;
    readonly Records?[] _records = new Records?[2];
    int _nextRecords;
    bool _changed = true;
    CameraSettings _camera;
    (int Width, int Height, float Scale) _size;
    IReadOnlyList<DrawItem> _items = [];
    IReadOnlyList<Particle> _particles = [];
    Camera _view = new();
    Vector4? _background;
    SunLight _sun;
    CelStyle _style = CelStyle.Default;
    float _time;

    /// <summary>One picture's surface records (sample 0 per pixel) on the CPU side, and what its model ids stand for.</summary>
    sealed class Records(GpuBuffer buffer, int width, int height, object?[] owners, Frame frame)
    {
        public GpuBuffer Buffer { get; } = buffer;
        public int Width { get; } = width;
        public int Height { get; } = height;
        public object?[] Owners { get; } = owners;
        public Frame Frame { get; } = frame;
    }

    readonly record struct CameraSettings(Projection Projection, Vector3 Position, Vector3 Target, Vector3 Up, float VerticalFieldOfView, float OrthographicHeight, float Near, float Far)
    {
        public static CameraSettings Of(Camera c) => new(c.Projection, c.Position, c.Target, c.Up, c.VerticalFieldOfView, c.OrthographicHeight, c.Near, c.Far);
    }

    public IReadOnlyList<DrawItem> Items { get => _items; set => Set(ref _items, value); }
    public IReadOnlyList<Particle> Particles { get => _particles; set => Set(ref _particles, value); }
    /// <summary>The camera; its settings are compared each update, so moving it redraws the view.</summary>
    public Camera Camera { get => _view; set => Set(ref _view, value); }
    /// <summary>The background (linear, premultiplied); null: transparent.</summary>
    public Vector4? Background { get => _background; set => Set(ref _background, value); }
    public SunLight Sun { get => _sun; set => Set(ref _sun, value); }
    public CelStyle Style { get => _style; set => Set(ref _style, value); }
    /// <summary>The scene's clock in seconds (the water's waves).</summary>
    public float Time { get => _time; set => Set(ref _time, value); }

    void Set<T>(ref T field, T value)
    {
        field = value;
        _changed = true;
    }

    /// <summary>Renders the view again at its next update.</summary>
    public void Invalidate() => _changed = true;

    /// <summary>How many pictures the view has rendered.</summary>
    public int RenderCount { get; private set; }

    /// <summary>The bindless index of its picture (straight colours, sRGB-encoded, as the canvas takes images); null before the first.</summary>
    public int? Texture => _picture?.BindlessIndex;

    /// <summary>Tests: the picture itself.</summary>
    internal GpuImage? Picture => _picture;

    /// <summary>
    /// Renders a picture of <paramref name="width"/> × <paramref name="height"/> pixels at
    /// <paramref name="scale"/> (the device scale: ink widths) if anything changed since the last;
    /// returns whether it did.
    /// </summary>
    public bool Update(Frame frame, int width, int height, float scale)
    {
        if (width <= 0 || height <= 0)
            return false;
        var camera = CameraSettings.Of(_view);
        if (!_changed && camera == _camera && (width, height, scale) == _size)
            return false;
        if (_target is null || _target.Width != width || _target.Height != height)
        {
            // Released once the frames still drawing with them finish.
            _target?.Dispose();
            _picture?.Dispose();
            _target = new RenderTarget3D(device, width, height) { KeepRecords = true };
            _picture = GpuImage.CreateRenderTarget(device, width, height);
        }
        var commands = frame.Commands;
        var owners = _renderer.Render(frame, _target, _view, _items, _background ?? Vector4.Zero, _sun, _style, scale, _time, _particles);
        commands.Transition(_target.Resolved, VkImageLayout.ShaderReadOnlyOptimal);
        commands.BeginRendering(new ColorAttachment(_picture!, LoadAction.DontCare));
        commands.BindPipeline(PicturePipeline);
        commands.Push((uint)_target.Resolved.BindlessIndex);
        commands.Draw(3);
        commands.EndRendering();
        commands.Transition(_picture!, VkImageLayout.ShaderReadOnlyOptimal);
        // The records for picking, copied out alternately so the last finished copy stays readable.
        var size = (ulong)width * (ulong)height * 8;
        var slot = _nextRecords;
        _nextRecords = 1 - slot;
        var buffer = _records[slot]?.Buffer is { } old && old.Size >= size ? old : null;
        if (buffer is null)
        {
            _records[slot]?.Buffer.Dispose();
            buffer = GpuBuffer.Create(device, size, 0, MemoryKind.Readback);
        }
        commands.Transition(_target.Records, VkImageLayout.TransferSrcOptimal);
        commands.CopyImageToBuffer(_target.Records, buffer, 0);
        _records[slot] = new Records(buffer, width, height, owners, frame);
        _changed = false;
        _camera = camera;
        _size = (width, height, scale);
        RenderCount++;
        return true;
    }

    Pipeline? _picturePipeline;

    Pipeline PicturePipeline => _picturePipeline ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "fullscreen.vert",
        FragmentShader = "view_picture.frag",
        Blend = BlendMode.Opaque,
        ColorFormats = [VkFormat.R8G8B8A8UNorm],
    });

    /// <summary>
    /// What its latest submitted picture shows at <paramref name="pixel"/> (its pixels from the top
    /// left; waits for that picture's frame if the GPU hasn't finished it): null over the background,
    /// outside the picture, or before one.
    /// </summary>
    public Pick3D? Pick(Vector2 pixel)
    {
        var records = new[] { _records[1 - _nextRecords], _records[_nextRecords] }.FirstOrDefault(r => r is { Frame.SubmittedValue: > 0 });
        if (records is null)
            return null;
        int x = (int)MathF.Floor(pixel.X), y = (int)MathF.Floor(pixel.Y);
        if (x < 0 || y < 0 || x >= records.Width || y >= records.Height)
            return null;
        device.WaitFor(records.Frame.SubmittedValue);
        Span<uint> record = stackalloc uint[2];
        records.Buffer.Read(record, ((ulong)y * (ulong)records.Width + (ulong)x) * 8);
        var model = record[0] & 1023;
        if (model == 0 || records.Owners[model] is not { } owner)
            return null;
        return new Pick3D(owner, (int)((record[0] >> 10) & 255), BitConverter.UInt32BitsToSingle(record[1]));
    }

    public void Dispose()
    {
        _target?.Dispose();
        _picture?.Dispose();
        foreach (var records in _records)
            records?.Buffer.Dispose();
    }
}
