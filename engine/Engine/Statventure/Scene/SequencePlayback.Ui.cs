namespace Statventure.Scene;

public sealed partial record UiTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => new UiPlayback();
}

/// <summary>
/// The UI track's playback: text, classes and attributes of elements found by id within the bound host, and the CSS animations they start paused and resumed with the sequence (M10-9, Q29). Step 6 of M10 adds it here
/// (Docs/Scene.md, "Steps"); until then the track plays nothing.
/// </summary>
internal sealed class UiPlayback : TrackPlayback
{
}
