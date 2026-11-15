namespace Statventure.Game.Sounds;

/// <summary>
/// The game's sounds' names: GM1's eight interface sounds (GM1-11, Q13, Q14; Docs/Game/Foundation.md, "The sounds") and
/// GM2's seven for the hero (Q38; Docs/Game/Hero.md, "Sounds" and "How it works", step 12), each an effect patch at
/// <c>audio/effects/&lt;name&gt;</c> in <c>audio.pak</c> (GM1 step 4's and GM2 step 12's patches), played by name through
/// <c>GameUi.Sound</c> (a name with no patch in the data is logged once and skipped). Which action plays which of GM2's
/// seven, and when, is GM2-P17 (Hero.md, "How it works", step 12), wired by GM2's steps 7 and 8.
/// </summary>
public static class SoundNames
{
    /// <summary>A button pressed (and a volume slider let go, at the new level).</summary>
    public const string Press = "ui-press";
    /// <summary>A switch, box or select changed.</summary>
    public const string Toggle = "ui-toggle";
    /// <summary>A window opening.</summary>
    public const string Open = "ui-open";
    /// <summary>A window closing.</summary>
    public const string Close = "ui-close";
    /// <summary>A character file written.</summary>
    public const string Saved = "game-saved";
    /// <summary>A character opened: Continue, Open, New, Load Character.</summary>
    public const string Opened = "game-opened";
    /// <summary>An action refused: a file that isn't a character, an empty name.</summary>
    public const string Refused = "ui-refused";
    /// <summary>A character deleted.</summary>
    public const string Deleted = "game-deleted";

    /// <summary>A race chosen in the carousel, or changed (Dev Mode's too): the race's emblem stamped firmly.</summary>
    public const string RaceChosen = "race-chosen";
    /// <summary>One of the maker's choices changed, a preset picked among them: a pattern book's page flicked.</summary>
    public const string MakerChoice = "maker-choice";
    /// <summary>The maker's random roll: the pounce pot shaken.</summary>
    public const string MakerRandom = "maker-random";
    /// <summary>A free point spent on an attribute, once per point: an ink drop.</summary>
    public const string PointSpent = "point-spent";
    /// <summary>A level up (Dev Mode's without EXP too): gold leaf laid and burnished.</summary>
    public const string LevelUp = "level-up";
    /// <summary>The free points spent given back: the wax tablet smoothed.</summary>
    public const string Respec = "respec";
    /// <summary>The hero confirmed ("Begin as …"): the book clapped shut.</summary>
    public const string HeroConfirmed = "hero-confirmed";

    /// <summary>GM2's seven (Q38), in its order: each outweighs every sound of GM1's in an action (GM2-P17).</summary>
    public static IReadOnlyList<string> Hero { get; } = [RaceChosen, MakerChoice, MakerRandom, PointSpent, LevelUp, Respec, HeroConfirmed];

    /// <summary>The fifteen: GM1's eight in its design's order, then GM2's seven in Q38's.</summary>
    public static IReadOnlyList<string> All { get; } = [Press, Toggle, Open, Close, Saved, Opened, Refused, Deleted, .. Hero];

    /// <summary>Where a sound's patch is in the game's data.</summary>
    public static string PatchPath(string name) => "audio/effects/" + name;
}
