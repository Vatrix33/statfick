using Statventure.Animation;
using Statventure.Render3D;

namespace Statventure.Scene;

public sealed partial record ParticleTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context)
    {
        var emitters = new (NodeHandle Node, SceneEmitter Emitter)[Cues.Count];
        for (var i = 0; i < Cues.Count; i++)
        {
            var cue = Cues[i];
            var node = context.Node(cue.Node);
            SceneEmitter emitter;
            try
            {
                emitter = new SceneEmitter(context.Effect(cue.Effect))
                {
                    Seed = cue.Seed, Burst = cue.Burst, Rate = (float)cue.Rate, Duration = (float)cue.Duration,
                    Palette = cue.Palette is { } role ? (Palette)context.Role(role) : null,
                };
            }
            catch (ArgumentException problem)
            {
                throw context.Problem($"cue {i}: {problem.Message}");
            }
            emitters[i] = (node, emitter);
        }
        return new ParticlePlayback(context, emitters, Cues.Select(c => context.Time(c)).ToArray());
    }
}

/// <summary>
/// The particle track's playback (Q25, Q26, S5-3): a state, as every track at τ: each cue's emitter
/// hangs on its node from the start, held, its clock τ less the cue's time (before the cue it has
/// released nothing; seeking back before it takes its particles away again). The scene records the
/// nodes' worlds and draws the particles; a seek, a skip and a frame across a cut of the reduced
/// variant are jumps (particles born in the time jumped over start where the node is after it). At a
/// skip the particles are those alive at the end time (Q26); at the end the emitters go on, on the
/// scene's clock, until their last particle has died.
/// </summary>
internal sealed class ParticlePlayback(PlaybackContext context, (NodeHandle Node, SceneEmitter Emitter)[] emitters, double[] times) : TrackPlayback
{
    (double From, double To)[] _cuts = [];
    double _tau = double.NaN;

    public override void Begin()
    {
        foreach (var (node, emitter) in emitters)
        {
            emitter.Held = true;
            node.Node.Attach(emitter);
        }
        // The stretches of the timeline the played variant cuts out (between its spans, and after the last).
        var sequence = context.Sequence;
        if (context.Player.Reduced && sequence.Reduced is { } spans)
        {
            var cuts = new List<(double, double)>();
            for (var i = 0; i < spans.Count; i++)
            {
                var next = i + 1 < spans.Count ? spans[i + 1].From : sequence.Duration;
                if (next - spans[i].To > AnimationStrip.Epsilon)
                    cuts.Add((spans[i].To, next));
            }
            _cuts = [.. cuts];
        }
        for (var i = 0; i < emitters.Length; i++)
            emitters[i].Emitter.Show(context.Player.TimelineTime - times[i], jump: false);
    }

    public override void Apply(double tau, bool force)
    {
        var across = !double.IsNaN(_tau) && _cuts.Any(c => _tau < c.To - AnimationStrip.Epsilon && tau > c.From + AnimationStrip.Epsilon);
        _tau = tau;
        for (var i = 0; i < emitters.Length; i++)
            emitters[i].Emitter.Show(tau - times[i], across);
    }

    public override void Jumped(double tau)
    {
        foreach (var (_, emitter) in emitters)
            emitter.Jumped();
    }

    public override void Ended(bool skipped)
    {
        foreach (var (_, emitter) in emitters)
        {
            emitter.Held = false;
            if (skipped)
                emitter.Jumped();
        }
    }
}
