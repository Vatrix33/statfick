using Statventure.Graphics;
using Statventure.Imaging;

namespace Statventure.Canvas;

/// <summary>
/// A raster image as Chromium 152 keeps a decoded one for drawing: its pixels premultiplied
/// (<see cref="SkiaMipmap.Premultiply"/>) and, made the first time a draw needs them, the scaled-down copies cc's
/// software decode cache would make (<see cref="ImageDecodeScale"/>, <see cref="SkiaMipmap.ScalePixels"/>), each a
/// texture of its own. Drawn with <see cref="Canvas.DrawImage(CanvasImage, Core.RectF, Core.RectF?, CornerRadii, SamplerKind)"/>.
/// The textures are made on the device of the first canvas that draws the image.
/// </summary>
public sealed class CanvasImage : IDisposable
{
    readonly RgbaImage _premultiplied;
    readonly Lock _lock = new();
    readonly Dictionary<(ImageDecodeScale.RectI Source, int Width, int Height), GpuImage> _scaled = [];
    GpuDevice? _device;
    GpuImage? _original;

    /// <param name="straight">The image's pixels as decoded from its file (straight alpha).</param>
    public CanvasImage(RgbaImage straight)
    {
        _premultiplied = SkiaMipmap.Premultiply(straight);
    }

    public int Width => _premultiplied.Width;
    public int Height => _premultiplied.Height;

    /// <summary>The scaled copies made so far (tests).</summary>
    internal int ScaledCopies
    {
        get
        {
            lock (_lock)
                return _scaled.Count;
        }
    }

    /// <summary>The texture of the image itself.</summary>
    internal GpuImage Original(GpuDevice device)
    {
        lock (_lock)
        {
            Use(device);
            return _original ??= GpuImage.CreateTexture(device, Width, Height, _premultiplied.Pixels);
        }
    }

    /// <summary>The texture of <paramref name="source"/> scaled down to <paramref name="width"/> × <paramref name="height"/> (cc's kSubrectAndScale).</summary>
    internal GpuImage Scaled(GpuDevice device, ImageDecodeScale.RectI source, int width, int height)
    {
        lock (_lock)
        {
            Use(device);
            if (!_scaled.TryGetValue((source, width, height), out var texture))
            {
                var pixels = ScaledPixels(source, width, height);
                texture = GpuImage.CreateTexture(device, width, height, pixels.Pixels);
                _scaled[(source, width, height)] = texture;
            }
            return texture;
        }
    }

    /// <summary>The scaled copy's pixels (premultiplied): the source part cut out, then <see cref="SkiaMipmap.ScalePixels"/>.</summary>
    internal RgbaImage ScaledPixels(ImageDecodeScale.RectI source, int width, int height)
    {
        var part = _premultiplied;
        if (source != new ImageDecodeScale.RectI(0, 0, Width, Height))
        {
            part = new RgbaImage(source.Width, source.Height);
            for (var y = 0; y < source.Height; y++)
                Array.Copy(_premultiplied.Pixels, ((source.Y + y) * Width + source.X) * 4, part.Pixels, y * source.Width * 4, source.Width * 4);
        }
        return SkiaMipmap.ScalePixels(part, width, height);
    }

    void Use(GpuDevice device)
    {
        _device ??= device;
        if (_device != device)
            throw new InvalidOperationException("a canvas image is drawn on one device");
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _original?.Dispose();
            _original = null;
            foreach (var texture in _scaled.Values)
                texture.Dispose();
            _scaled.Clear();
        }
    }
}
