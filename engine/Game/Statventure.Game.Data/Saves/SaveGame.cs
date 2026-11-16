using System.Globalization;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Looks;
using Statventure.Records;

namespace Statventure.Game.Data.Saves;

/// <summary>
/// The save (Docs/Game/Foundation.md, "The records and files GM1 adds"; GM1-P5): one character's whole state, the record
/// <c>save.game</c>. A slot's file holds its binary form behind the engine's save header (<c>saves/&lt;slot&gt;.dat</c>),
/// a character file its JSON form. Version 1 (GM1) held the character's name (Q1); version 2 (GM2-P5; Docs/Game/Hero.md,
/// "The save, version 2" and "How it works", step 6) adds the hero: its sheet (<see cref="ActorSheet"/>) and its look
/// (<see cref="LookHero"/>, none until the maker confirms it: a hero without a race and a look opens on the new hero's
/// flow, Q26). Version 1 is kept as <see cref="V1"/>, its <c>Upgrade()</c> giving the old game's new hero with no race and
/// no look, so a GM1 character opens on that flow; each later milestone adds its part so. Not a kind of mods: no
/// <see cref="KindAttribute"/>, and marked <see cref="NotAPackKindAttribute"/> so the editor offers no New entry of it
/// (the user's Q26 of GM1). The theme and Dev Mode are settings (GM1-4, GM1-13), the random state isn't saved (GM1-5),
/// and the slot's last write time is its file's. A save the game opens is first <see cref="Corrected"/> (Q45).
/// </summary>
[NotAPackKind]
[Record("save.game", 2)]
public sealed record SaveGame
{
    /// <summary>The longest name (Q1: the old companions' name field's <c>maxlength</c>, counted as it counts, in UTF-16 units).</summary>
    public const int MaxNameLength = 80;

    /// <summary>The character's name as the player gave it (Q1), kept exactly as typed.</summary>
    [Field(1)] [Help("The character's name, as the slot list shows it.")] public string Name { get; init; } = "";

    /// <summary>The hero's sheet (GM2-P5): a new hero's (the old <c>defaultCharacter</c>) until it is played.</summary>
    [Field(2)] [Help("The hero's sheet: race, level, EXP, points, attributes, bonuses, HP, Mana and purse.")] public ActorSheet Hero { get; init; } = new();

    /// <summary>The hero's look (GM2-P5, GM2-P6), or null until the maker confirms one (a hero of an enemy-only race chosen in Dev Mode has none, Q27).</summary>
    [Field(3)] [Help("The hero's look as the character maker made it (none: not made yet, or an enemy-only race chosen in Dev Mode).")] public LookHero? Look { get; init; }

    /// <summary>Version 1 (GM1): the character's name only.</summary>
    [Record("save.game", 1)]
    public sealed record V1
    {
        [Field(1)] public string Name { get; init; } = "";

        /// <summary>The name kept; the old game's new hero (<see cref="ActorSheet"/>'s defaults: HP 31, Mana 20, 5 gold) with no race and no look.</summary>
        public SaveGame Upgrade() => new() { Name = Name };
    }

    /// <summary>
    /// The save as the game opens it (the user's Q45, 2026-11-16: out-of-range numbers corrected silently, as the old
    /// game's <c>normalizeCharacter</c>): the hero's sheet through <see cref="SheetCorrection.Correct"/>; the name and the
    /// look as they are (the look's checks refuse what the maker couldn't make).
    /// </summary>
    public SaveGame Corrected() => this with { Hero = SheetCorrection.Correct(Hero) };

    /// <summary>
    /// What is wrong with <paramref name="name"/> as a character's name, or null: empty once trimmed, longer than
    /// <see cref="MaxNameLength"/>, or holding a control character. Each problem a phrase naming the name (the editor shows
    /// it on the field, the game after "That file is not a valid character save: ").
    /// </summary>
    public static IEnumerable<string> NameProblems(string name)
    {
        if (name.Trim().Length == 0)
            yield return "the name is empty";
        if (name.Length > MaxNameLength)
            yield return $"the name has {name.Length} characters, more than {MaxNameLength}";
        foreach (var c in name)
            if (char.IsControl(c))
            {
                yield return $"the name holds a control character (U+{((int)c).ToString("X4", CultureInfo.InvariantCulture)})";
                break;
            }
    }

    /// <summary>The first of <see cref="NameProblems"/>, or null when the name is a good one.</summary>
    public static string? NameProblem(string name) => NameProblems(name).FirstOrDefault();

    [Check]
    static void GoodName(SaveGame save, CheckContext check)
    {
        foreach (var problem in NameProblems(save.Name))
            check.Report(RecordPath.Root.Field(nameof(Name)), problem);
    }

    /// <summary>The look is the hero's race's (the race and the look are chosen together, Q25, Q26).</summary>
    [Check]
    static void LookOfTheHerosRace(SaveGame save, CheckContext check)
    {
        if (save.Look is { } look && look.Race != save.Hero.Race)
            check.Report(RecordPath.Root.Field(nameof(Look)).Field(nameof(LookHero.Race)),
                save.Hero.Race is { } race ? $"the look is a {look.Race}'s, but the hero is a {race}" : $"the look is a {look.Race}'s, but the hero has no race");
    }
}
