using System.Globalization;
using Statventure.Records;

namespace Statventure.Game.Data.Saves;

/// <summary>
/// The save (Docs/Game/Foundation.md, "The records and files GM1 adds"; GM1-P5): one character's whole state, the record
/// <c>save.game</c>. A slot's file holds its binary form behind the engine's save header (<c>saves/&lt;slot&gt;.dat</c>),
/// a character file its JSON form (<c>{ "$types": { "save.game": 1 }, "name": "Ser Alrik" }</c>). Version 1 holds what
/// GM1 needs, the character's name (Q1); GM2's part (the hero) comes as version 2, this one kept as a frozen copy with
/// its <c>Upgrade()</c> and fixtures (M9's rule), and each later milestone adds its part so. Not a kind of mods: no
/// <see cref="KindAttribute"/>, and marked <see cref="NotAPackKindAttribute"/> so the editor offers no New entry of it
/// (the user's Q26 of GM1). The theme and Dev Mode are settings (GM1-4, GM1-13), the random state isn't saved (GM1-5),
/// and the slot's last write time is its file's.
/// </summary>
[NotAPackKind]
[Record("save.game", 1)]
public sealed record SaveGame
{
    /// <summary>The longest name (Q1: the old companions' name field's <c>maxlength</c>, counted as it counts, in UTF-16 units).</summary>
    public const int MaxNameLength = 80;

    /// <summary>The character's name as the player gave it (Q1), kept exactly as typed.</summary>
    [Field(1)] [Help("The character's name, as the slot list shows it.")] public string Name { get; init; } = "";

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
}
