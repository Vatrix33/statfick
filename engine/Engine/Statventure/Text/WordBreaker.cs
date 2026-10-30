namespace Statventure.Text;

/// <summary>
/// Word boundaries as ICU 78's word break iterator finds them (the one Chromium 152's editing uses
/// for word moves, word deletion and double-click selection): Unicode's word rules (UAX #29) over
/// ICU's sets, in which a full stop only joins numbers ("3.14", not "e.g") and a colon never joins
/// letters. Runs of Kana and Kanji, and of Hangul syllables, stay together (ICU splits them further
/// with dictionaries the engine doesn't have).
/// </summary>
public static class WordBreaker
{
    /// <summary>A code point's word class, and whether it is Extended_Pictographic or Kana/Kanji.</summary>
    public static WordClass ClassOf(int codePoint, out bool extendedPictographic, out bool kanaKanji)
    {
        var value = WordBreakData.Lookup(codePoint);
        extendedPictographic = (value & 32) != 0;
        kanaKanji = (value & 64) != 0;
        return (WordClass)(value & 31);
    }

    /// <summary>The word boundaries of the text (UTF-16 offsets, 0 and the length included).</summary>
    public static List<int> Boundaries(string text)
    {
        var offsets = new List<int>();
        var classes = new List<WordClass>();
        var pictographic = new List<bool>();
        var kanaKanji = new List<bool>();
        for (var i = 0; i < text.Length;)
        {
            var codePoint = CodePointAt(text, i, out var length);
            offsets.Add(i);
            classes.Add(ClassOf(codePoint, out var pict, out var kana));
            pictographic.Add(pict);
            kanaKanji.Add(kana);
            i += length;
        }
        var count = classes.Count;
        var boundaries = new List<int> { 0 };
        static bool Ignored(WordClass c) => c is WordClass.Extend or WordClass.Format or WordClass.ZWJ;
        static bool Hard(WordClass c) => c is WordClass.CR or WordClass.LF or WordClass.Newline;
        static bool AHLetter(WordClass c) => c is WordClass.ALetter or WordClass.HebrewLetter;
        static bool MidNumLetQ(WordClass c) => c is WordClass.MidNumLet or WordClass.SingleQuote;
        // The character before position i, skipping Extend, Format and ZWJ (WB4) unless they follow sot or a newline.
        int Before(int i)
        {
            var j = i - 1;
            while (j >= 0 && Ignored(classes[j]))
                j--;
            if (j < 0 || Hard(classes[j]))
                return i - 1;
            return j;
        }
        // The character after position i (itself included), skipping Extend, Format and ZWJ.
        int After(int i)
        {
            var j = i + 1;
            while (j < count && Ignored(classes[j]))
                j++;
            return j;
        }
        for (var i = 1; i < count; i++)
        {
            var previous = classes[i - 1];
            var current = classes[i];
            bool keep;
            if (previous == WordClass.CR && current == WordClass.LF)
                keep = true; // WB3
            else if (Hard(previous) || Hard(current))
                keep = false; // WB3a, WB3b
            else if (previous == WordClass.ZWJ && pictographic[i])
                keep = true; // WB3c
            else if (previous == WordClass.WSegSpace && current == WordClass.WSegSpace)
                keep = true; // WB3d
            else if (Ignored(current))
                keep = true; // WB4
            else
            {
                var p = Before(i);
                var pp = p >= 0 ? Before(p) : -1;
                var n = i;
                var nn = After(n);
                var P = p >= 0 ? classes[p] : WordClass.Other;
                var PP = pp >= 0 && pp != p ? classes[pp] : WordClass.Other;
                var N = classes[n];
                var NN = nn < count ? classes[nn] : WordClass.Other;
                if (AHLetter(P) && AHLetter(N))
                    keep = true; // WB5
                else if (AHLetter(P) && (N == WordClass.MidLetter || MidNumLetQ(N)) && AHLetter(NN))
                    keep = true; // WB6
                else if (AHLetter(PP) && (P == WordClass.MidLetter || MidNumLetQ(P)) && AHLetter(N))
                    keep = true; // WB7
                else if (P == WordClass.HebrewLetter && N == WordClass.SingleQuote)
                    keep = true; // WB7a
                else if (P == WordClass.HebrewLetter && N == WordClass.DoubleQuote && NN == WordClass.HebrewLetter)
                    keep = true; // WB7b
                else if (PP == WordClass.HebrewLetter && P == WordClass.DoubleQuote && N == WordClass.HebrewLetter)
                    keep = true; // WB7c
                else if (P == WordClass.Numeric && N == WordClass.Numeric)
                    keep = true; // WB8
                else if (AHLetter(P) && N == WordClass.Numeric)
                    keep = true; // WB9
                else if (P == WordClass.Numeric && AHLetter(N))
                    keep = true; // WB10
                else if (PP == WordClass.Numeric && (P == WordClass.MidNum || MidNumLetQ(P)) && N == WordClass.Numeric)
                    keep = true; // WB11
                else if (P == WordClass.Numeric && (N == WordClass.MidNum || MidNumLetQ(N)) && NN == WordClass.Numeric)
                    keep = true; // WB12
                else if (P == WordClass.Katakana && N == WordClass.Katakana)
                    keep = true; // WB13
                else if ((AHLetter(P) || P is WordClass.Numeric or WordClass.Katakana or WordClass.ExtendNumLet) && N == WordClass.ExtendNumLet)
                    keep = true; // WB13a
                else if (P == WordClass.ExtendNumLet && (AHLetter(N) || N is WordClass.Numeric or WordClass.Katakana))
                    keep = true; // WB13b
                else if (P == WordClass.RegionalIndicator && N == WordClass.RegionalIndicator)
                {
                    // WB15, WB16: regional indicators pair up.
                    var run = 0;
                    for (var j = p; j >= 0; j--)
                    {
                        if (Ignored(classes[j]))
                            continue;
                        if (classes[j] != WordClass.RegionalIndicator)
                            break;
                        run++;
                    }
                    keep = run % 2 == 1;
                }
                else if (P == WordClass.HangulSyllable && N == WordClass.HangulSyllable)
                    keep = true; // ICU: Hangul syllables stay together
                else if (p >= 0 && kanaKanji[p] && kanaKanji[n])
                    keep = true; // ICU: Kana and Kanji stay together
                else
                    keep = false; // WB999
            }
            if (!keep)
                boundaries.Add(offsets[i]);
        }
        if (text.Length > 0)
            boundaries.Add(text.Length);
        return boundaries;
    }

    /// <summary>The code point at <paramref name="index"/> (a lone surrogate as itself), and how many UTF-16 units it takes.</summary>
    internal static int CodePointAt(string text, int index, out int length)
    {
        if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(text[index], text[index + 1]);
        }
        length = 1;
        return text[index];
    }

    /// <summary>ICU's following(): the first boundary after <paramref name="offset"/> (-1: none).</summary>
    public static int Following(IReadOnlyList<int> boundaries, int offset)
    {
        foreach (var b in boundaries)
            if (b > offset)
                return b;
        return -1;
    }

    /// <summary>ICU's preceding(): the last boundary before <paramref name="offset"/> (-1: none).</summary>
    public static int Preceding(IReadOnlyList<int> boundaries, int offset)
    {
        for (var i = boundaries.Count - 1; i >= 0; i--)
            if (boundaries[i] < offset)
                return boundaries[i];
        return -1;
    }
}

static partial class WordBreakData
{
    internal static byte Lookup(int codePoint)
    {
        var index = Array.BinarySearch(Starts, codePoint);
        if (index < 0)
            index = ~index - 1;
        return Values[index];
    }
}
