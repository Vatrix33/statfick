namespace Statventure.UI.Dom;

/// <summary>
/// Something that animates with a document's frames (<see cref="UiDocument.AddFrameClient"/>): an
/// animator of 3D models (Docs/Animation.md).
/// </summary>
public interface IUiFrameClient
{
    /// <summary>The frame's animation timing update: <paramref name="seconds"/> since its last frame.</summary>
    void AdvanceFrame(double seconds);

    /// <summary>The frame's animation events step: deliver what the update queued, each delivery as game code on <paramref name="loop"/>.</summary>
    void DispatchFrameEvents(UiEventLoop loop);
}
