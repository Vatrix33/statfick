namespace Statventure.Scene;

public sealed partial record CameraTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => new CameraPlayback();
}

/// <summary>
/// The camera track's playback: camera cuts and the field of view (Q11). Step 5 of M10 adds it here
/// (Docs/Scene.md, "Steps"); until then the track plays nothing.
/// </summary>
internal sealed class CameraPlayback : TrackPlayback
{
}
