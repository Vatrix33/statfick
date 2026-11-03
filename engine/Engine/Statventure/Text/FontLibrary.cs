using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Statventure.Text;

/// <summary>A face chosen for a style, with what a browser would synthesize.</summary>
public readonly record struct ResolvedFont(FontFace Face, bool SyntheticBold, bool SyntheticOblique);

/// <summary>
/// The fonts a game uses, by family, weight and style, with CSS font matching: the weight order of
/// CSS Fonts 4 §5.2, italic before oblique before normal, bold synthesized for 600+ when only a face
/// of 500 or lighter exists, oblique synthesized when no italic face exists. Faces come from code
/// (<see cref="Add"/>, as a platform's fonts) or from stylesheets' @font-face rules
/// (<see cref="AcquireSheetFace"/>, Q24): a family some sheet face has is matched among the sheet faces
/// only, as Chromium matches a document's @font-face families before the platform's fonts.
/// </summary>
public sealed class FontLibrary : IDisposable
{
    readonly nint _freeType;
    readonly List<FontFace> _faces = [];
    readonly List<string> _fallbackFamilies = [];
    // Layout can run on several threads (documents, tests): lookups don't lock, misses do.
    readonly ConcurrentDictionary<FontKey, ResolvedFont?> _cache = new();
    readonly ConcurrentDictionary<(int Face, bool Bold, bool Oblique, int Codepoint), ResolvedFont?> _fallbacks = new();

    readonly record struct FontKey(string Family, int Weight, bool Italic)
    {
        public bool Equals(FontKey other) => Weight == other.Weight && Italic == other.Italic && string.Equals(Family, other.Family, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Family), Weight, Italic);
    }

    void ForgetResolved()
    {
        _cache.Clear();
        _fallbacks.Clear();
    }
    readonly Dictionary<string, string> _generic = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The family used when none of a style's families exist (the first added face's, if unset).</summary>
    public string? DefaultFamily { get; set; }

    /// <summary>Maps a generic or missing family name (serif, sans-serif, Georgia ...) to a loaded family.</summary>
    public void SetGeneric(string name, string family)
    {
        lock (_lock)
        {
            _generic[name] = family;
            ForgetResolved();
        }
    }

    /// <summary>The default family, matched for weight and style (unset: the first face added in code, else the first sheet face).</summary>
    public ResolvedFont? ResolveDefault(int weight, bool italic)
    {
        var family = DefaultFamily;
        if (family is null)
            lock (_lock)
                family = _faces.Count > 0 ? _faces[0].Family : _sheetFaces.Count > 0 ? _sheetFaces[0].Face.Family : null;
        return family is null ? null : Resolve(family, weight, italic);
    }

    /// <summary>The faces added in code (<see cref="Add"/>).</summary>
    public IReadOnlyList<FontFace> Faces => _faces;

    /// <summary>A face made from a stylesheet's @font-face rule, with the descriptors and data it was made from and how many sheets use it.</summary>
    sealed class SheetFace(FontFace face, byte[] hash)
    {
        public FontFace Face { get; } = face;
        public byte[] Hash { get; } = hash;
        public int Users = 1;

        public bool Is(byte[] hash, string family, int weight, int weightMax, bool italic) =>
            Face.Family == family && Face.Weight == weight && Face.WeightMax == Math.Max(weight, weightMax) && Face.Italic == italic && Hash.AsSpan().SequenceEqual(hash);
    }

    readonly List<SheetFace> _sheetFaces = [];
    // Sheet faces no sheet uses any more: out of matching, kept until the library goes (layout may still hold them).
    readonly List<FontFace> _retired = [];

    /// <summary>The faces stylesheets' @font-face rules added and still use, in the order they were first added.</summary>
    public IReadOnlyList<FontFace> SheetFaces
    {
        get
        {
            lock (_lock)
                return [.. _sheetFaces.Select(f => f.Face)];
        }
    }

    /// <summary>
    /// A face for a stylesheet's @font-face rule (Q24): made from <paramref name="data"/>, or the face already
    /// made from the same data and descriptors, which then has one more user. Each acquisition is given back
    /// with <see cref="ReleaseSheetFace"/> when the sheet goes. A family some sheet face has hides the faces
    /// added in code for that family. FreeType failing to read the data throws.
    /// </summary>
    public FontFace AcquireSheetFace(ReadOnlySpan<byte> data, string family, int weight = 400, bool italic = false, int? weightMax = null)
    {
        var max = weightMax ?? weight;
        var hash = SHA256.HashData(data);
        lock (_lock)
        {
            if (_sheetFaces.Find(f => f.Is(hash, family, weight, max, italic)) is { } known)
            {
                known.Users++;
                return known.Face;
            }
        }
        FontFace face;
        lock (FreeTypeLock)
            face = new FontFace(this, data, family, weight, max, italic);
        lock (_lock)
        {
            // Another thread may have made the same face meanwhile.
            if (_sheetFaces.Find(f => f.Is(hash, family, weight, max, italic)) is { } known)
            {
                known.Users++;
                _retired.Add(face);
                return known.Face;
            }
            _sheetFaces.Add(new SheetFace(face, hash));
            ForgetResolved();
        }
        return face;
    }

    /// <summary>Gives back one acquisition of a sheet face; when no sheet uses it, it is matched no more.</summary>
    public void ReleaseSheetFace(FontFace face)
    {
        lock (_lock)
        {
            var index = _sheetFaces.FindIndex(f => f.Face == face);
            if (index < 0)
                throw new InvalidOperationException($"{face} isn't a sheet face of this library");
            if (--_sheetFaces[index].Users > 0)
                return;
            _sheetFaces.RemoveAt(index);
            _retired.Add(face);
            ForgetResolved();
        }
    }

    public FontLibrary()
    {
        Core.NativeLibraries.Install();
        FreeType.Check(FreeType.InitFreeType(out _freeType), "FT_Init_FreeType");
    }

    internal nint FreeTypeLibrary => _freeType;

    /// <summary>
    /// Held while a face of this library is made or destroyed, and while its LCD filter is set and
    /// used: FreeType wants those one at a time per library (freetype.h, FT_Library and FT_Face),
    /// while glyphs of different faces may load at once (each face has its own lock,
    /// <see cref="FontFace.FreeTypeLock"/>).
    /// </summary>
    internal readonly Lock FreeTypeLock = new();

    /// <summary>The LCD filter last set on this library (FreeType keeps one per library); -1 before any.</summary>
    internal int LcdFilter = -1;

    /// <summary>Adds a font file; <paramref name="weightMax"/> gives a range ("font-weight: 500 600").</summary>
    public FontFace Add(ReadOnlySpan<byte> data, string family, int weight = 400, bool italic = false, int? weightMax = null)
    {
        FontFace face;
        lock (FreeTypeLock)
            face = new FontFace(this, data, family, weight, weightMax ?? weight, italic);
        lock (_lock)
        {
            _faces.Add(face);
            ForgetResolved();
        }
        return face;
    }

    public FontFace AddFile(string path, string family, int weight = 400, bool italic = false, int? weightMax = null) =>
        Add(File.ReadAllBytes(path), family, weight, italic, weightMax);

    /// <summary>Families tried, in order, for characters the requested family doesn't have.</summary>
    public void AddFallback(string family)
    {
        lock (_lock)
        {
            _fallbackFamilies.Add(family);
            ForgetResolved();
        }
    }

    /// <summary>The first family in the list that exists, matched for weight and style; null if none.</summary>
    public ResolvedFont? Resolve(IReadOnlyList<string> families, int weight, bool italic)
    {
        foreach (var family in families)
            if (Resolve(family, weight, italic) is { } resolved)
                return resolved;
        return null;
    }

    readonly Lock _lock = new();

    public ResolvedFont? Resolve(string family, int weight, bool italic)
    {
        if (_cache.TryGetValue(new FontKey(family, weight, italic), out var cached))
            return cached;
        lock (_lock)
            return ResolveLocked(family, weight, italic);
    }

    ResolvedFont? ResolveLocked(string family, int weight, bool italic)
    {
        var key = new FontKey(family, weight, italic);
        if (_cache.TryGetValue(key, out var cached))
            return cached;
        // A family a stylesheet's @font-face defines is matched among those faces only, the later rules first
        // (Chromium's FontFaceCache before the platform's fonts); else among the faces added in code.
        var candidates = Enumerable.Reverse(_sheetFaces).Select(f => f.Face).Where(f => string.Equals(f.Family, family, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0)
            candidates = _faces.Where(f => string.Equals(f.Family, family, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0 && _generic.TryGetValue(family, out var mapped) && !string.Equals(mapped, family, StringComparison.OrdinalIgnoreCase))
        {
            var viaGeneric = ResolveLocked(mapped, weight, italic);
            _cache[key] = viaGeneric;
            return viaGeneric;
        }
        ResolvedFont? result = null;
        if (candidates.Count > 0)
        {
            // Style first: the requested style if any face has it, otherwise the other one.
            var styled = candidates.Where(f => f.Italic == italic).ToList();
            if (styled.Count == 0)
                styled = candidates;
            var face = MatchWeight(styled, weight);
            var faceWeight = Math.Min(Math.Max(weight, face.Weight), face.WeightMax);
            result = new ResolvedFont(face, weight >= 600 && faceWeight <= 500, italic && !face.Italic);
        }
        _cache[key] = result;
        return result;
    }

    /// <summary>The weight matching order of CSS Fonts 4 §5.2.</summary>
    static FontFace MatchWeight(List<FontFace> faces, int desired)
    {
        var exact = faces.FirstOrDefault(f => desired >= f.Weight && desired <= f.WeightMax);
        if (exact is not null)
            return exact;
        IEnumerable<FontFace> heavier = faces.Where(f => f.Weight > desired).OrderBy(f => f.Weight);
        IEnumerable<FontFace> lighter = faces.Where(f => f.WeightMax < desired).OrderByDescending(f => f.WeightMax);
        IEnumerable<FontFace> order;
        if (desired >= 400 && desired <= 500)
            order = heavier.Where(f => f.Weight <= 500).Concat(lighter).Concat(heavier.Where(f => f.Weight > 500));
        else if (desired < 400)
            order = lighter.Concat(heavier);
        else
            order = heavier.Concat(lighter);
        return order.First();
    }

    /// <summary>
    /// The last resort for a character no loaded family has (for example the system's fonts, as a
    /// browser falls back to them): given the character and the face it was wanted in.
    /// </summary>
    public Func<int, ResolvedFont, ResolvedFont?>? CharacterFallback
    {
        get => _characterFallback;
        set
        {
            _characterFallback = value;
            _fallbacks.Clear();
        }
    }

    Func<int, ResolvedFont, ResolvedFont?>? _characterFallback;

    /// <summary>A face that has the character: the resolved one, else the fallback families in order, else <see cref="CharacterFallback"/>.</summary>
    public ResolvedFont? ResolveFor(int codepoint, ResolvedFont preferred)
    {
        if (preferred.Face.Has(codepoint))
            return preferred;
        var key = (preferred.Face.Id, preferred.SyntheticBold, preferred.SyntheticOblique, codepoint);
        if (_fallbacks.TryGetValue(key, out var known))
            return known;
        ResolvedFont? found = null;
        foreach (var family in _fallbackFamilies)
            if (Resolve(family, preferred.Face.Weight, preferred.Face.Italic) is { } fallback && fallback.Face.Has(codepoint))
            {
                found = fallback with { SyntheticBold = preferred.SyntheticBold || fallback.SyntheticBold, SyntheticOblique = preferred.SyntheticOblique || fallback.SyntheticOblique };
                break;
            }
        found ??= _characterFallback?.Invoke(codepoint, preferred);
        _fallbacks[key] = found;
        return found;
    }

    public void Dispose()
    {
        foreach (var face in _faces)
            face.Dispose();
        _faces.Clear();
        foreach (var face in _sheetFaces.Select(f => f.Face).Concat(_retired))
            face.Dispose();
        _sheetFaces.Clear();
        _retired.Clear();
        FreeType.DoneFreeType(_freeType);
    }
}
