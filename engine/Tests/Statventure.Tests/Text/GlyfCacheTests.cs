using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Statventure.Text;
using Statventure.Text.Skia;

namespace Statventure.Tests.Text;

/// <summary>
/// Whether a face has TrueType outlines decides how Fontations outlines its glyphs at slight hinting (a glyf face through
/// skrifa's autohinter, a CFF face as FreeType loads it), and the answer is cached. It must be the face's own: FreeType
/// gives a freed face's memory to a face made after it, and when the cache was keyed by the face's address a CFF face
/// (STIX Two Text) made where a TrueType face (Lato) had been took Lato's answer and was autohinted, and the other way
/// round, so a screen's text drew one way or the other depending on what an earlier document had freed (found by GM1
/// step 8's game screens: Docs/Game/Foundation.md, "How it works", step 8).
/// </summary>
public sealed unsafe class GlyfCacheTests : IDisposable
{
    static readonly byte[] Lato = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "Fonts", "Lato-Regular.ttf"));
    static readonly byte[] Stix = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "Fonts", "STIXTwoText-Regular.otf"));

    readonly FontLibrary _fonts = new();

    public void Dispose()
    {
        _fonts.Dispose();
        RecyclingMemory.Release();
    }

    /// <summary>A face of the library's own that the test frees itself (not in the library's list).</summary>
    FontFace Make(byte[] data, string family)
    {
        lock (_fonts.FreeTypeLock)
            return new FontFace(_fonts, data, family, 400, 400, false);
    }

    static nint Address(FontFace face) => (nint)face.FtFace;

    [Fact]
    public void EachFaceHasItsOwnAnswer()
    {
        using var lato = Make(Lato, "Lato");
        using var stix = Make(Stix, "STIX Two Text");
        Assert.True(SkiaGlyphs.HasGlyf(lato));
        Assert.False(SkiaGlyphs.HasGlyf(stix));
        // Asked again: the same (from the cache).
        Assert.True(SkiaGlyphs.HasGlyf(lato));
        Assert.False(SkiaGlyphs.HasGlyf(stix));
    }

    /// <summary>
    /// A face made at a freed face's address, of the other kind, gives its own answer, in both orders. The reuse is made
    /// certain, not likely: the test's library gets a FreeType memory (<see cref="RecyclingMemory"/>) that gives a freed block
    /// back to the next request of its size, the last freed first, and FreeType keeps a TrueType face and an OpenType CFF
    /// face in the same face record (sfnt's TT_FaceRec), the last block it frees of a face and among the first it asks for
    /// making one. The test checks the address all the same.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AFaceMadeWhereAnotherWasFreedHasItsOwnAnswer(bool trueTypeFirst)
    {
        RecyclingMemory.Install(_fonts.FreeTypeLibrary);
        var (first, firstFamily, firstAnswer) = trueTypeFirst ? (Lato, "Lato", true) : (Stix, "STIX Two Text", false);
        var (second, secondFamily, secondAnswer) = trueTypeFirst ? (Stix, "STIX Two Text", false) : (Lato, "Lato", true);
        nint freed;
        using (var earlier = Make(first, firstFamily))
        {
            Assert.Equal(firstAnswer, SkiaGlyphs.HasGlyf(earlier));
            freed = Address(earlier);
        }
        using var later = Make(second, secondFamily);
        Assert.Equal(freed, Address(later));
        Assert.Equal(secondAnswer, SkiaGlyphs.HasGlyf(later));
    }

    /// <summary>
    /// A FreeType memory (FT_MemoryRec: user, alloc, free, realloc; a library's first field points to it) whose blocks are
    /// kept when freed and given back to the next request of the same size, the last freed first. Installed on one test's
    /// library only, from its own thread; blocks FreeType had before go back to its own free.
    /// </summary>
    static class RecyclingMemory
    {
        static readonly Lock Gate = new();
        static readonly Dictionary<nint, long> Ours = [];
        static readonly Dictionary<long, Stack<nint>> Freed = [];
        static delegate* unmanaged[Cdecl]<nint, nint, void> _originalFree;

        public static void Install(nint library)
        {
            var memory = *(nint**)library;
            lock (Gate)
                _originalFree = (delegate* unmanaged[Cdecl]<nint, nint, void>)memory[2];
            memory[1] = (nint)(delegate* unmanaged[Cdecl]<nint, CLong, nint>)&Alloc;
            memory[2] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&Free;
            memory[3] = (nint)(delegate* unmanaged[Cdecl]<nint, CLong, CLong, nint, nint>)&Realloc;
        }

        /// <summary>Frees every block kept (after the library is done).</summary>
        public static void Release()
        {
            lock (Gate)
            {
                foreach (var stack in Freed.Values)
                    foreach (var block in stack)
                        NativeMemory.Free((void*)block);
                Freed.Clear();
                Ours.Clear();
            }
        }

        static nint Take(long size)
        {
            lock (Gate)
            {
                if (Freed.TryGetValue(size, out var stack) && stack.Count > 0)
                    return stack.Pop();
                var block = (nint)NativeMemory.Alloc((nuint)Math.Max(size, 1));
                Ours[block] = size;
                return block;
            }
        }

        static void Give(nint memory, nint block)
        {
            if (block == 0)
                return;
            lock (Gate)
            {
                if (Ours.TryGetValue(block, out var size))
                {
                    if (!Freed.TryGetValue(size, out var stack))
                        Freed[size] = stack = new Stack<nint>();
                    stack.Push(block);
                    return;
                }
            }
            _originalFree(memory, block);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static nint Alloc(nint memory, CLong size) => Take(size.Value);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void Free(nint memory, nint block) => Give(memory, block);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static nint Realloc(nint memory, CLong currentSize, CLong newSize, nint block)
        {
            var moved = Take(newSize.Value);
            if (block != 0)
            {
                Buffer.MemoryCopy((void*)block, (void*)moved, newSize.Value, Math.Min(currentSize.Value, newSize.Value));
                Give(memory, block);
            }
            return moved;
        }
    }
}
