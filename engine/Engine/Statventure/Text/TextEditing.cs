namespace Statventure.Text;

/// <summary>Blink's word moves (position_units_word.cc) over ICU word boundaries, as on Linux (no skipping of spaces).</summary>
public static class TextEditing
{
    static bool IsWhitespace(char c) => c is ' ' or ' ' or '\n' or '\t';

    static bool IsLineBreak(char c) => c is '\n' or '\r';

    static bool IsPrintable(char c) => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) is not (
        System.Globalization.UnicodeCategory.Control or System.Globalization.UnicodeCategory.Format or System.Globalization.UnicodeCategory.Surrogate
        or System.Globalization.UnicodeCategory.PrivateUse or System.Globalization.UnicodeCategory.OtherNotAssigned
        or System.Globalization.UnicodeCategory.LineSeparator or System.Globalization.UnicodeCategory.ParagraphSeparator);

    static bool IsAlphanumeric(char c) => char.IsLetterOrDigit(c);

    static bool IsPunctuation(char c) => char.IsPunctuation(c);

    static bool IsSymbol(char c) => char.IsSymbol(c);

    /// <summary>Blink's IsWordBreak: printable and not white space, a surrogate, a line break, or an underscore.</summary>
    static bool IsWordBreak(char c) => (IsPrintable(c) && !IsWhitespace(c)) || char.IsSurrogate(c) || IsLineBreak(c) || c == '_';

    /// <summary>Blink's IsWordBoundary: punctuation, a symbol or a surrogate.</summary>
    static bool IsWordBoundary(char c) => IsPunctuation(c) || IsSymbol(c) || char.IsSurrogate(c);

    /// <summary>NextWordPosition (Ctrl+Right): the end of the next word; a run of punctuation after a word is a word of its own.</summary>
    public static int NextWordPosition(string text, int offset)
    {
        if (offset >= text.Length || text.Length == 0)
            return text.Length;
        var boundaries = WordBreaker.Boundaries(text);
        for (var runner = WordBreaker.Following(boundaries, offset); runner != -1; runner = WordBreaker.Following(boundaries, runner))
        {
            if (runner < text.Length && IsLineBreak(text[runner]))
                return runner;
            if (runner < text.Length && IsWordBoundary(text[runner]))
            {
                if (IsAlphanumeric(text[runner - 1]))
                    return runner;
                continue;
            }
            if (runner < text.Length && IsWordBreak(text[runner - 1]))
                return runner;
        }
        return text.Length;
    }

    /// <summary>PreviousWordPosition (Ctrl+Left): the start of the previous word (punctuation counted as one).</summary>
    public static int PreviousWordPosition(string text, int offset)
    {
        if (offset <= 0 || text.Length == 0)
            return 0;
        var boundaries = WordBreaker.Boundaries(text);
        var punctuation = -1;
        for (var runner = WordBreaker.Preceding(boundaries, offset); runner != -1; runner = WordBreaker.Preceding(boundaries, runner))
        {
            if (runner < text.Length && IsWordBoundary(text[runner]))
            {
                if (runner > 0 && IsAlphanumeric(text[runner - 1]))
                    return runner;
                punctuation = runner;
                continue;
            }
            if (punctuation >= 0)
                return punctuation;
            if (runner < text.Length && IsWordBreak(text[runner]))
                return runner;
        }
        return 0;
    }

    /// <summary>StartOfWordPosition: the word boundary before <paramref name="offset"/> (on a boundary, that boundary or the one before, by side).</summary>
    public static int StartOfWord(string text, int offset, bool nextWordIfOnBoundary)
    {
        var boundaries = WordBreaker.Boundaries(text);
        if (nextWordIfOnBoundary)
        {
            if (offset >= text.Length)
                return text.Length;
            return Math.Max(0, WordBreaker.Preceding(boundaries, offset + 1));
        }
        if (offset <= 0)
            return 0;
        return Math.Max(0, WordBreaker.Preceding(boundaries, offset));
    }

    /// <summary>EndOfWordPosition: the word boundary after <paramref name="offset"/> (on a boundary, by side).</summary>
    public static int EndOfWord(string text, int offset, bool previousWordIfOnBoundary)
    {
        var boundaries = WordBreaker.Boundaries(text);
        if (previousWordIfOnBoundary)
        {
            if (offset <= 0)
                return 0;
            var r = WordBreaker.Following(boundaries, offset - 1);
            return r <= 0 ? text.Length : r;
        }
        if (offset >= text.Length)
            return text.Length;
        var result = WordBreaker.Following(boundaries, offset);
        return result <= 0 ? text.Length : result;
    }
}
