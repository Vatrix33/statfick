using System.Globalization;
using System.Numerics;
using Statventure.Editor.Session;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.UI.Dom;

namespace Statventure.Editor.Preview;

/// <summary>
/// A particle effect's preview (P32): an emitter of the effect at the origin, looping: a burst of
/// <see cref="Burst"/> and <see cref="Rate"/> a second for two of the effect's longest lives (the manager's word,
/// 2026-11-07), started again once its last particle has died; a seed and Restart; the preview's palette, a distinct
/// colour per slot the effect names, editable and kept while the editor runs, not saved in the pack. A valid change
/// starts the loop again with the new effect (P35). The turnable camera frames how far its particles can go.
/// </summary>
sealed class ParticlePreview(PreviewPart part, EditorEntry entry) : PreviewView(part, entry, PreviewKind.Particles)
{
    /// <summary>The loop's burst and stream (P32).</summary>
    public const int Burst = 20;
    public const float Rate = 20;

    ParticleEffect? _last;
    SceneNode? _node;
    SceneEmitter? _emitter;
    UiElement _surface = null!;
    UiElement _palette = null!;
    string[] _slots = [];

    protected override bool HasLast => _last is not null;

    /// <summary>The effect shown: the last valid (null before one).</summary>
    public ParticleEffect? Effect => _last;

    /// <summary>The emitter looping now (null without a 3D view).</summary>
    public SceneEmitter? Emitter => _emitter;

    /// <summary>The seed the loop's emitters are made with.</summary>
    public int Seed { get; private set; } = 1;

    /// <summary>How many times the loop has started (its first start included).</summary>
    public int Starts { get; private set; }

    protected override void BuildControls()
    {
        _surface = Surface(Root);
        var buttons = Root.Append("div", "preview-buttons");
        Button(buttons, "restart", "Restart", Restart).SetAttribute("title", "Start the loop again");
        Button(buttons, "reset-view", "Reset view", () => Part.Viewport?.Orbit.Reset()).SetAttribute("title", "The camera back where it started");
        var seed = Line(Root, "Seed", "Picks the particles' random values: the same seed gives the same particles.");
        var field = seed.Append("input", "preview-number");
        field.SetAttribute("type", "number");
        field.SetAttribute("data-preview", "seed");
        field.SetAttribute("step", "1");
        field.Value = Seed.ToString(CultureInfo.InvariantCulture);
        field.AddEventListener("change", _ =>
        {
            if (int.TryParse(field.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            {
                Seed = value;
                Restart();
            }
            field.Value = Seed.ToString(CultureInfo.InvariantCulture);
        });
        _palette = Root.Append("div", "preview-palette");
    }

    protected override string? Use(object value)
    {
        var effect = (ParticleEffect)value;
        if (effect.Problem() is { } problem)
            return $"The particle effect isn't valid ({problem})";
        var first = _last is null;
        _last = effect;
        ShowPalette();
        if (Part.Viewport is { } viewport)
        {
            if (first)
            {
                _node = viewport.Begin(_surface);
                viewport.Orbit.Frame(Vector3.Zero, Reach(effect));
            }
            Restart();
        }
        return null;
    }

    /// <summary>
    /// How far from the emitter its particles can be: the farthest a particle goes in its longest life at its greatest
    /// speed under gravity and drag (the motion's closed form: with drag k, (v₀ − g/k)(1 − e^(−k·t))/k + g/k·t; without,
    /// v₀·t + g·t²/2, each part at its greatest), half its greatest size and a sphere's radius besides.
    /// </summary>
    public static float Reach(ParticleEffect effect)
    {
        double life = Math.Max(0, effect.Life.Max), speed = Math.Max(0, effect.Speed.Max), gravity = effect.Gravity.Length(), drag = effect.Drag;
        var moved = drag > 0
            ? speed * (1 - Math.Exp(-drag * life)) / drag + gravity * (life / drag - (1 - Math.Exp(-drag * life)) / (drag * drag))
            : speed * life + 0.5 * gravity * life * life;
        var reach = moved + Math.Max(0, effect.Size.Max) / 2 + (effect.Spawn is ParticleSpawn.Sphere sphere ? sphere.Radius : 0);
        return double.IsFinite(reach) ? (float)Math.Max(reach, 0.25) : 1;
    }

    /// <summary>The loop started again: a new emitter of the effect shown, at its seed, in the preview's palette.</summary>
    public void Restart()
    {
        if (_last is not { } effect || _node is not { } node)
            return;
        if (_emitter is { Node: not null } old)
            node.Detach(old);
        _emitter = node.Attach(new SceneEmitter(effect)
        {
            Seed = Seed,
            Burst = Burst,
            Rate = Rate,
            Duration = 2 * Math.Max(0, effect.Life.Max),
            Palette = PreviewColours.Palette(Colours()),
        });
        Starts++;
    }

    public override void Frame(double seconds)
    {
        if (Part.Viewport is not { } viewport || _last is null)
            return;
        viewport.ApplyOrbit();
        // Once its last particle has died the scene takes the emitter off its node, in the frame its clock got there:
        // the loop starts again in the next.
        if (_emitter is null || _emitter.Node is null)
            Restart();
    }

    /// <summary>The effect's slots with their colours: the user's, else the preview's distinct ones.</summary>
    IReadOnlyList<(string Slot, string Colour)> Colours() => PreviewColours.For(_slots, Part.ParticleColours);

    /// <summary>The palette's rows, one per slot the effect names (start, then end): a swatch and its colour as text.</summary>
    void ShowPalette()
    {
        var slots = new[] { _last!.StartColor, _last.EndColor }.Distinct(StringComparer.Ordinal).ToArray();
        if (slots.SequenceEqual(_slots))
        {
            if (_emitter is { } emitter)
                emitter.Palette = PreviewColours.Palette(Colours());
            return;
        }
        _slots = slots;
        _palette.ClearChildren();
        foreach (var (slot, colour) in Colours())
        {
            var line = Line(_palette, slot, $"The preview's colour for the slot {slot} (not saved in the pack)");
            var swatch = line.Append("span", "preview-swatch");
            swatch.SetStyle("background-color", colour);
            var field = line.Append("input", "preview-hex");
            field.SetAttribute("type", "text");
            field.SetAttribute("data-slot", slot);
            field.Value = colour;
            field.AddEventListener("change", _ =>
            {
                if (PreviewColours.Read(field.Value) is { } hex)
                {
                    Part.ParticleColours[slot] = hex;
                    swatch.SetStyle("background-color", hex);
                    if (_emitter is { } emitter)
                        emitter.Palette = PreviewColours.Palette(Colours());
                }
                field.Value = Colours().First(c => c.Slot == slot).Colour;
            });
        }
    }

    public override void Dispose() => Part.Viewport?.End();
}
