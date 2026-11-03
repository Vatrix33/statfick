using Statventure.Audio;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Records;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Audio;

/// <summary>
/// The application's sound plays the music in its data (Docs/Assets.md, step 6; Q3): a real window
/// and the default output device (silent: nothing is played), the music from a development folder
/// holding the committed pack's entries with one theme changed. Needs a display, as the swapchain test.
/// </summary>
public class ApplicationMusicTests
{
    static bool HasDisplay => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));

    sealed class Probe(string development) : Application(new ApplicationOptions
    {
        Window = new WindowOptions { Title = "Statventure music test", Width = 160, Height = 100 },
        DevelopmentFolder = development,
    })
    {
        public MusicLibrary? Music;

        protected override void Update(in GameTime time)
        {
            Music = Audio?.Music.Library;
            Quit();
        }

        protected override void Render(Frame frame, GpuImage target, in GameTime time) { }
    }

    [Fact]
    public void TheSoundPlaysTheMusicOfTheData()
    {
        if (!HasDisplay)
            return;
        using var folder = new TempFolder();
        using (var pack = PackFile.Open(TestMusic.Pack))
            pack.ExtractTo(folder["dev"]);
        var changed = TestMusic.Library.Theme("alder-vale")! with { Tempo = 141 };
        File.WriteAllText(folder["dev/audio/themes/alder-vale.json"], RecordSerializer.ToJson(changed));
        var probe = new Probe(folder["dev"]);
        probe.Run();
        Assert.NotNull(probe.Music);
        Assert.Equal(TestMusic.Library.Themes, probe.Music.Themes);
        Assert.Equal(141, probe.Music.Theme("alder-vale")!.Tempo);
    }
}
