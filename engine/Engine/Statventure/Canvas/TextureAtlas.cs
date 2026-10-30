using Brutal.VulkanApi;
using Statventure.Graphics;

namespace Statventure.Canvas;

/// <summary>A rectangle in an atlas page, in pixels and in texture coordinates.</summary>
public readonly record struct AtlasEntry(int Page, int TextureIndex, int X, int Y, int Width, int Height, float U0, float V0, float U1, float V1);

/// <summary>
/// Single-channel (coverage) atlas pages for glyphs and rasterized paths. Rectangles are packed in
/// shelves; the CPU keeps a copy of each page and changed regions are copied to the GPU at the start
/// of the next canvas render. When every page is full the atlas is cleared (callers drop their
/// cached entries through <see cref="Generation"/>).
/// </summary>
public sealed class TextureAtlas : IDisposable
{
    const int Padding = 1;

    sealed class Page
    {
        public required GpuImage Image;
        public required byte[] Pixels;
        public readonly List<(int Y, int Height, int X)> Shelves = [];
        public int NextShelfY;
        public int DirtyX0 = int.MaxValue, DirtyY0 = int.MaxValue, DirtyX1, DirtyY1;
    }

    readonly GpuDevice _device;
    readonly List<Page> _pages = [];

    public int PageSize { get; }
    public int MaxPages { get; }
    /// <summary>Bytes per pixel: 1 (coverage) or 4 (RGBA, e.g. LCD glyph masks).</summary>
    public int BytesPerPixel { get; }
    /// <summary>Increases whenever the atlas is cleared; entries from older generations are invalid.</summary>
    public int Generation { get; private set; }
    public int PageCount => _pages.Count;

    public TextureAtlas(GpuDevice device, int pageSize = 2048, int maxPages = 4, int bytesPerPixel = 1)
    {
        _device = device;
        PageSize = pageSize;
        MaxPages = maxPages;
        BytesPerPixel = bytesPerPixel;
    }

    /// <summary>Stores a bitmap (tightly packed, <see cref="BytesPerPixel"/> per pixel) and returns where it went.</summary>
    public AtlasEntry Add(int width, int height, ReadOnlySpan<byte> pixels)
    {
        if (width > PageSize - 2 * Padding || height > PageSize - 2 * Padding)
            throw new ArgumentException($"{width}x{height} does not fit an atlas page of {PageSize}");
        if (!TryPlace(width, height, out var page, out var x, out var y))
        {
            if (_pages.Count >= MaxPages)
                Clear();
            AddPage();
            if (!TryPlace(width, height, out page, out x, out y))
                throw new InvalidOperationException("atlas placement failed");
        }
        var p = _pages[page];
        var bpp = BytesPerPixel;
        for (var row = 0; row < height; row++)
            pixels.Slice(row * width * bpp, width * bpp).CopyTo(p.Pixels.AsSpan(((y + row) * PageSize + x) * bpp, width * bpp));
        p.DirtyX0 = Math.Min(p.DirtyX0, x);
        p.DirtyY0 = Math.Min(p.DirtyY0, y);
        p.DirtyX1 = Math.Max(p.DirtyX1, x + width);
        p.DirtyY1 = Math.Max(p.DirtyY1, y + height);
        float s = PageSize;
        return new AtlasEntry(page, p.Image.BindlessIndex, x, y, width, height, x / s, y / s, (x + width) / s, (y + height) / s);
    }

    bool TryPlace(int width, int height, out int page, out int x, out int y)
    {
        for (page = 0; page < _pages.Count; page++)
        {
            var p = _pages[page];
            for (var i = 0; i < p.Shelves.Count; i++)
            {
                var (shelfY, shelfHeight, shelfX) = p.Shelves[i];
                if (height <= shelfHeight && height >= shelfHeight * 0.6f && shelfX + width + Padding <= PageSize)
                {
                    x = shelfX;
                    y = shelfY;
                    p.Shelves[i] = (shelfY, shelfHeight, shelfX + width + Padding);
                    return true;
                }
            }
            var newHeight = height + Padding;
            if (p.NextShelfY + newHeight <= PageSize)
            {
                x = Padding;
                y = p.NextShelfY + Padding;
                p.Shelves.Add((y, height, x + width + Padding));
                p.NextShelfY += newHeight + Padding;
                return true;
            }
        }
        x = y = 0;
        page = -1;
        return false;
    }

    void AddPage()
    {
        var image = GpuImage.Create(_device, new ImageDesc
        {
            Width = PageSize,
            Height = PageSize,
            Format = BytesPerPixel == 4 ? VkFormat.R8G8B8A8UNorm : VkFormat.R8UNorm,
            Usage = VkImageUsageFlags.SampledBit | VkImageUsageFlags.TransferDstBit | VkImageUsageFlags.TransferSrcBit,
        });
        var page = new Page { Image = image, Pixels = new byte[PageSize * PageSize * BytesPerPixel] };
        page.DirtyX0 = 0;
        page.DirtyY0 = 0;
        page.DirtyX1 = PageSize;
        page.DirtyY1 = PageSize;
        _pages.Add(page);
    }

    /// <summary>Forgets every entry (the pages are reused).</summary>
    public void Clear()
    {
        foreach (var page in _pages)
        {
            Array.Clear(page.Pixels);
            page.Shelves.Clear();
            page.NextShelfY = 0;
            page.DirtyX0 = 0;
            page.DirtyY0 = 0;
            page.DirtyX1 = PageSize;
            page.DirtyY1 = PageSize;
        }
        Generation++;
    }

    /// <summary>Copies changed regions to the GPU (outside any rendering pass).</summary>
    public void Upload(Frame frame)
    {
        foreach (var page in _pages)
        {
            if (page.DirtyX1 <= page.DirtyX0 || page.DirtyY1 <= page.DirtyY0)
                continue;
            int x = page.DirtyX0, y = page.DirtyY0, w = page.DirtyX1 - x, h = page.DirtyY1 - y;
            var bpp = BytesPerPixel;
            var slice = frame.Allocate<byte>(w * h * bpp);
            for (var row = 0; row < h; row++)
                page.Pixels.AsSpan(((y + row) * PageSize + x) * bpp, w * bpp).CopyTo(slice.Data.Slice(row * w * bpp, w * bpp));
            var commands = frame.Commands;
            commands.Transition(page.Image, VkImageLayout.TransferDstOptimal);
            commands.CopyBufferToImageRegion(slice.Buffer, page.Image, slice.Offset, x, y, w, h);
            commands.Transition(page.Image, VkImageLayout.ShaderReadOnlyOptimal);
            page.DirtyX0 = page.DirtyY0 = int.MaxValue;
            page.DirtyX1 = page.DirtyY1 = 0;
        }
    }

    public void Dispose()
    {
        foreach (var page in _pages)
            page.Image.Dispose();
        _pages.Clear();
    }
}
