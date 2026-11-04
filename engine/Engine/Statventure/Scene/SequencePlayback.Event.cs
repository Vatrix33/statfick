namespace Statventure.Scene;

public sealed partial record EventTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => new EventPlayback(context.Player, Cues, context.Times(Cues));
}

/// <summary>Event cues: moments delivered to game code (Q17), those at a skip or cut out of the reduced variant marked as skipped (Q18, Q19).</summary>
internal sealed class EventPlayback(SequencePlayer player, IReadOnlyList<EventCue> cues, CueTimes times) : TrackPlayback
{
    public override IReadOnlyList<(double Time, int Cue)> Moments { get; } = times.Moments;

    public override void Pass(int cue, bool skipped) => player.Queue(new SequenceCue(cues[cue].Name, times.TimeOf(cue), skipped));
}
