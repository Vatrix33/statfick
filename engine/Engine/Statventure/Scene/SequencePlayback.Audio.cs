namespace Statventure.Scene;

public sealed partial record AudioTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => new AudioPlayback();
}

/// <summary>
/// The audio track's playback: sounds and music cues scheduled ahead on the audio clock, cancelled on a pause, a seek, a rate change or a skip, a music cue at a skip applied with music.js's fades (M10-8, Q27, Q28). Step 6 of M10 adds it here
/// (Docs/Scene.md, "Steps"); until then the track plays nothing.
/// </summary>
internal sealed class AudioPlayback : TrackPlayback
{
}
