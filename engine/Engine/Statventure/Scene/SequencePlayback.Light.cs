namespace Statventure.Scene;

public sealed partial record LightTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => new LightPlayback();
}

/// <summary>
/// The light track's playback: the view's sun from light nodes, its strength and angle (Q12). Step 5 of M10 adds it here
/// (Docs/Scene.md, "Steps"); until then the track plays nothing.
/// </summary>
internal sealed class LightPlayback : TrackPlayback
{
}
