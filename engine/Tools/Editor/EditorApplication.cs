using Statventure.Core;
using Statventure.Editor.Browser;
using Statventure.Editor.Preview;
using Statventure.Editor.Session;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.UI;

namespace Statventure.Editor;

/// <summary>
/// The editor's program: one window showing the view's document through a <see cref="UiHost"/>, its title the
/// view's, its page zoom the view's (P1, P4). Its data is the game's packs only (<see cref="ApplicationOptions.GamePacksOnly"/>,
/// P6: nothing of the player's can stop its start); files dropped on the window open or import (P27); closing with
/// unsaved work asks first (M11-17, <see cref="ConfirmClose"/>); the page zoom and the window's last normal size are
/// remembered in <c>editor/shell.json</c> (P4, G6): read before the window opens, written when the editor closes.
/// </summary>
public sealed class EditorApplication : Application
{
    readonly GameData _game;
    readonly ShellSettings _shell;
    VirtualFileSystem _uiData = null!;
    EditorSession _session = null!;
    EditorView _view = null!;
    UiHost _host = null!;
    int _width, _height;

    /// <summary>The editor as its program makes it: its window shown, its logs written (M12-8, <see cref="ApplicationOptions.LogName"/>).</summary>
    public EditorApplication(GameData game, string? playerFolder) : this(game, playerFolder, visible: true, logName: "statventure-editor")
    {
    }

    /// <summary>The editor with its window <paramref name="visible"/> or not (tests make theirs hidden), writing no log unless given <paramref name="logName"/>.</summary>
    internal EditorApplication(GameData game, string? playerFolder, bool visible, string? logName = null)
        : this(game, ShellSettings.Read(playerFolder ?? GameFolder.ForThisMachine()), playerFolder, visible, logName)
    {
    }

    EditorApplication(GameData game, ShellSettings shell, string? playerFolder, bool visible, string? logName)
        : base(OptionsFor(shell, playerFolder, visible) with { LogName = logName })
    {
        _game = game;
        _shell = shell;
        _width = Options.Window.Width;
        _height = Options.Window.Height;
    }

    /// <summary>
    /// The application's options: the window (step 4's: "Statventure Editor", 1280 × 800 unless <paramref name="shell"/>
    /// remembers a size, at least 800 × 500), the sound (the preview's, step 13), the player's folder, and the game's
    /// packs only for its data (P6).
    /// </summary>
    internal static ApplicationOptions OptionsFor(ShellSettings shell, string? playerFolder, bool visible = true) => new()
    {
        Window = new WindowOptions
        {
            Title = EditorView.Name,
            Width = shell.Width ?? ShellSettings.DefaultWidth,
            Height = shell.Height ?? ShellSettings.DefaultHeight,
            MinWidth = ShellSettings.MinWidth,
            MinHeight = ShellSettings.MinHeight,
            Visible = visible,
        },
        // The preview's sound (step 13).
        Audio = true,
        GameFolder = playerFolder,
        GamePacksOnly = true,
    };

    /// <summary>The session (tests).</summary>
    internal EditorSession Session => _session;

    /// <summary>The view (tests).</summary>
    internal EditorView View => _view;

    /// <summary>Called after each update (tests drive the editor from it).</summary>
    internal Action<EditorApplication>? AfterUpdate { get; init; }

    /// <summary>Called at the end of <see cref="Load"/> (tests).</summary>
    internal Action<EditorApplication>? AfterLoad { get; init; }

    protected override void Load()
    {
        _uiData = new VirtualFileSystem([Mount.Pack(_game.UiPack)]);
        _session = new EditorSession(_game) { PlayerFolder = Storage.Folder };
        _view = new EditorView(_session, _uiData);
        _host = new UiHost(Window, Gpu, _view.Document);
        _host.Audio = Audio;
        _view.Part<PreviewPart>().Connect(Gpu, _host.Views, Audio);
        _view.ApplyZoom = zoom => _host.PageZoom = zoom;
        _view.ZoomLevel = _shell.ZoomLevel;
        _view.TitleChanged += () => Window.Title = _view.Title;
        Window.Title = _view.Title;
        Window.PathsDropped += paths => _view.Part<Opener>().Drop(paths);
        AfterLoad?.Invoke(this);
    }

    protected override void Update(in GameTime time)
    {
        _host.Update(time.Total);
        KeepNormalSize();
        AfterUpdate?.Invoke(this);
    }

    protected override void Render(Frame frame, GpuImage target, in GameTime time) => _host.Render(frame, target);

    /// <summary>Closing with unsaved work asks first (M11-17): the view's question, the loop going on until it is answered.</summary>
    protected override bool ConfirmClose() => _view.Part<Opener>().ConfirmClose(Quit);

    /// <summary>The window's size while it is normal (not minimized, maximized or full screen): what closing remembers.</summary>
    void KeepNormalSize()
    {
        var (width, height, normal) = WindowStateForTests?.Invoke()
            ?? (Window.Width, Window.Height, !Window.Native.IsIconified && !Window.Native.IsMaximised && !Window.Fullscreen);
        if (!normal || width < 1 || height < 1)
            return;
        _width = width;
        _height = height;
    }

    /// <summary>The window's size and whether it is normal, in place of the window's own (tests: a hidden window can't be resized or maximized everywhere).</summary>
    internal Func<(int Width, int Height, bool Normal)>? WindowStateForTests { get; set; }

    protected override void Unload()
    {
        KeepNormalSize();
        new ShellSettings(_view.ZoomLevel, _width, _height).Write(Storage.Folder);
        _host.Dispose();
        _view.Dispose();
        _session.Dispose();
        _uiData.Dispose();
    }
}
