using Statventure.Editor;
using Statventure.Editor.Preview;
using Statventure.Editor.Session;
using Statventure.Tests.Core;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Editor.Preview;

using PreviewPart = Statventure.Editor.Preview.PreviewPart;

/// <summary>
/// The program wires the preview (Docs/Editor.md, "How it works", step 13; the manager's decision Q1, 2026-11-07): the sound
/// on, and the preview given the window's GPU, the host's surface views and the application's sound, which takes the
/// editor's volume. A real application in a hidden window counted as minimized (nothing shown); its sound is the default
/// device's, and nothing is played on it. In the "Window" collection (GLFW is one for the process).
/// </summary>
[Collection("Window")]
public class PreviewApplicationTests
{
    [Fact]
    public void TheProgramConnectsThePreviewToItsGpuViewsAndSound()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        var player = folder["player"];
        VolumeFile.Write(player, 0.25);
        string? seen = null;
        var application = new EditorApplication(GameData.Repo(EditorHarness.EngineFolder), player, visible: false)
        {
            AfterLoad = a =>
            {
                a.Window.MinimizedForTests = true;
                var preview = a.View.Part<PreviewPart>();
                seen = $"gpu {preview.Gpu == a.Gpu}, views {preview.Views is not null}, viewport {preview.Viewport is not null}, sound {preview.Audio is not null && preview.Audio == a.Audio}, volume {a.Audio?.MasterVolume}";
            },
            AfterUpdate = a => a.Quit(),
        };
        application.Run();
        Assert.Equal("gpu True, views True, viewport True, sound True, volume 0.25", seen);
    }
}
