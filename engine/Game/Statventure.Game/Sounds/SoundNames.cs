namespace Statventure.Game.Sounds;

/// <summary>
/// The interface sounds' names (GM1-11, Q13, Q14; Docs/Game/Foundation.md, "The sounds"): each an effect patch at
/// <c>audio/effects/&lt;name&gt;</c> in <c>audio.pak</c> (step 4's patches), played by name through <c>GameUi.Sound</c>
/// (step 8; a name with no patch in the data is logged once and skipped). Final since step 2.
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

    /// <summary>The eight, in the design's order.</summary>
    public static IReadOnlyList<string> All { get; } = [Press, Toggle, Open, Close, Saved, Opened, Refused, Deleted];

    /// <summary>Where a sound's patch is in the game's data.</summary>
    public static string PatchPath(string name) => "audio/effects/" + name;
}
