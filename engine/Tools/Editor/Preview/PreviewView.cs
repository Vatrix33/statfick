using System.Globalization;
using System.Numerics;
using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.UI.Dom;

namespace Statventure.Editor.Preview;

/// <summary>What the preview shows for an entry (M11-13).</summary>
public enum PreviewKind
{
    /// <summary>Nothing (state machines, UI files, terrain, files, the other kinds).</summary>
    None,
    /// <summary>A sound effect (P31).</summary>
    Effect,
    /// <summary>A music theme (P31).</summary>
    Theme,
    /// <summary>An instrument, percussion voice or room: no preview of its own; a theme playing takes its edits (P31).</summary>
    MusicPart,
    /// <summary>A particle effect (P32).</summary>
    Particles,
    /// <summary>A model (P33).</summary>
    Model,
    /// <summary>A cutscene (P34).</summary>
    Cutscene,
}

/// <summary>
/// One entry's preview in the pane: its controls, what it plays or shows, and its last valid state (P35): it takes the
/// entry's value when the entry builds, has no problems and the engine takes it; otherwise it keeps what it took last
/// and says so under its controls.
/// </summary>
abstract class PreviewView : IDisposable
{
    UiElement? _note;

    protected PreviewView(PreviewPart part, EditorEntry entry, PreviewKind kind)
    {
        Part = part;
        Entry = entry;
        Kind = kind;
    }

    public PreviewPart Part { get; }
    public EditorEntry Entry { get; }
    public PreviewKind Kind { get; }

    /// <summary>The preview's element (its controls and its view).</summary>
    protected UiElement Root { get; private set; } = null!;

    /// <summary>Whether it has taken a valid state yet.</summary>
    protected abstract bool HasLast { get; }

    /// <summary>The preview of <paramref name="entry"/>'s kind (M11-13): the kinds with a preview by their record type.</summary>
    public static PreviewKind KindOf(EditorEntry entry) => entry.Kind?.Type switch
    {
        var t when t == typeof(EffectPatch) => PreviewKind.Effect,
        var t when t == typeof(ThemeSpec) => PreviewKind.Theme,
        var t when t == typeof(InstrumentSpec) || t == typeof(PercussionSpec) || t == typeof(RoomSpec) => PreviewKind.MusicPart,
        var t when t == typeof(ParticleEffect) => PreviewKind.Particles,
        var t when t == typeof(ModelData) => PreviewKind.Model,
        var t when t == typeof(Sequence) => PreviewKind.Cutscene,
        _ => PreviewKind.None,
    };

    /// <summary>
    /// Whether a theme playing plays on when <paramref name="next"/> is selected (P31; the manager's word, 2026-11-07):
    /// an instrument, percussion voice or room (a theme playing takes their edits), or the theme playing itself.
    /// </summary>
    public static bool KeepsTheMusic(EditorEntry next, string? playing) =>
        KindOf(next) == PreviewKind.MusicPart || KindOf(next) == PreviewKind.Theme && next.Name == playing;

    public static PreviewView For(PreviewPart part, EditorEntry entry, PreviewKind kind) => kind switch
    {
        PreviewKind.Effect => new EffectPreview(part, entry),
        PreviewKind.Theme => new ThemePreview(part, entry),
        PreviewKind.MusicPart => new MusicPartPreview(part, entry),
        PreviewKind.Particles => new ParticlePreview(part, entry),
        PreviewKind.Model => new ModelPreview(part, entry),
        PreviewKind.Cutscene => new CutscenePreview(part, entry),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>Builds its controls into <paramref name="root"/> (the part's element's child).</summary>
    public void Build(UiElement root)
    {
        Root = root;
        Root.SetAttribute("data-kind", Kind.ToString().ToLowerInvariant());
        BuildControls();
        _note = Root.Append("div", "preview-note");
        _note.ToggleAttribute("hidden", true);
    }

    protected abstract void BuildControls();

    /// <summary>
    /// Takes the entry as it is now, if it is valid (P35): it builds, has no problems, and <see cref="Use"/> takes its
    /// value; otherwise the last valid state stays and the note says why.
    /// </summary>
    public void Take()
    {
        var why = Entry.Builds
            ? Entry.Problems.Count > 0 ? $"This version has problems ({Entry.Problems[0].Message})" : Use(Entry.Build!.Value!)
            : "This version doesn't build";
        Note(why);
    }

    /// <summary>Takes a value that built: null when taken, else why not (the engine's words); then nothing changes.</summary>
    protected abstract string? Use(object value);

    /// <summary>Says why the entry isn't previewed as it is (null: nothing to say).</summary>
    protected void Note(string? why)
    {
        if (_note is null)
            return;
        _note.ToggleAttribute("hidden", why is null);
        _note.TextContent = why is null ? "" : HasLast ? $"{why}: the preview keeps the last valid version." : $"{why}: nothing to preview yet.";
    }

    /// <summary>Says <paramref name="text"/> as it is under the controls (null: nothing).</summary>
    protected void Say(string? text)
    {
        if (_note is null)
            return;
        _note.ToggleAttribute("hidden", text is null);
        _note.TextContent = text ?? "";
    }

    /// <summary>The note's text ("" while hidden).</summary>
    public string NoteText => _note?.TextContent ?? "";

    /// <summary>A frame of the document: <paramref name="seconds"/> since the last (before the scene moves on).</summary>
    public virtual void Frame(double seconds) { }

    /// <summary>The frame's scene has moved on (the frame's events step).</summary>
    public virtual void AfterFrame() { }

    /// <summary>Space reached the document (P31): true when it played or stopped something.</summary>
    public virtual bool Space() => false;

    /// <summary>The editor's volume changed.</summary>
    public virtual void VolumeChanged() { }

    public virtual void Dispose() { }

    // ---------- The 3D view ----------

    /// <summary>
    /// The preview's <c>surface</c> (its 3D view, the shared <see cref="Viewport"/> shown in it once connected): dragged
    /// with the main button it turns the camera (under the pointer's capture: the press cancelled, so the focus and the
    /// selection stay), the wheel zooms it (P33). Nothing happens while a camera of the scene is looked through.
    /// </summary>
    protected UiElement Surface(UiElement parent)
    {
        var surface = parent.Append("surface", "preview-view");
        Vector2? last = null;
        surface.AddEventListener<UiPointerEvent>("pointerdown", e =>
        {
            if (e.Button != 0 || Part.Viewport is null)
                return;
            e.PreventDefault();
            Part.View.Document.Input.SetPointerCapture(surface);
            last = new Vector2(e.ClientX, e.ClientY);
        });
        surface.AddEventListener<UiPointerEvent>("pointermove", e =>
        {
            if (last is not { } from || Part.Viewport is not { } viewport)
                return;
            var to = new Vector2(e.ClientX, e.ClientY);
            last = to;
            if (viewport.Scene.Camera is null)
                viewport.Orbit.Turn(to.X - from.X, to.Y - from.Y);
        });
        surface.AddEventListener("lostpointercapture", _ => last = null);
        surface.AddEventListener<UiWheelEvent>("wheel", e =>
        {
            if (Part.Viewport is not { } viewport || viewport.Scene.Camera is not null)
                return;
            e.PreventDefault();
            viewport.Orbit.Zoom(e.DeltaY);
        });
        return surface;
    }

    // ---------- Controls ----------

    /// <summary>A button with <paramref name="text"/>, named by <c>data-preview</c>.</summary>
    protected static UiElement Button(UiElement parent, string name, string text, Action click)
    {
        var button = parent.Append("button", null, text);
        button.SetAttribute("type", "button");
        button.SetAttribute("data-preview", name);
        button.AddEventListener("click", _ => click());
        return button;
    }

    /// <summary>A line of the preview: a name and what follows it.</summary>
    protected static UiElement Line(UiElement parent, string name, string? title = null)
    {
        var line = parent.Append("div", "preview-line");
        var label = line.Append("span", "preview-name", name);
        if (title is not null)
            label.SetAttribute("title", title);
        return line;
    }

    /// <summary>
    /// A slider (<c>input type=range</c>, named by <c>data-preview</c>) on its own line with its value written after it;
    /// <paramref name="changed"/> runs as it moves (input).
    /// </summary>
    protected static (UiElement Slider, UiElement Value) Slider(UiElement parent, string name, string label, double min, double max, double step, double value,
        Func<double, string> text, Action<double> changed, string? title = null)
    {
        var line = Line(parent, label, title);
        var slider = line.Append("input", "preview-slider");
        slider.SetAttribute("type", "range");
        slider.SetAttribute("data-preview", name);
        slider.SetAttribute("min", Number(min));
        slider.SetAttribute("max", Number(max));
        slider.SetAttribute("step", Number(step));
        slider.Value = Number(value);
        var shown = line.Append("span", "preview-value", text(value));
        void Moved(UiEvent _)
        {
            var now = slider.ValueAsNumber;
            if (!double.IsFinite(now))
                return;
            shown.TextContent = text(now);
            changed(now);
        }
        // (A range's change always follows its input: input is enough.)
        slider.AddEventListener("input", Moved);
        return (slider, shown);
    }

    /// <summary>The editor's volume slider (P31): 0–100 %.</summary>
    protected void VolumeSlider(UiElement parent)
    {
        var (slider, shown) = Slider(parent, "volume", "Editor volume", 0, 100, 1, Math.Round(Part.Volume * 100), v => $"{v:0} %",
            v => Part.Volume = v / 100, "The volume of everything the editor plays, remembered by the editor.");
        _volume = (slider, shown);
    }

    (UiElement Slider, UiElement Value)? _volume;

    /// <summary>The volume slider shown again with the editor's volume.</summary>
    protected void ShowVolume()
    {
        if (_volume is not var (slider, shown))
            return;
        var percent = Math.Round(Part.Volume * 100);
        slider.Value = Number(percent);
        shown.TextContent = $"{percent:0} %";
    }

    /// <summary>A select of <paramref name="options"/> (value, text), named by <c>data-preview</c>.</summary>
    protected static UiElement Select(UiElement parent, string name, IEnumerable<(string Value, string Text)> options, string selected, Action<string> changed)
    {
        var select = parent.Append("select");
        select.SetAttribute("data-preview", name);
        foreach (var (value, text) in options)
        {
            var option = select.Append("option", null, text);
            option.SetAttribute("value", value);
            if (value == selected)
                option.SetAttribute("selected", "");
        }
        select.AddEventListener("change", _ => changed(select.Value));
        return select;
    }

    /// <summary>A number as the controls write it (invariant, shortest).</summary>
    protected static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>Seconds as the time line writes them: "1.25 s".</summary>
    protected static string Seconds(double value) => value.ToString("0.00", CultureInfo.InvariantCulture) + " s";
}
