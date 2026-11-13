namespace Statventure.Game.Data.Saves;

/// <summary>
/// The save (Docs/Game/Foundation.md, "The records and files GM1 adds"; GM1-P5): one character's whole state. Step 2's
/// stub, a plain record with the character's name and no attributes, so the program's parts compile against it; step 5
/// makes it the record <c>save.game</c>, version 1 (<c>[Record]</c>, <c>name</c> as field 1 with its help and checks),
/// with its layout signature and fixtures. Not a kind of mods (no <c>[Kind]</c>).
/// </summary>
public sealed record SaveGame
{
    /// <summary>The character's name as the player gave it (Q1).</summary>
    public string Name { get; init; } = "";
}
