using Statventure.Audio;
using Statventure.Core;
using Statventure.Game.Sounds;
using Statventure.UI.Dom;

namespace Statventure.Game.Ui;

/// <summary>
/// The interface sounds by name (GM1-11, Q13; Docs/Game/Foundation.md, "The sounds", Wiring), one per action: every sound
/// asked for while the document handles one action (an input event's dispatch, the tasks and microtasks it runs, until
/// the event loop's next task) is held, and when the action ends the one that says most is played: an event sound
/// (<c>game-saved</c>, <c>game-opened</c>, <c>game-deleted</c>, <c>ui-refused</c>; the first asked of them), else a window
/// opening, else one closing, else a switch changed, else a press. So a click that opens a window sounds only
/// <c>ui-open</c>, Cancel only <c>ui-close</c>, a confirmed Delete only <c>game-deleted</c>. A new input event ends the
/// action before it. The sounds go through the effects bus at their patches' own level (dry); a name with no patch in
/// the data is logged once and skipped. No music is ever started here (Q15).
/// </summary>
sealed class GameSounds(UiDocument document, SoundEffects? effects)
{
    readonly List<string> _asked = [];
    readonly HashSet<string> _missing = new(StringComparer.Ordinal);
    int _flush;

    /// <summary>A sound was played (its name), after the action's sounds were weighed; with or without a sound device.</summary>
    public event Action<string>? Played;

    /// <summary>How much a sound says: the one of an action that says most is played.</summary>
    public static int Weight(string name) => name switch
    {
        SoundNames.Saved or SoundNames.Opened or SoundNames.Deleted or SoundNames.Refused => 4,
        SoundNames.Open => 3,
        SoundNames.Close => 2,
        SoundNames.Toggle => 1,
        _ => 0,
    };

    /// <summary>Asks for <paramref name="name"/> in the action going on (one begins if none is).</summary>
    public void Ask(string name)
    {
        _asked.Add(name);
        if (_flush == 0)
            _flush = document.SetTimeout(EndAction);
    }

    /// <summary>A new action begins (an input event reaches the document): the one before it ends now.</summary>
    public void BeginAction()
    {
        if (_asked.Count > 0)
            EndAction();
    }

    /// <summary>The action ended: the sound of it that says most plays.</summary>
    public void EndAction()
    {
        if (_flush != 0)
            document.ClearTimeout(_flush);
        _flush = 0;
        if (_asked.Count == 0)
            return;
        var chosen = _asked[0];
        foreach (var name in _asked)
            if (Weight(name) > Weight(chosen))
                chosen = name;
        _asked.Clear();
        Play(chosen);
    }

    void Play(string name)
    {
        if (_missing.Contains(name))
            return;
        if (effects is not null)
        {
            try
            {
                effects.Play(name);
            }
            catch (ArgumentException)
            {
                // No patch of that name in the data (SoundEffects registers every audio/effects/<name> at the start).
                _missing.Add(name);
                Log.Warning("Ui", $"sound {name}: no patch at {SoundNames.PatchPath(name)} in the data, skipped");
                return;
            }
        }
        Played?.Invoke(name);
    }
}
