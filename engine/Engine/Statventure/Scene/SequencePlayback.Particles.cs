namespace Statventure.Scene;

public sealed partial record ParticleTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => new ParticlePlayback();
}

/// <summary>
/// The particles track's playback: emitters started on nodes, evaluated at τ, at a skip and after the end (Q25, Q26). Step 5 of M10 adds it here
/// (Docs/Scene.md, "Steps"); until then the track plays nothing.
/// </summary>
internal sealed class ParticlePlayback : TrackPlayback
{
}
