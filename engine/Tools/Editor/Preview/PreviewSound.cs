using Statventure.Audio;
using Statventure.Packs;

namespace Statventure.Editor.Preview;

/// <summary>
/// What the previews give the sound (P31, P35): the music library made from the data as edited
/// (<c>MusicLibrary.Load</c> on <c>Session.EditedData</c>) and swapped in (<c>AudioSystem.Reload</c>, step 3), so a theme
/// playing restarts through its fades when its music changed; the last library that loaded kept when the data as
/// edited doesn't load (one theme that doesn't pass refuses the whole library, step 5), with why. The theme the preview
/// plays, which a change of any music entry reaches while it plays.
/// </summary>
sealed class PreviewSound(PreviewPart part)
{
    MusicLibrary? _library;
    bool _stale = true;

    AudioSystem? Audio => part.Audio;

    /// <summary>The theme the preview plays (null: none).</summary>
    public string? PlayingTheme { get; private set; }

    /// <summary>Why the data as edited doesn't give a music library now (the last one that did plays on), or null.</summary>
    public string? LibraryProblem { get; private set; }

    /// <summary>The library the sound has from the preview (null before the first that loaded).</summary>
    public MusicLibrary? Library => _library;

    /// <summary>The data changed: the library is made again when next needed (<see cref="Refresh"/>).</summary>
    public void DataChanged() => _stale = true;

    /// <summary>
    /// The library from the data as edited, given to the sound when it differs from the last; false (the last kept,
    /// <see cref="LibraryProblem"/> saying why) when it doesn't load.
    /// </summary>
    public bool Refresh()
    {
        if (!_stale)
            return _library is not null && LibraryProblem is null;
        _stale = false;
        if (part.Session.EditedData is not { } data)
        {
            LibraryProblem = part.Session.EditedDataProblem ?? "no pack is open";
            return false;
        }
        MusicLibrary library;
        try
        {
            library = MusicLibrary.Load(data);
        }
        catch (IOException problem)
        {
            LibraryProblem = problem is DataFileException file ? file.Message : problem.Message;
            return false;
        }
        LibraryProblem = null;
        _library = library;
        Audio?.Reload(library);
        return true;
    }

    /// <summary>
    /// Plays the theme <paramref name="id"/> from the data as edited (the library made again first if the data
    /// changed); null when it played, else why not.
    /// </summary>
    public string? PlayTheme(string id)
    {
        if (Audio is not { } audio)
            return "the sound is off";
        Refresh();
        if (_library?.Theme(id) is null)
            return LibraryProblem ?? $"the music has no theme {id}";
        audio.Music.Play(id);
        PlayingTheme = id;
        return null;
    }

    /// <summary>Stops the theme the preview plays (music.js's fade out).</summary>
    public void StopMusic()
    {
        if (PlayingTheme is null)
            return;
        PlayingTheme = null;
        Audio?.Music.Stop();
    }

    /// <summary>While a theme plays, the data as edited reaches it at once (P35): the library made again and swapped in.</summary>
    public void Follow()
    {
        if (PlayingTheme is not null)
            Refresh();
    }
}
