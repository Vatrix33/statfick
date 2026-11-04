using Statventure.Render3D;

namespace Statventure.Scene;

public sealed partial record PaletteTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context)
    {
        var palettes = Cues.Select(c => c.Palette is { } role ? (Palette)context.Role(role) : null).ToArray();
        if (context.Stages.TryGetValue(Target, out var stage))
            return new PalettePlayback(() => new PaletteTarget(stage.Instance!), Cues, palettes, context.Times(Cues));
        var target = context.Role(Target) switch
        {
            Actor actor => new PaletteTarget(actor),
            PlacedModel placed => new PaletteTarget(placed),
            ModelInstance instance => new PaletteTarget(instance),
            var other => throw context.Problem($"its target {Target} is a {other.GetType().Name}"),
        };
        return new PalettePlayback(() => target, Cues, palettes, context.Times(Cues));
    }
}

/// <summary>What a palette track colours: a figure (with its gear), a placed model or an instanced model (a stage).</summary>
internal sealed class PaletteTarget
{
    readonly Func<Palette?> _get;
    readonly Action<Palette?> _set;

    public PaletteTarget(Actor actor)
    {
        (_get, _set) = (() => actor.Palette, p => actor.Palette = p);
        Models = [actor.Model.Data, .. actor.Items.Select(i => i.Model.Data), .. actor.Garments.Select(g => g.Model.Data)];
    }

    public PaletteTarget(PlacedModel placed)
    {
        (_get, _set) = (() => placed.Palette, p => placed.Palette = p);
        Models = [placed.Model.Data];
    }

    public PaletteTarget(ModelInstance instance)
    {
        (_get, _set) = (() => instance.Palette, p => instance.Palette = p);
        Models = [instance.Data];
    }

    /// <summary>The models it colours (a figure's gear included).</summary>
    public IReadOnlyList<ModelData> Models { get; }

    public Palette? Palette
    {
        get => _get();
        set => _set(value);
    }
}

/// <summary>
/// The palette of the cue in effect at τ (Q24, S4-3), set when that cue changes (S4-8): a bound palette
/// role, one colour on every slot of the target's models (<see cref="PaletteCue.Fill"/>), or the
/// target's own palette, as it was when the sequence started (also before the first cue).
/// </summary>
internal sealed class PalettePlayback(Func<PaletteTarget> target, IReadOnlyList<PaletteCue> cues, Palette?[] palettes, CueTimes times) : TrackPlayback
{
    PaletteTarget _target = null!;
    Palette? _own;
    int _current = -2;

    public override void Begin()
    {
        _target = target();
        _own = _target.Palette;
        var slots = _target.Models.SelectMany(m => m.Materials).Select(m => m.Slot).Distinct().ToArray();
        for (var i = 0; i < cues.Count; i++)
            if (cues[i].Fill is { } fill)
            {
                var palette = new Palette();
                var colour = Colors.LinearFromHex(fill);
                foreach (var slot in slots)
                    palette.Set(slot, colour);
                palettes[i] = palette;
            }
    }

    public override void Apply(double tau, bool force)
    {
        var cue = times.At(tau);
        if (cue == _current && !force)
            return;
        _current = cue;
        _target.Palette = cue < 0 || palettes[cue] is not { } palette ? _own : palette;
    }
}
