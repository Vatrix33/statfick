using System.Globalization;
using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.UI.Dom;

namespace Statventure.Editor.Preview;

/// <summary>
/// A sound effect's preview (P31): Play (and Space) plays it once at the play's own volume, pan and pitch on the effects
/// bus, at the editor's volume; the patch is registered again under its name after each valid change (P35), so the next
/// play is the new one (a voice already sounding plays on as it began). Stop stops a theme playing; a sounding effect
/// isn't cut, it ends by itself (the engine's effects have no stop: the manager's decision Q2, 2026-11-07).
/// </summary>
sealed class EffectPreview(PreviewPart part, EditorEntry entry) : PreviewView(part, entry, PreviewKind.Effect)
{
    EffectPatch? _last;
    string? _name;

    protected override bool HasLast => _last is not null;

    /// <summary>The patch played: the last valid (null before one).</summary>
    public EffectPatch? Patch => _last;

    protected override void BuildControls()
    {
        var buttons = Root.Append("div", "preview-buttons");
        Button(buttons, "play", "Play", Play).SetAttribute("title", "Play the sound effect (Space)");
        Button(buttons, "stop", "Stop", Part.Sound.StopMusic).SetAttribute("title", "Stop the music (a sound effect ends by itself)");
        VolumeSlider(Root);
        Slider(Root, "play-volume", "Play volume", 0, 2, 0.05, Part.PlayVolume, Times, v => Part.PlayVolume = v, "This play's volume: 1 plays it as it is made.");
        Slider(Root, "pan", "Pan", -1, 1, 0.05, Part.PlayPan, Pan, v => Part.PlayPan = v, "Where it is heard, from −1 (left) to 1 (right).");
        Slider(Root, "pitch", "Pitch", -24, 24, 1, Part.PlaySemitones, s => Times(PreviewPart.PitchOf(s)), v => Part.PlaySemitones = v,
            "This play's pitch, in semitones: 12 is an octave higher (twice the pitch).");
    }

    protected override string? Use(object value)
    {
        var patch = (EffectPatch)value;
        // The patch's rules as registering checks them (its declared limits among them, G4: registering refuses exactly
        // these), also without sound.
        if (RecordChecks.Check(patch).FirstOrDefault() is { } problem)
            return $"The sound effect isn't playable ({problem.Message})";
        var name = Entry.Name;
        Part.Audio?.Effects.Register(name, patch);
        _last = patch;
        _name = name;
        return null;
    }

    /// <summary>Plays the last valid patch once (nothing before one, or without sound).</summary>
    public void Play()
    {
        if (_name is { } name && Part.Audio is { } audio)
            audio.Effects.Play(name, Part.PlayVolume, Part.PlayPan, PreviewPart.PitchOf(Part.PlaySemitones));
    }

    public override bool Space()
    {
        Play();
        return true;
    }

    public override void VolumeChanged() => ShowVolume();

    static string Times(double factor) => "× " + factor.ToString("0.00", CultureInfo.InvariantCulture);

    static string Pan(double pan) => pan.ToString("0.00", CultureInfo.InvariantCulture);
}

/// <summary>
/// A theme's preview (P31): Play (and Space, which also stops) plays it from the data as edited at the editor's volume,
/// Stop fades it out; while it plays, every valid change of the theme or of the music it is made of reaches it at once,
/// the library swapped in and the theme restarted through its fades when its music changed (P35); a library that
/// doesn't load keeps the last that did, saying why.
/// </summary>
sealed class ThemePreview(PreviewPart part, EditorEntry entry) : PreviewView(part, entry, PreviewKind.Theme)
{
    UiElement _state = null!;
    bool _took;

    protected override bool HasLast => _took || Part.Sound.Library?.Theme(Entry.Name) is not null;

    /// <summary>Whether the preview plays this theme.</summary>
    public bool Playing => Part.Sound.PlayingTheme == Entry.Name;

    protected override void BuildControls()
    {
        var buttons = Root.Append("div", "preview-buttons");
        Button(buttons, "play", "Play", Play).SetAttribute("title", "Play the theme (Space)");
        Button(buttons, "stop", "Stop", Stop).SetAttribute("title", "Stop the theme (Space)");
        _state = buttons.Append("span", "preview-state");
        VolumeSlider(Root);
        State();
    }

    protected override string? Use(object value)
    {
        if (!Playing)
            return null;
        Part.Sound.Follow();
        _took |= Part.Sound.LibraryProblem is null;
        return Part.Sound.LibraryProblem is { } problem ? $"The music doesn't load ({problem})" : null;
    }

    public void Play()
    {
        if (Entry.Builds && Entry.Problems.Count == 0 || HasLast)
        {
            var why = Part.Sound.PlayTheme(Entry.Name);
            _took |= why is null;
            if (why is not null)
                Say($"Not played: {why}.");
            else if (Part.Sound.LibraryProblem is { } problem)
                Note($"The music doesn't load ({problem})");
        }
        State();
    }

    public void Stop()
    {
        Part.Sound.StopMusic();
        State();
    }

    public override bool Space()
    {
        if (Playing)
            Stop();
        else
            Play();
        return true;
    }

    void State() => _state.TextContent = Playing ? "Playing" : "Stopped";

    public override void VolumeChanged() => ShowVolume();
}

/// <summary>
/// An instrument's, a percussion voice's or a room's preview (P31): none of its own; while a theme plays, it says so with
/// Stop and the editor's volume, and every valid change of the entry reaches the theme at once (P35). Nothing shows
/// while no theme plays.
/// </summary>
sealed class MusicPartPreview(PreviewPart part, EditorEntry entry) : PreviewView(part, entry, PreviewKind.MusicPart)
{
    UiElement _line = null!;
    UiElement _theme = null!;

    protected override bool HasLast => Part.Sound.Library is not null;

    protected override void BuildControls()
    {
        _line = Root.Append("div", "preview-music");
        var buttons = _line.Append("div", "preview-buttons");
        Button(buttons, "stop", "Stop", Stop).SetAttribute("title", "Stop the theme (Space)");
        _theme = buttons.Append("span", "preview-state");
        VolumeSlider(_line);
        Shown();
    }

    protected override string? Use(object value)
    {
        Shown();
        if (Part.Sound.PlayingTheme is null)
            return null;
        Part.Sound.Follow();
        return Part.Sound.LibraryProblem is { } problem ? $"The music doesn't load ({problem})" : null;
    }

    void Stop()
    {
        Part.Sound.StopMusic();
        Shown();
        Note(null);
    }

    public override bool Space()
    {
        if (Part.Sound.PlayingTheme is null)
            return false;
        Stop();
        return true;
    }

    void Shown()
    {
        var playing = Part.Sound.PlayingTheme;
        // Nothing shows while no theme plays (the instruments, percussion voices and rooms have no preview of their own).
        Root.ToggleAttribute("hidden", playing is null);
        _theme.TextContent = playing is null ? "" : $"Playing {playing}, which takes this entry's changes";
    }

    public override void VolumeChanged() => ShowVolume();
}
