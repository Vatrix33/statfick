using Statventure.Core;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.UI;

namespace Statventure.Editor;

/// <summary>
/// The editor's program: one window showing the view's document through a <see cref="UiHost"/>, its title the
/// view's, its page zoom the view's (P1, P4).
/// </summary>
public sealed class EditorApplication(GameData game, string? playerFolder) : Application(new ApplicationOptions
{
    Window = new WindowOptions { Title = EditorView.Name, Width = 1280, Height = 800, MinWidth = 800, MinHeight = 500 },
    // No sound until the preview (step 13) needs it.
    Audio = false,
    GameFolder = playerFolder,
})
{
    VirtualFileSystem _uiData = null!;
    EditorSession _session = null!;
    EditorView _view = null!;
    UiHost _host = null!;

    protected override void Load()
    {
        _uiData = new VirtualFileSystem([Mount.Pack(game.UiPack)]);
        _session = new EditorSession(game);
        _view = new EditorView(_session, _uiData);
        _host = new UiHost(Window, Gpu, _view.Document);
        _view.ApplyZoom = zoom => _host.PageZoom = zoom;
        _view.TitleChanged += () => Window.Title = _view.Title;
        Window.Title = _view.Title;
    }

    protected override void Update(in GameTime time) => _host.Update(time.Total);

    protected override void Render(Frame frame, GpuImage target, in GameTime time) => _host.Render(frame, target);

    protected override void Unload()
    {
        _host.Dispose();
        _view.Dispose();
        _session.Dispose();
        _uiData.Dispose();
    }
}
