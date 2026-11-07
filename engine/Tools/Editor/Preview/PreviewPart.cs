using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Graphics;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Editor.Preview;

/// <summary>
/// The preview pane (Docs/Editor.md, "The preview (step 13)"; M11-3, M11-13, P31–P35): the selected entry seen or heard
/// as the game would show it, through the engine's own sound system, scene view and sequence player. Sound effects and
/// themes play and stop at the editor's volume (P31); particle effects loop (P32); models show their clips and turn under
/// a camera (P33); cutscenes play, pause and seek with stand-ins (P34). A valid change is taken at once; an invalid one
/// (a draft that doesn't build, problems, or what the engine itself refuses) keeps the last valid state, saying so
/// (M11-12, P35). State machines, UI files, terrain and the other kinds show nothing. The program connects the window's
/// GPU, the host's surface views and the sound (<see cref="Connect"/>); without them the controls still show, nothing
/// plays and the 3D view stays empty (the tests' editor without a window).
/// </summary>
public sealed class PreviewPart : EditorPart
{
    /// <summary>The background of the preview's 3D view: the shell's pane colour (<c>--paper-deep</c>).</summary>
    public const string Background = "#292c28";

    UiElement _element = null!;
    PreviewView? _view;
    double? _volume;
    readonly PreviewClock _clock;

    public PreviewPart() => _clock = new PreviewClock(this);

    public override EditorPane? Pane => EditorPane.Preview;

    /// <summary>The GPU the 3D previews draw on, once connected.</summary>
    public GpuDevice? Gpu { get; private set; }

    /// <summary>The host's surface views the 3D previews show in, once connected.</summary>
    public SurfaceViews? Views { get; private set; }

    /// <summary>The sound the previews play on (the application's), once connected; null: silent.</summary>
    public AudioSystem? Audio { get; private set; }

    /// <summary>The 3D view and scene the previews share (made when a GPU is connected).</summary>
    internal Viewport? Viewport { get; private set; }

    /// <summary>The music and the effects as the previews give them to the sound.</summary>
    internal PreviewSound Sound { get; private set; } = null!;

    /// <summary>The preview shown now (null: none for the entry, or nothing selected).</summary>
    internal PreviewView? Shown => _view;

    /// <summary>The part's element in the preview pane.</summary>
    public UiElement Element => _element;

    protected internal override void Attach(UiElement? element)
    {
        _element = element!;
        _element.AddClass("preview-part");
        Sound = new PreviewSound(this);
        Session.SelectionChanged += Show;
        Session.EntryChanged += entry =>
        {
            Sound.DataChanged();
            if (_view is { } view && view.Entry == entry)
                view.Take();
        };
        Session.EntriesChanged += _ =>
        {
            Sound.DataChanged();
            if (_view is { } view && Session.Entries.Contains(view.Entry))
                view.Take();
        };
        Session.PackOpened += Sound.DataChanged;
        View.Document.AddFrameClient(_clock);
        View.Document.AddEventListener<UiKeyboardEvent>("keydown", Key);
        Show();
    }

    /// <summary>
    /// Gives the preview the window's GPU, the host's surface views and the application's sound (Docs/Editor.md, step 13,
    /// Q1: <c>EditorApplication.Load</c> calls it once the host is made); the sound takes the editor's volume. Without a GPU
    /// or views there is no 3D view; without sound nothing plays (the tests leave out what they don't need).
    /// </summary>
    public void Connect(GpuDevice? gpu, SurfaceViews? views, AudioSystem? audio)
    {
        Leave(null);
        if (Viewport is { } old)
        {
            View.Document.RemoveFrameClient(old.Scene);
            old.Dispose();
        }
        Viewport = null;
        Gpu = gpu;
        Views = views;
        Audio = audio;
        if (gpu is not null && views is not null)
        {
            Viewport = new Viewport(gpu, views);
            View.Document.AddFrameClient(Viewport.Scene);
            Session.Own(Viewport);
            // Disposed before the view (last first): the preview shown gives back its GPU models while the view is there.
            Session.Own(new Closer(this));
        }
        if (audio is not null)
            audio.MasterVolume = Volume;
        Show();
    }

    /// <summary>
    /// The editor's volume, 0 to 1 (P31): the sound's master volume, remembered in <c>editor/volume.json</c> (G6), 50 %
    /// when nothing is remembered (Q3).
    /// </summary>
    public double Volume
    {
        get => _volume ??= VolumeFile.Read(Session.PlayerFolder);
        set
        {
            var volume = Math.Clamp(double.IsFinite(value) ? value : VolumeFile.Default, 0, 1);
            if (_volume == volume)
                return;
            _volume = volume;
            if (Audio is { } audio)
                audio.MasterVolume = volume;
            VolumeFile.Write(Session.PlayerFolder, volume);
            _view?.VolumeChanged();
        }
    }

    /// <summary>The particle preview's colours the user gave, by slot, kept while the editor runs (P32: not saved in the pack).</summary>
    internal Dictionary<string, string> ParticleColours { get; } = new(StringComparer.Ordinal);

    Dictionary<string, Dictionary<string, StandIn>>? _standIns;

    /// <summary>The stand-ins chosen for the cutscene at <paramref name="cutscene"/>, by role (P34; <c>editor/stand-ins.json</c>, read once).</summary>
    public IReadOnlyDictionary<string, StandIn> StandInsOf(string cutscene) =>
        (_standIns ??= StandInsFile.Read(Session.PlayerFolder)).GetValueOrDefault(cutscene) ?? new Dictionary<string, StandIn>();

    /// <summary>A stand-in chosen for <paramref name="role"/> of the cutscene at <paramref name="cutscene"/>, remembered at once (G6).</summary>
    public void SetStandIn(string cutscene, string role, StandIn standIn)
    {
        _standIns ??= StandInsFile.Read(Session.PlayerFolder);
        if (!_standIns.TryGetValue(cutscene, out var roles))
            _standIns[cutscene] = roles = new(StringComparer.Ordinal);
        roles[role] = standIn;
        StandInsFile.Write(Session.PlayerFolder, _standIns);
    }

    /// <summary>A sound effect's play: its volume (a factor, 1 as it is made), kept while the editor runs (P31).</summary>
    public double PlayVolume { get; set; } = 1;

    /// <summary>A sound effect's play: its pan, from −1 (left) to 1 (right).</summary>
    public double PlayPan { get; set; }

    /// <summary>A sound effect's play: its pitch in semitones (0: as it is made; 12 an octave higher).</summary>
    public double PlaySemitones { get; set; }

    /// <summary>The pitch factor of <paramref name="semitones"/> (2 to the power of a twelfth of them).</summary>
    public static double PitchOf(double semitones) => Math.Pow(2, semitones / 12);

    /// <summary>Shows the preview of the selected entry: the one shown goes (stopping what it played) unless it is the same.</summary>
    void Show()
    {
        var entry = Session.Selected;
        var kind = entry is null ? PreviewKind.None : PreviewView.KindOf(entry);
        if (_view is { } shown && shown.Entry == entry && shown.Kind == kind)
            return;
        Leave(entry);
        if (kind == PreviewKind.None)
            return;
        _view = PreviewView.For(this, entry!, kind);
        _view.Build(_element.Append("div", "preview"));
        _view.Take();
    }

    /// <summary>The preview shown goes; a theme playing stops unless <paramref name="next"/> is a part of the music (P31).</summary>
    void Leave(EditorEntry? next)
    {
        if (_view is { } view)
        {
            _view = null;
            view.Dispose();
        }
        _element.ClearChildren();
        if (next is null || !PreviewView.KeepsTheMusic(next, Sound.PlayingTheme))
            Sound.StopMusic();
    }

    /// <summary>Space plays or stops the sound previewed (P31) when it reaches the document unhandled and isn't a control's.</summary>
    void Key(UiKeyboardEvent e)
    {
        if (e.DefaultPrevented || e.Key != " " || e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey || View.Dialogs.Top is not null)
            return;
        if (e.Target is { } target && TakesSpace(target))
            return;
        if (_view?.Space() == true)
            e.PreventDefault();
    }

    /// <summary>
    /// Whether Space on <paramref name="target"/> is its own: a field that takes text, or a control Space works (a button,
    /// a box, a select, a slider, a group's summary, a link, a tab). The entry list's Space isn't its own unless it typed
    /// ahead (then it cancelled the key), so Space plays the sound selected in the list.
    /// </summary>
    static bool TakesSpace(UiElement target) =>
        EditorCommands.TakesText(target) || target.IsFormControl || target.Tag is "summary" or "a"
        || target.GetAttribute("role") is "tab" or "button" or "checkbox" or "switch";

    /// <summary>Takes the preview shown away when the session goes (its scene, its GPU models, the music it played).</summary>
    sealed class Closer(PreviewPart part) : IDisposable
    {
        public void Dispose()
        {
            if (part._view is { } view)
            {
                part._view = null;
                view.Dispose();
            }
        }
    }

    /// <summary>The preview's own clock: each frame of the document, the preview shown moves on.</summary>
    sealed class PreviewClock(PreviewPart part) : IUiFrameClient
    {
        public void AdvanceFrame(double seconds) => part._view?.Frame(seconds);

        public void DispatchFrameEvents(UiEventLoop loop) => part._view?.AfterFrame();
    }

    /// <summary>The surface's background as the view takes it (linear).</summary>
    internal static System.Numerics.Vector4 BackgroundColour => Colors.LinearFromHex(Background);
}
