using System.Globalization;

namespace Statventure.Text;

static partial class CharacterProperties
{
    static byte Lookup(int codePoint)
    {
        var index = Array.BinarySearch(Starts, codePoint);
        if (index < 0)
            index = ~index - 1;
        return Values[index];
    }

    public static bool IsVariationSelector(int c) => (Lookup(c) & 1) != 0;
    public static bool IsEmojiModifierBase(int c) => (Lookup(c) & 2) != 0;
    public static bool HasCombiningClass(int c) => (Lookup(c) & 4) != 0;
    public static bool IsExtendedPictographic(int c) => (Lookup(c) & 8) != 0;
    /// <summary>Blink's Character::CanTextDecorationSkipInk: false for "/", "\\", "_", CJK ideographs and symbols, Hangul and Linear B ideograms.</summary>
    public static bool CanDecorationSkipInk(int c) => (Lookup(c) & 16) == 0;
    public static bool IsEmojiModifier(int c) => c is >= 0x1F3FB and <= 0x1F3FF;
    public static bool IsRegionalIndicator(int c) => c is >= 0x1F1E6 and <= 0x1F1FF;
    public static bool IsEmojiKeycapBase(int c) => c is >= '0' and <= '9' or '#' or '*';
    public static bool IsEmojiTagSequence(int c) => c is >= 0xE0030 and <= 0xE0039 or >= 0xE0061 and <= 0xE007A;
}

/// <summary>
/// Where the caret goes and what one key deletes, as Blink's editing: arrows and Delete step by
/// grapheme clusters; Backspace removes what Blink's BackspaceStateMachine removes (one code point,
/// or a whole emoji sequence, a flag, a keycap, a CR LF pair).
/// </summary>
public static class TextBoundaries
{
    /// <summary>The next grapheme cluster boundary after <paramref name="offset"/> (the length at the end).</summary>
    public static int NextGrapheme(string text, int offset)
    {
        if (offset >= text.Length)
            return text.Length;
        return offset + StringInfo.GetNextTextElementLength(text, offset);
    }

    /// <summary>The grapheme cluster boundary before <paramref name="offset"/> (0 at the start).</summary>
    public static int PreviousGrapheme(string text, int offset)
    {
        if (offset <= 0)
            return 0;
        var position = 0;
        var previous = 0;
        while (position < offset)
        {
            previous = position;
            position += StringInfo.GetNextTextElementLength(text, position);
        }
        return previous;
    }

    /// <summary>Whether <paramref name="offset"/> is a grapheme cluster boundary.</summary>
    public static bool IsGraphemeBoundary(string text, int offset)
    {
        if (offset <= 0 || offset >= text.Length)
            return true;
        var position = 0;
        while (position < offset)
            position += StringInfo.GetNextTextElementLength(text, position);
        return position == offset;
    }

    /// <summary>Where a Backspace at <paramref name="offset"/> deletes from (Blink's BackspaceStateMachine).</summary>
    public static int BackspaceStart(string text, int offset)
    {
        var machine = new BackspaceMachine();
        for (var i = offset - 1; i >= 0; i--)
            if (!machine.FeedPreceding(text[i]))
                return offset - machine.Finish();
        machine.TellEnd();
        return offset - machine.Finish();
    }

    sealed class BackspaceMachine
    {
        enum State { Start, BeforeLF, BeforeKeycap, BeforeVSAndKeycap, BeforeEmojiModifier, BeforeVSAndEmojiModifier, BeforeVS, BeforeZWJEmoji, BeforeZWJ, BeforeVSAndZWJ, OddNumberedRIS, EvenNumberedRIS, BeforeTagTerm, InTagSequence, Finished }

        State _state = State.Start;
        int _toDelete;
        char _trail;
        int _lastVs;
        bool _tagBase;

        static int Length(int cp) => cp > 0xFFFF ? 2 : 1;

        bool Next(State state)
        {
            _state = state;
            return true;
        }

        bool Done()
        {
            _state = State.Finished;
            return false;
        }

        /// <summary>Feeds the code unit before the ones seen so far; false when the machine has finished.</summary>
        public bool FeedPreceding(char unit)
        {
            int cp = unit;
            if (char.IsHighSurrogate(unit))
            {
                if (_trail == 0)
                {
                    _toDelete++;
                    return Done();
                }
                cp = char.ConvertToUtf32(unit, _trail);
                _trail = '\0';
            }
            else if (char.IsLowSurrogate(unit))
            {
                if (_trail != 0)
                    return Done();
                _trail = unit;
                return true;
            }
            else if (_trail != 0)
                return Done();
            switch (_state)
            {
                case State.Start:
                    _toDelete = Length(cp);
                    if (cp == '\n')
                        return Next(State.BeforeLF);
                    if (CharacterProperties.IsVariationSelector(cp))
                        return Next(State.BeforeVS);
                    if (CharacterProperties.IsRegionalIndicator(cp))
                        return Next(State.OddNumberedRIS);
                    if (CharacterProperties.IsEmojiModifier(cp))
                        return Next(State.BeforeEmojiModifier);
                    if (CharacterProperties.IsExtendedPictographic(cp))
                        return Next(State.BeforeZWJEmoji);
                    if (cp == 0x20E3)
                        return Next(State.BeforeKeycap);
                    if (cp == 0xE007F)
                        return Next(State.BeforeTagTerm);
                    return Done();
                case State.BeforeLF:
                    if (cp == '\r')
                        _toDelete++;
                    return Done();
                case State.BeforeKeycap:
                    if (CharacterProperties.IsVariationSelector(cp))
                    {
                        _lastVs = Length(cp);
                        return Next(State.BeforeVSAndKeycap);
                    }
                    if (CharacterProperties.IsEmojiKeycapBase(cp))
                        _toDelete += Length(cp);
                    return Done();
                case State.BeforeVSAndKeycap:
                    if (CharacterProperties.IsEmojiKeycapBase(cp))
                        _toDelete += _lastVs + Length(cp);
                    return Done();
                case State.BeforeEmojiModifier:
                    if (CharacterProperties.IsVariationSelector(cp))
                    {
                        _lastVs = Length(cp);
                        return Next(State.BeforeVSAndEmojiModifier);
                    }
                    if (CharacterProperties.IsEmojiModifierBase(cp))
                    {
                        _toDelete += Length(cp);
                        if (_tagBase)
                        {
                            _tagBase = false;
                            return Done();
                        }
                        return Next(State.BeforeZWJEmoji);
                    }
                    return Done();
                case State.BeforeVSAndEmojiModifier:
                    if (CharacterProperties.IsEmojiModifierBase(cp))
                        _toDelete += _lastVs + Length(cp);
                    return Done();
                case State.BeforeVS:
                    if (CharacterProperties.IsExtendedPictographic(cp))
                    {
                        _toDelete += Length(cp);
                        if (_tagBase)
                        {
                            _tagBase = false;
                            return Done();
                        }
                        return Next(State.BeforeZWJEmoji);
                    }
                    if (!CharacterProperties.IsVariationSelector(cp) && !CharacterProperties.HasCombiningClass(cp))
                        _toDelete += Length(cp);
                    return Done();
                case State.BeforeZWJEmoji:
                    return cp == 0x200D ? Next(State.BeforeZWJ) : Done();
                case State.BeforeZWJ:
                    if (CharacterProperties.IsExtendedPictographic(cp))
                    {
                        _toDelete += Length(cp) + 1;
                        return CharacterProperties.IsEmojiModifier(cp) ? Next(State.BeforeEmojiModifier) : Next(State.BeforeZWJEmoji);
                    }
                    if (CharacterProperties.IsVariationSelector(cp))
                    {
                        _lastVs = Length(cp);
                        return Next(State.BeforeVSAndZWJ);
                    }
                    return Done();
                case State.BeforeVSAndZWJ:
                    if (!CharacterProperties.IsExtendedPictographic(cp))
                        return Done();
                    _toDelete += Length(cp) + 1 + _lastVs;
                    _lastVs = 0;
                    return Next(State.BeforeZWJEmoji);
                case State.OddNumberedRIS:
                    if (!CharacterProperties.IsRegionalIndicator(cp))
                        return Done();
                    _toDelete += 2;
                    return Next(State.EvenNumberedRIS);
                case State.EvenNumberedRIS:
                    if (!CharacterProperties.IsRegionalIndicator(cp))
                        return Done();
                    _toDelete -= 2;
                    return Next(State.OddNumberedRIS);
                case State.BeforeTagTerm:
                    if (CharacterProperties.IsEmojiTagSequence(cp))
                    {
                        _toDelete += Length(cp);
                        return Next(State.InTagSequence);
                    }
                    return Done();
                case State.InTagSequence:
                    if (CharacterProperties.IsEmojiTagSequence(cp))
                    {
                        _toDelete += Length(cp);
                        return true;
                    }
                    if (CharacterProperties.IsEmojiModifier(cp))
                    {
                        _toDelete += Length(cp);
                        _tagBase = true;
                        return Next(State.BeforeEmojiModifier);
                    }
                    if (CharacterProperties.IsVariationSelector(cp))
                    {
                        _toDelete += Length(cp);
                        _tagBase = true;
                        return Next(State.BeforeVS);
                    }
                    if (CharacterProperties.IsExtendedPictographic(cp))
                        _toDelete += Length(cp);
                    return Done();
                default:
                    return Done();
            }
        }

        public void TellEnd()
        {
            if (_trail != 0)
            {
                _toDelete++;
                _trail = '\0';
            }
        }

        /// <summary>How many code units the Backspace deletes.</summary>
        public int Finish()
        {
            if (_trail != 0)
            {
                _toDelete++;
                _trail = '\0';
            }
            _state = State.Finished;
            return _toDelete;
        }
    }
}
