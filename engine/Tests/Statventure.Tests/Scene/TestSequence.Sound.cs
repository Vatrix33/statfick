using Statventure.Scene;

namespace Statventure.Tests.Scene;

internal static partial class TestSequence
{
    /// <summary>The UI host role of the test sequence's UI track (the bench's <c>#card</c>).</summary>
    public const string Card = "card";

    /// <summary>
    /// Step 6's tracks: the music (Ji'ruan's theme from 0, stopped at 1.6 s), the effects "chime" (at 0.25 s
    /// with its volume, pan and pitch, and at the end) and "thud" (at the marker "open"); the host "card"
    /// marked playing with its <c>data-boss</c> at 0, its text at the marker "reveal", and at the end no
    /// longer playing and hidden. In the reduced variant (0–0.5 s, then 1.5 s held) the stop is cut out
    /// (applied at the end) and the last chime too (dropped).
    /// </summary>
    static partial void Sound(SequenceBuilder builder) => builder
        .Role(Card, RoleKind.Element)
        .Audio(a => a
            .Music(0, "great-imp-jiruan")
            .Effect(0.25, "chime", 0.8, -0.5, 1.25)
            .Effect(CueTime.At("hierarchy/open"), "thud")
            .StopMusic(1.6)
            .Effect(Duration, "chime", 0.5))
        .Ui(Card, u => u
            .AddClass(0, "is-playing")
            .SetAttribute(0, "data-boss", "test")
            .Text(CueTime.At("hierarchy/reveal"), null, "Revealed")
            .RemoveClass(Duration, "is-playing")
            .SetAttribute(Duration, "hidden", ""));
}
