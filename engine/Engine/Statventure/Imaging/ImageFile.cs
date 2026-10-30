using Brutal.StbApi;

namespace Statventure.Imaging;

/// <summary>An image in CPU memory: tightly packed 8-bit RGBA pixels, top row first.</summary>
public sealed class RgbaImage
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }

    public RgbaImage(int width, int height, byte[]? pixels = null)
    {
        Width = width;
        Height = height;
        Pixels = pixels ?? new byte[width * height * 4];
        if (Pixels.Length != width * height * 4)
            throw new ArgumentException($"expected {width * height * 4} bytes, got {Pixels.Length}");
    }

    public (byte R, byte G, byte B, byte A) this[int x, int y]
    {
        get
        {
            var i = (y * Width + x) * 4;
            return (Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
        }
        set
        {
            var i = (y * Width + x) * 4;
            (Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]) = value;
        }
    }
}

/// <summary>Reads image files (PNG, JPEG, TGA, BMP through stb_image) and writes PNG.</summary>
public static class ImageFile
{
    public static RgbaImage Load(string path)
    {
        var image = Stb.Load(path, 4);
        if (image.IsNull())
            throw new InvalidDataException("could not read image " + path);
        try
        {
            return new RgbaImage(image.Width, image.Height, image.AsSpan().ToArray());
        }
        finally
        {
            image.Dispose();
        }
    }

    public static RgbaImage Load(ReadOnlySpan<byte> encoded)
    {
        var image = Stb.LoadFromMemory(encoded, 4);
        if (image.IsNull())
            throw new InvalidDataException("could not decode image data");
        try
        {
            return new RgbaImage(image.Width, image.Height, image.AsSpan().ToArray());
        }
        finally
        {
            image.Dispose();
        }
    }

    public static void SavePng(string path, RgbaImage image)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        if (!Stb.WritePng(path, image.Width, image.Height, 4, image.Pixels))
            throw new IOException("could not write " + path);
    }
}
