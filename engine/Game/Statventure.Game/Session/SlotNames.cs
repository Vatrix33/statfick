using System.Globalization;
using System.Text;
using HarfBuzzSharp;
using Statventure.Storage;

namespace Statventure.Game.Session;

/// <summary>
/// A slot's file named from its character's name (Q1; Docs/Game/Foundation.md, "The slot's file"): the name itself is
/// kept whole in the save, the slot (<c>saves/&lt;slot&gt;.dat</c>) gets a name under the engine's slot name rule
/// (<see cref="SaveSlots.NameProblem"/>: lowercase a–z, 0–9, <c>-</c>, <c>_</c>, <c>.</c>, at most 100 characters, nothing
/// Windows can't hold). "Ser Alrik" is <c>ser-alrik</c>, a second one <c>ser-alrik-2</c>. The rule's cases
/// (Docs/Game/Foundation.md, "How it works", step 5):
/// <list type="bullet">
/// <item>Each character taken apart by its canonical decompositions and its accents (non-spacing marks) dropped, as the
/// editor's <c>ImportNames.Fit</c> does with HarfBuzz's Unicode data (the program runs in invariant globalization), then
/// lowercased: "Élise" and "Elise" both <c>elise</c>.</item>
/// <item>a–z and 0–9 kept; every run of anything else (spaces, punctuation, <c>_</c> and <c>.</c> too, letters outside
/// a–z such as ß, æ, ø, Cyrillic or Chinese) one <c>-</c>; <c>-</c> taken off both ends.</item>
/// <item>Nothing left (a name of only such characters, "日本" or "!!!"): <c>character</c>.</item>
/// <item>Names differing only in case or accents, or that meet in the rule, share the stem: the later one gets a
/// number.</item>
/// <item>The first of <c>stem</c>, <c>stem-2</c>, <c>stem-3</c> … that is a slot name the rule allows and isn't taken
/// (its save or backup there: what the files say, so a case-insensitive file system counts too). A name the rule
/// would make a Windows device name (<c>con</c>, <c>aux</c>, <c>nul</c>, <c>prn</c>, <c>com0</c>–<c>com9</c>,
/// <c>lpt0</c>–<c>lpt9</c>) isn't allowed, so it starts at <c>-2</c>: "Con" is <c>con-2</c>.</item>
/// <item>The stem cut so the slot's name with its number stays within <see cref="SaveSlots.MaxNameLength"/> (100), a
/// <c>-</c> at the cut taken off. A character's name has at most 80 characters and its stem never more characters than
/// the name, so the cut is only the rule's guard.</item>
/// </list>
/// </summary>
public static class SlotNames
{
    /// <summary>The stem when the name keeps nothing.</summary>
    public const string Fallback = "character";

    /// <summary>The stem <paramref name="name"/> gives (before a number is added and before the length's cut).</summary>
    public static string Stem(string name)
    {
        var text = new StringBuilder(name.Length);
        foreach (var rune in name.EnumerateRunes())
            Add(text, rune.Value);
        var stem = text.ToString().Trim('-');
        return stem.Length == 0 ? Fallback : stem;
    }

    /// <summary>The <paramref name="n"/>th candidate for <paramref name="stem"/>: the stem itself for 1, else <c>stem-n</c>; cut to the slot name's limit.</summary>
    public static string Candidate(string stem, int n)
    {
        var suffix = n == 1 ? "" : "-" + n.ToString(CultureInfo.InvariantCulture);
        var room = SaveSlots.MaxNameLength - suffix.Length;
        if (stem.Length > room)
            stem = stem[..room].TrimEnd('-');
        return stem + suffix;
    }

    /// <summary>The slot a character named <paramref name="name"/> gets: the first candidate the rule allows that <paramref name="taken"/> says is free.</summary>
    public static string Free(string name, Func<string, bool> taken)
    {
        var stem = Stem(name);
        for (var n = 1; ; n++)
        {
            var slot = Candidate(stem, n);
            if (SaveSlots.IsValidName(slot) && !taken(slot))
                return slot;
        }
    }

    /// <summary>A code point added to the stem: its canonical decompositions first, marks dropped, lowercased, kept if a–z or 0–9, else a <c>-</c> (one for a run).</summary>
    static void Add(StringBuilder text, int codePoint)
    {
        if (UnicodeFunctions.Default.TryDecompose(codePoint, out var first, out var second))
        {
            Add(text, first);
            if (second != 0)
                Add(text, second);
            return;
        }
        var rune = new Rune(codePoint);
        if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark)
            return;
        var small = Rune.ToLowerInvariant(rune).Value;
        var kept = small is >= 'a' and <= 'z' or >= '0' and <= '9' ? (char)small : '-';
        if (kept == '-' && text.Length > 0 && text[^1] == '-')
            return;
        text.Append(kept);
    }
}
