using System.Numerics;
using Statventure.Audio;
using Statventure.Game.Settings;
using Statventure.Game.Ui;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The game's document without a window (GM1 step 8): <see cref="GameUi.Load(Core.Application, GameSettings, VirtualFileSystem)"/>
/// for a game of the test's own on a scratch per-user folder, its data the committed packs (or none), its settings that
/// folder's; the document at a viewport size (CSS px) and a device scale, driven by the UI's input as a window host would
/// drive it, with what it played, the commands it ran and the folder and quit hooks recorded. Optionally a sound of its
/// own (an audio graph with no output or thread, the committed patches registered), so the sounds go through the engine.
/// </summary>
sealed class GameUiHarness : IDisposable
{
    readonly ScratchFolder _folder = new();
    readonly GameStorage _storage;
    readonly StatFickGame _game;
    readonly VirtualFileSystem _data;
    double _time;

    public GameUiHarness(float width = 800, float height = 600, float scale = 1, GameTheme? theme = null, bool packs = true, bool sound = false, Action<GameSettings>? before = null, string? extra = null)
    {
        _storage = new GameStorage(_folder["p"]);
        Settings = GameSettings.Load(_storage);
        if (theme is { } t)
            Settings.Theme = t;
        before?.Invoke(Settings);
        _game = GameTesting.Game(_folder["p"]);
        List<Mount> mounts = packs ? [Mount.Pack(Path.Combine(TestPaths.Packs, "ui.pak")), Mount.Pack(Path.Combine(TestPaths.Packs, "audio.pak"))] : [];
        if (extra is not null)
            mounts.Add(Mount.Pack(extra));
        _data = new VirtualFileSystem(mounts);
        if (sound)
            Audio = new AudioSystem(new AudioGraph(48000), MusicLibrary.Load(_data));
        Ui = GameUi.Load(_game, Settings, _data, Audio);
        Ui.OpenGameFolder = () =>
        {
            Done.Add("open game folder");
            return OpenFolderWorks;
        };
        Ui.QuitGame = () => Done.Add("quit");
        Ui.SoundPlayed += Sounds.Add;
        Document.SetViewport(width, height);
        Document.SetZoom(scale, Document.PageZoom);
        Frame();
    }

    public GameSettings Settings { get; }
    public GameUi Ui { get; }
    public AudioSystem? Audio { get; }
    public UiDocument Document => Ui.Document;
    public UiInput Input => Document.Input;

    /// <summary>The sounds played, in order.</summary>
    public List<string> Sounds { get; } = [];

    /// <summary>What the folder and quit hooks were asked to do.</summary>
    public List<string> Done { get; } = [];

    /// <summary>What the folder hook answers.</summary>
    public bool OpenFolderWorks { get; set; } = true;

    /// <summary>Registers <paramref name="names"/> as commands that record their runs in <paramref name="record"/>.</summary>
    public void Commands(List<string> record, params string[] names)
    {
        foreach (var name in names)
            Ui.Commands.Register(name, argument => record.Add(argument is null ? name : $"{name} {argument}"));
    }

    /// <summary>The first element <paramref name="selector"/> matches.</summary>
    public UiElement Q(string selector) => Document.QuerySelector(selector) ?? throw new InvalidOperationException($"nothing matches {selector}");

    /// <summary>The middle of an element (window units).</summary>
    public Vector2 Middle(UiElement element)
    {
        var rect = element.GetBoundingClientRect();
        return new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2) * Document.PageZoom;
    }

    /// <summary>A frame, a little later than the last (the event loop's tasks run: an action's sound is played).</summary>
    public void Frame() => Document.Update(_time += 1 / 60.0);

    /// <summary>Frames for <paramref name="seconds"/>.</summary>
    public void Wait(double seconds)
    {
        var end = _time + seconds;
        while (_time < end)
            Frame();
    }

    /// <summary>The pointer moved to <paramref name="at"/> (window units).</summary>
    public void Move(Vector2 at)
    {
        Input.PointerMove(at);
        Frame();
    }

    /// <summary>A click of the main button at <paramref name="at"/> (window units).</summary>
    public void Click(Vector2 at)
    {
        Input.PointerMove(at);
        Input.PointerDown(at, MouseButton.Left);
        Input.PointerUp(at, MouseButton.Left);
        Frame();
    }

    public void Click(UiElement element) => Click(Middle(element));

    /// <summary>A key pressed and released (DOM key value and code); false when the page cancelled its keydown.</summary>
    public bool Key(string key, string code, KeyModifiers modifiers = KeyModifiers.None)
    {
        var allowed = Input.KeyDown(key, code, modifiers);
        Input.KeyUp(key, code, modifiers);
        Frame();
        return allowed;
    }

    /// <summary>Text typed into the focused element, a character at a time.</summary>
    public void Type(string text)
    {
        foreach (var c in text)
            Key(c.ToString(), "");
    }

    /// <summary>A window layer in the old markup: a backdrop, a dialog with a close button and two more buttons.</summary>
    public UiElement Layer(string id, bool close = true)
    {
        var layer = Markup.Element($"""
            <div class="modal-layer" id="{id}" hidden><button class="modal-backdrop" type="button" aria-label="Close"></button><section class="spell-picker-dialog" role="dialog" aria-modal="true"><header class="dialog-header"><div><p class="eyebrow">TEST</p><h2>{id}</h2></div>{(close ? """<button class="modal-close" type="button" aria-label="Close">×</button>""" : "")}</header><button class="one" type="button">One</button><button class="disabled" type="button" disabled>Off</button><button class="two" type="button">Two</button></section></div>
            """);
        return layer;
    }

    public void Dispose()
    {
        Ui.Dispose();
        Audio?.Dispose();
        _data.Dispose();
        _game.Dispose();
        _storage.Dispose();
        _folder.Dispose();
    }
}
