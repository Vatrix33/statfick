using System.Numerics;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor;

/// <summary>
/// The editor without a window (P2): a session on the repo's packs (the dev setup) and its view, the document at a
/// viewport size (CSS px) and a device scale, driven by the UI's input as a window host would drive it.
/// </summary>
public sealed class EditorHarness : IDisposable
{
    public EditorHarness(float width = 1000, float height = 640, float scale = 1, params Type[] parts)
    {
        UiData = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "ui.pak"))]);
        Session = new EditorSession(GameData.Repo(EngineFolder));
        View = new EditorView(Session, UiData, parts);
        Document.SetViewport(width, height);
        Document.SetZoom(scale);
        Document.Update(0);
    }

    /// <summary>The repo's engine/ folder (the dev setup's).</summary>
    public static string EngineFolder => Path.GetFullPath(Path.Combine(TestPaths.Packs, "..", ".."));

    public VirtualFileSystem UiData { get; }
    public EditorSession Session { get; }
    public EditorView View { get; }
    public UiDocument Document => View.Document;
    public UiInput Input => Document.Input;

    /// <summary>The first element <paramref name="selector"/> matches.</summary>
    public UiElement Q(string selector) => Document.QuerySelector(selector) ?? throw new InvalidOperationException($"nothing matches {selector}");

    /// <summary>An element's box (CSS px).</summary>
    public Statventure.Core.RectF Rect(string selector) => Q(selector).GetBoundingClientRect();

    /// <summary>The middle of an element (CSS px, the window's units at scale 1).</summary>
    public Vector2 Middle(UiElement element)
    {
        var rect = element.GetBoundingClientRect();
        return new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2) * Document.PageZoom;
    }

    double _time;

    /// <summary>A frame, a little later than the last.</summary>
    public void Frame() => Document.Update(_time += 1 / 60.0);

    /// <summary>A click of the main button at <paramref name="at"/> (window units).</summary>
    public void Click(Vector2 at)
    {
        Input.PointerMove(at);
        Input.PointerDown(at, MouseButton.Left);
        Input.PointerUp(at, MouseButton.Left);
        Frame();
    }

    public void Click(UiElement element) => Click(Middle(element));

    /// <summary>A key pressed and released (DOM key value and code).</summary>
    public bool Key(string key, string code, KeyModifiers modifiers = KeyModifiers.None)
    {
        var allowed = Input.KeyDown(key, code, modifiers);
        Input.KeyUp(key, code, modifiers);
        Frame();
        return allowed;
    }

    /// <summary>A drag with the main button from <paramref name="from"/> through <paramref name="to"/> (window units).</summary>
    public void Drag(Vector2 from, params Vector2[] to)
    {
        Input.PointerMove(from);
        Input.PointerDown(from, MouseButton.Left);
        Frame();
        foreach (var point in to)
        {
            Input.PointerMove(point);
            Frame();
        }
        Input.PointerUp(to[^1], MouseButton.Left);
        Frame();
    }

    public void Dispose()
    {
        View.Dispose();
        Session.Dispose();
        UiData.Dispose();
    }
}
