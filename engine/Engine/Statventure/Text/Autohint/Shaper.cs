namespace Statventure.Text.Autohint;

/// <summary>A glyph shaped for metrics: its id and vertical offset (skrifa's ShapedGlyph).</summary>
internal readonly record struct ShapedGlyph(uint Id, int YOffset);

internal enum ShaperCoverageKind
{
    /// <summary>The style's script only.</summary>
    Script,
    /// <summary>The script and DFLT, the catch-all after every style.</summary>
    Default,
}

/// <summary>Lookups already walked for one style's coverage (skrifa's VisitedLookupSet).</summary>
internal sealed class VisitedLookupSet(int lookupCount)
{
    readonly byte[] _bits = new byte[(lookupCount + 7) / 8];

    /// <summary>Adds the lookup; false when it's already in the set (or beyond it).</summary>
    public bool Insert(int lookupIndex)
    {
        var byteIx = lookupIndex / 8;
        var mask = (byte)(1 << (lookupIndex % 8));
        if (byteIx >= _bits.Length || (_bits[byteIx] & mask) != 0)
            return false;
        _bits[byteIx] |= mask;
        return true;
    }

    public void Clear() => Array.Clear(_bits);
}

/// <summary>
/// Maps characters to glyphs for the autohinter and widens style coverage through GSUB
/// (skrifa's autohint Shaper in best-effort mode, as Chromium builds skrifa with
/// autohint_shaping; FreeType's afshaper.c).
/// </summary>
internal sealed class Shaper
{
    // As HarfBuzz limits nesting.
    const int MaxNestingDepth = 64;

    public readonly AutohintFont Font;
    readonly Gsub? _gsub;

    public Shaper(AutohintFont font)
    {
        Font = font;
        _gsub = font.Gsub is { } bytes ? Gsub.TryRead(bytes) : null;
    }

    public int LookupCount => _gsub?.LookupCount ?? 0;

    /// <summary>A shaper for one style's clusters: single substitutions of its feature, else the character map.</summary>
    public ClusterShaper ForStyle(StyleClass style)
    {
        // Substitutions apply only for styles with a feature.
        if (style.Feature is { } featureTag && _gsub is { } gsub
            && gsub.SelectScript(ScriptTags.FromUnicode(style.Script.Tag)) is { } scriptIndex
            && gsub.DefaultLangSys(scriptIndex) is { } langSys
            && gsub.FeatureIndexForTag(langSys, featureTag) is { } featureIndex)
            return new ClusterShaper(this, gsub, gsub.FeatureLookups(featureIndex));
        return new ClusterShaper(this, null, null);
    }

    /// <summary>
    /// Gives the glyphs GSUB reaches for a style (and that no character maps to) that style;
    /// true when it gave any (afshaper.c af_shaper_get_coverage).
    /// </summary>
    public bool ComputeCoverage(StyleClass style, ShaperCoverageKind kind, GlyphStyle[] glyphStyles, VisitedLookupSet visited)
    {
        if (_gsub is not { } gsub)
            return false;
        var tags = new string?[3];
        var fromUnicode = ScriptTags.FromUnicode(style.Script.Tag);
        for (var i = 0; i < Math.Min(3, fromUnicode.Length); i++)
            tags[i] = fromUnicode[i];
        const string defaultScript = "DFLT";
        if (kind == ShaperCoverageKind.Default)
        {
            if (tags[0] is null)
                tags[0] = defaultScript;
            else if (tags[1] is null)
                tags[1] = defaultScript;
            else if (tags[1] != defaultScript)
                tags[2] = defaultScript;
        }
        // Script classes with tags for special purposes are skipped (as skrifa compares them,
        // against the tags it made from them: these never match).
        else if (tags[0] is "Khms" or "Latb" or "Latp")
            return false;
        var handler = new GsubHandler(this, gsub, style, glyphStyles, visited);
        foreach (var tag in tags)
        {
            if (tag is null || gsub.ScriptIndexForTag(tag) is not { } scriptIndex)
                continue;
            foreach (var langSys in gsub.LangSystems(scriptIndex))
                foreach (var featureIndex in gsub.FeatureIndices(langSys))
                {
                    if (gsub.FeatureTag(featureIndex) is not { } featureTag || (style.Feature is not null && style.Feature != featureTag))
                        continue;
                    foreach (var lookup in gsub.FeatureLookups(featureIndex))
                        handler.ProcessLookup(lookup);
                }
        }
        if (handler.Finish() is not { } range)
            return false;
        var result = false;
        for (var gid = range.Start; gid < range.End; gid++)
            result |= glyphStyles[gid].MaybeAssignGsubOutputStyle(style);
        return result;
    }

    /// <summary>Collects the output glyphs of a style's lookups (skrifa's GsubHandler).</summary>
    sealed class GsubHandler(Shaper shaper, Gsub gsub, StyleClass style, GlyphStyle[] glyphStyles, VisitedLookupSet visited)
    {
        // A style with a feature only counts when its lookups substitute a blue string character.
        bool _needBlueSubsts = style.Feature is not null;
        int _minGid = glyphStyles.Length;
        int _maxGid;
        int _depth;

        public bool ProcessLookup(int lookupIndex)
        {
            if (_depth == MaxNestingDepth)
                return false;
            if (!visited.Insert(lookupIndex))
                return true;
            _depth++;
            var result = ProcessLookupInner(lookupIndex);
            _depth--;
            return result;
        }

        bool ProcessLookupInner(int lookupIndex)
        {
            if (gsub.Subtables(lookupIndex) is not { } lookup)
                return true;
            foreach (var subtable in lookup.Offsets)
            {
                // A subtable that doesn't read is skipped (read-fonts' filter_map over results).
                try
                {
                    if (!ProcessSubtable(lookup.Type, subtable))
                        return false;
                }
                catch (IndexOutOfRangeException)
                {
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
            return true;
        }

        bool ProcessSubtable(int type, int t)
        {
            var d = gsub.Data;
            switch (type)
            {
                case 1:
                {
                    var format = Gsub.U16(d, t);
                    var coverage = t + Gsub.U16(d, t + 2);
                    if (format == 1)
                    {
                        var delta = (short)Gsub.U16(d, t + 4);
                        foreach (var gid in Gsub.CoverageGlyphs(d, coverage))
                            Capture((uint)(ushort)(gid + delta));
                        if (_needBlueSubsts && _depth == 1)
                            CheckBlueCoverage(coverage);
                    }
                    else if (format == 2)
                    {
                        var count = Gsub.U16(d, t + 4);
                        for (var i = 0; i < count; i++)
                            Capture(Gsub.U16(d, t + 6 + 2 * i));
                        if (_needBlueSubsts && _depth == 1)
                            CheckBlueCoverage(coverage);
                    }
                    return true;
                }
                case 2:
                {
                    var coverage = t + Gsub.U16(d, t + 2);
                    var count = Gsub.U16(d, t + 4);
                    for (var i = 0; i < count; i++)
                    {
                        var seq = t + Gsub.U16(d, t + 6 + 2 * i);
                        var glyphs = Gsub.U16(d, seq);
                        for (var j = 0; j < glyphs; j++)
                            Capture(Gsub.U16(d, seq + 2 + 2 * j));
                    }
                    if (_needBlueSubsts && _depth == 1)
                        CheckBlueCoverage(coverage);
                    return true;
                }
                case 3:
                {
                    var count = Gsub.U16(d, t + 4);
                    for (var i = 0; i < count; i++)
                    {
                        var set = t + Gsub.U16(d, t + 6 + 2 * i);
                        var glyphs = Gsub.U16(d, set);
                        for (var j = 0; j < glyphs; j++)
                            Capture(Gsub.U16(d, set + 2 + 2 * j));
                    }
                    return true;
                }
                case 4:
                {
                    var count = Gsub.U16(d, t + 4);
                    for (var i = 0; i < count; i++)
                    {
                        var set = t + Gsub.U16(d, t + 6 + 2 * i);
                        var ligatures = Gsub.U16(d, set);
                        for (var j = 0; j < ligatures; j++)
                            Capture(Gsub.U16(d, set + Gsub.U16(d, set + 2 + 2 * j)));
                    }
                    return true;
                }
                case 5:
                    foreach (var lookup in Gsub.ContextLookups(d, t))
                        if (!ProcessLookup(lookup))
                            return false;
                    return true;
                case 6:
                    foreach (var lookup in Gsub.ChainContextLookups(d, t))
                        if (!ProcessLookup(lookup))
                            return false;
                    return true;
                case 8:
                {
                    var backtrack = Gsub.U16(d, t + 4);
                    var lookahead = t + 6 + 2 * backtrack;
                    var lookaheadCount = Gsub.U16(d, lookahead);
                    var glyphCountAt = lookahead + 2 + 2 * lookaheadCount;
                    var glyphs = Gsub.U16(d, glyphCountAt);
                    for (var i = 0; i < glyphs; i++)
                        Capture(Gsub.U16(d, glyphCountAt + 2 + 2 * i));
                    return true;
                }
                default:
                    return true;
            }
        }

        /// <summary>The range of glyphs the lookups reached, or null (also when a feature style substitutes no blue character).</summary>
        public (int Start, int End)? Finish()
        {
            visited.Clear();
            if (_minGid > _maxGid)
                return null;
            if (_needBlueSubsts)
            {
                for (var gid = _minGid; gid <= _maxGid; gid++)
                    glyphStyles[gid].Bits &= unchecked((ushort)~GlyphStyle.FromGsubOutput);
                return null;
            }
            return (_minGid, _maxGid + 1);
        }

        void CheckBlueCoverage(int coverage)
        {
            foreach (var (blue, _) in style.Script.Blues)
                foreach (var rune in blue.EnumerateRunes())
                    if (shaper.Font.Map(rune.Value) is { } glyph && Gsub.CoverageIndex(gsub.Data, coverage, glyph) is not null)
                    {
                        _needBlueSubsts = false;
                        return;
                    }
        }

        void Capture(uint gid)
        {
            if (gid >= (uint)glyphStyles.Length)
                return;
            glyphStyles[gid].SetFromGsubOutput();
            _minGid = Math.Min(_minGid, (int)gid);
            _maxGid = Math.Max(_maxGid, (int)gid);
        }
    }
}

/// <summary>Shapes a blue string's cluster: nominal glyphs, then the style feature's single substitutions (skrifa's ClusterShaper).</summary>
internal sealed class ClusterShaper(Shaper shaper, Gsub? gsub, int[]? featureLookups)
{
    const int MaxNestingDepth = 64;

    public void Shape(string input, List<ShapedGlyph> output)
    {
        output.Clear();
        foreach (var rune in input.EnumerateRunes())
            output.Add(new ShapedGlyph(shaper.Font.Map(rune.Value) ?? 0, 0));
        if (gsub is null || featureLookups is null)
            return;
        var didSubst = false;
        foreach (var lookup in featureLookups)
            for (var i = 0; i < output.Count; i++)
                didSubst |= ApplyLookup(lookup, output, i, 0);
        // Clusters the feature doesn't change don't count.
        if (!didSubst)
            output.Clear();
    }

    /// <summary>Applies a lookup's single substitutions to one glyph.</summary>
    bool ApplyLookup(int lookupIndex, List<ShapedGlyph> cluster, int glyphIx, int depth)
    {
        if (depth > MaxNestingDepth || glyphIx >= cluster.Count || gsub!.Subtables(lookupIndex) is not { Type: 1 } lookup)
            return false;
        var d = gsub.Data;
        var glyph = cluster[glyphIx];
        foreach (var t in lookup.Offsets)
        {
            try
            {
                var format = Gsub.U16(d, t);
                var coverage = t + Gsub.U16(d, t + 2);
                if (format == 1)
                {
                    if (Gsub.CoverageIndex(d, coverage, glyph.Id) is null)
                        continue;
                    var delta = (short)Gsub.U16(d, t + 4);
                    cluster[glyphIx] = glyph with { Id = (ushort)(glyph.Id + delta) };
                    return true;
                }
                if (format == 2)
                {
                    if (Gsub.CoverageIndex(d, coverage, glyph.Id) is not { } index || index >= Gsub.U16(d, t + 4))
                        continue;
                    cluster[glyphIx] = glyph with { Id = Gsub.U16(d, t + 6 + 2 * index) };
                    return true;
                }
            }
            catch (IndexOutOfRangeException)
            {
            }
        }
        return false;
    }
}

/// <summary>OpenType script tags for an ISO 15924 script, as read-fonts' ScriptTags::from_unicode (HarfBuzz's hb-ot-tag.cc) gives them.</summary>
internal static class ScriptTags
{
    static readonly Dictionary<string, string> NewTags = new()
    {
        ["Beng"] = "bng2", ["Deva"] = "dev2", ["Gujr"] = "gjr2", ["Guru"] = "gur2", ["Knda"] = "knd2",
        ["Mlym"] = "mlm2", ["Mymr"] = "mym2", ["Orya"] = "ory2", ["Taml"] = "tml2", ["Telu"] = "tel2",
    };

    public static string[] FromUnicode(string script)
    {
        var tags = new List<string>(3);
        if (NewTags.TryGetValue(script, out var newTag))
        {
            // Myanmar has no version 3 tag.
            if (newTag != "mym2")
                tags.Add(newTag[..3] + "3");
            tags.Add(newTag);
        }
        tags.Add(script switch
        {
            "Zmth" => "math",
            "Hira" => "kana",
            "Laoo" => "lao ",
            "Yiii" => "yi  ",
            "Nkoo" => "nko ",
            "Vaii" => "vai ",
            _ => char.ToLowerInvariant(script[0]) + script[1..],
        });
        return [.. tags];
    }
}

/// <summary>
/// The parts of a GSUB table the autohinter reads, read as read-fonts reads them (script and
/// language system tags by binary search, extension lookups resolved by the first readable
/// extension's type).
/// </summary>
internal sealed class Gsub
{
    public readonly byte[] Data;
    readonly int _scriptList, _featureList, _lookupList;

    Gsub(byte[] data, int scriptList, int featureList, int lookupList)
    {
        Data = data;
        _scriptList = scriptList;
        _featureList = featureList;
        _lookupList = lookupList;
    }

    public static Gsub? TryRead(byte[] data)
    {
        if (data.Length < 10)
            return null;
        int scripts = U16(data, 4), features = U16(data, 6), lookups = U16(data, 8);
        if (scripts == 0 || features == 0 || lookups == 0 || scripts >= data.Length || features >= data.Length || lookups >= data.Length)
            return null;
        return new Gsub(data, scripts, features, lookups);
    }

    public static ushort U16(byte[] d, int at) => (ushort)(d[at] << 8 | d[at + 1]);

    static uint U32(byte[] d, int at) => (uint)(d[at] << 24 | d[at + 1] << 16 | d[at + 2] << 8 | d[at + 3]);

    static string Tag(byte[] d, int at) => new([(char)d[at], (char)d[at + 1], (char)d[at + 2], (char)d[at + 3]]);

    static int CompareTag(byte[] d, int at, string tag)
    {
        for (var i = 0; i < 4; i++)
            if (d[at + i] != tag[i])
                return d[at + i].CompareTo((byte)tag[i]);
        return 0;
    }

    public int LookupCount => U16(Data, _lookupList);

    /// <summary>A tagged record array's index for a tag (binary search, as read-fonts' index_for_tag).</summary>
    static int? BinarySearchTag(byte[] d, int countAt, string tag)
    {
        int lo = 0, hi = U16(d, countAt);
        while (lo < hi)
        {
            var mid = lo + (hi - lo) / 2;
            var c = CompareTag(d, countAt + 2 + 6 * mid, tag);
            if (c == 0)
                return mid;
            if (c < 0)
                lo = mid + 1;
            else
                hi = mid;
        }
        return null;
    }

    public int? ScriptIndexForTag(string tag) => BinarySearchTag(Data, _scriptList, tag);

    /// <summary>The first of the tags the script list has, else DFLT, dflt or latn (ScriptList::select).</summary>
    public int? SelectScript(string[] tags)
    {
        foreach (var tag in tags)
            if (ScriptIndexForTag(tag) is { } index)
                return index;
        foreach (var tag in (string[])["DFLT", "dflt", "latn"])
            if (ScriptIndexForTag(tag) is { } index)
                return index;
        return null;
    }

    int Script(int index) => _scriptList + U16(Data, _scriptList + 2 + 6 * index + 4);

    /// <summary>A script's default language system (its offset), or null.</summary>
    public int? DefaultLangSys(int scriptIndex)
    {
        var script = Script(scriptIndex);
        var offset = U16(Data, script);
        return offset == 0 ? null : script + offset;
    }

    /// <summary>A script's language systems: each record's, then the default one.</summary>
    public IEnumerable<int> LangSystems(int scriptIndex)
    {
        var script = Script(scriptIndex);
        var count = U16(Data, script + 2);
        for (var i = 0; i < count; i++)
            yield return script + U16(Data, script + 4 + 6 * i + 4);
        if (DefaultLangSys(scriptIndex) is { } defaultLangSys)
            yield return defaultLangSys;
    }

    public IEnumerable<int> FeatureIndices(int langSys)
    {
        var count = U16(Data, langSys + 4);
        for (var i = 0; i < count; i++)
            yield return U16(Data, langSys + 6 + 2 * i);
    }

    public string? FeatureTag(int featureIndex) =>
        featureIndex < U16(Data, _featureList) ? Tag(Data, _featureList + 2 + 6 * featureIndex) : null;

    /// <summary>The first of a language system's features with the tag (LangSys::feature_index_for_tag).</summary>
    public int? FeatureIndexForTag(int langSys, string tag)
    {
        foreach (var index in FeatureIndices(langSys))
            if (FeatureTag(index) == tag)
                return index;
        return null;
    }

    public int[] FeatureLookups(int featureIndex)
    {
        var feature = _featureList + U16(Data, _featureList + 2 + 6 * featureIndex + 4);
        var count = U16(Data, feature + 2);
        var lookups = new int[count];
        for (var i = 0; i < count; i++)
            lookups[i] = U16(Data, feature + 4 + 2 * i);
        return lookups;
    }

    /// <summary>A lookup's type and its subtables' offsets in the table (extensions resolved), or null.</summary>
    public (int Type, int[] Offsets)? Subtables(int lookupIndex)
    {
        try
        {
            if (lookupIndex >= LookupCount)
                return null;
            var lookup = _lookupList + U16(Data, _lookupList + 2 + 2 * lookupIndex);
            var type = U16(Data, lookup);
            var count = U16(Data, lookup + 4);
            var offsets = new int[count];
            for (var i = 0; i < count; i++)
                offsets[i] = lookup + U16(Data, lookup + 6 + 2 * i);
            if (type != 7)
                return (type, offsets);
            // Extensions: the first one that reads gives the type.
            int? extensionType = null;
            foreach (var ext in offsets)
                if (ext + 8 <= Data.Length && U16(Data, ext) == 1)
                {
                    extensionType = U16(Data, ext + 2);
                    break;
                }
            if (extensionType is not { } resolved)
                return (0, []);
            var subtables = new List<int>(count);
            foreach (var ext in offsets)
                if (ext + 8 <= Data.Length && U16(Data, ext) == 1)
                    subtables.Add(ext + (int)U32(Data, ext + 4));
            return (resolved, [.. subtables]);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>A coverage table's glyphs in order.</summary>
    public static IEnumerable<int> CoverageGlyphs(byte[] d, int coverage)
    {
        var format = U16(d, coverage);
        var count = U16(d, coverage + 2);
        if (format == 1)
            for (var i = 0; i < count; i++)
                yield return U16(d, coverage + 4 + 2 * i);
        else if (format == 2)
            for (var i = 0; i < count; i++)
            {
                var record = coverage + 4 + 6 * i;
                for (int g = U16(d, record), end = U16(d, record + 2); g <= end; g++)
                    yield return g;
            }
    }

    /// <summary>A glyph's index in a coverage table, or null (binary searches, as read-fonts).</summary>
    public static int? CoverageIndex(byte[] d, int coverage, uint glyph)
    {
        if (glyph > 0xFFFF)
            return null;
        var format = U16(d, coverage);
        var count = U16(d, coverage + 2);
        int lo = 0, hi = count;
        while (lo < hi)
        {
            var mid = lo + (hi - lo) / 2;
            if (format == 1)
            {
                var g = U16(d, coverage + 4 + 2 * mid);
                if (g == glyph)
                    return mid;
                if (g < glyph)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            else if (format == 2)
            {
                var record = coverage + 4 + 6 * mid;
                int start = U16(d, record), end = U16(d, record + 2);
                if (end < glyph)
                    lo = mid + 1;
                else if (start > glyph)
                    hi = mid;
                else
                    return (ushort)(U16(d, record + 4) + (int)(glyph - start));
            }
            else
                return null;
        }
        return null;
    }

    /// <summary>The nested lookups of a (sequence) context subtable, in rule order.</summary>
    public static IEnumerable<int> ContextLookups(byte[] d, int t)
    {
        var format = U16(d, t);
        if (format is 1 or 2)
        {
            var setsAt = format == 1 ? t + 4 : t + 6;
            var sets = U16(d, setsAt);
            for (var i = 0; i < sets; i++)
            {
                var setOffset = U16(d, setsAt + 2 + 2 * i);
                if (setOffset == 0)
                    continue;
                var set = t + setOffset;
                var rules = U16(d, set);
                for (var j = 0; j < rules; j++)
                {
                    var rule = set + U16(d, set + 2 + 2 * j);
                    var glyphCount = U16(d, rule);
                    var lookupCount = U16(d, rule + 2);
                    var records = rule + 4 + 2 * Math.Max(0, glyphCount - 1);
                    for (var k = 0; k < lookupCount; k++)
                        yield return U16(d, records + 4 * k + 2);
                }
            }
        }
        else if (format == 3)
        {
            var glyphCount = U16(d, t + 2);
            var lookupCount = U16(d, t + 4);
            var records = t + 6 + 2 * glyphCount;
            for (var k = 0; k < lookupCount; k++)
                yield return U16(d, records + 4 * k + 2);
        }
    }

    /// <summary>The nested lookups of a chained context subtable, in rule order.</summary>
    public static IEnumerable<int> ChainContextLookups(byte[] d, int t)
    {
        var format = U16(d, t);
        if (format is 1 or 2)
        {
            var setsAt = format == 1 ? t + 4 : t + 10;
            var sets = U16(d, setsAt);
            for (var i = 0; i < sets; i++)
            {
                var setOffset = U16(d, setsAt + 2 + 2 * i);
                if (setOffset == 0)
                    continue;
                var set = t + setOffset;
                var rules = U16(d, set);
                for (var j = 0; j < rules; j++)
                {
                    var rule = set + U16(d, set + 2 + 2 * j);
                    var at = rule;
                    var backtrack = U16(d, at);
                    at += 2 + 2 * backtrack;
                    var input = U16(d, at);
                    at += 2 + 2 * Math.Max(0, input - 1);
                    var lookahead = U16(d, at);
                    at += 2 + 2 * lookahead;
                    var lookupCount = U16(d, at);
                    for (var k = 0; k < lookupCount; k++)
                        yield return U16(d, at + 2 + 4 * k + 2);
                }
            }
        }
        else if (format == 3)
        {
            var at = t + 2;
            var backtrack = U16(d, at);
            at += 2 + 2 * backtrack;
            var input = U16(d, at);
            at += 2 + 2 * input;
            var lookahead = U16(d, at);
            at += 2 + 2 * lookahead;
            var lookupCount = U16(d, at);
            for (var k = 0; k < lookupCount; k++)
                yield return U16(d, at + 2 + 4 * k + 2);
        }
    }
}
