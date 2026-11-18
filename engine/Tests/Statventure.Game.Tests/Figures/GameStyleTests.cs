using Statventure.Core;
using Statventure.Game.Figures;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Sandbox.Figure;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The game's look in its data (GM2 step 5; the user's Q12, GM2-P11): one <c>render.style</c> entry,
/// <c>render/styles/game</c> in data.pak (<c>Assets/Data/render/styles/game.json</c>), the look the user chose ("Ember,
/// hearth": Tools/Models/humanoid/looks/ember-hearth.json, the file Blender's renders read), every number of it. Written
/// from the look file with <c>STATVENTURE_STYLE_DATA=write</c>, then <c>Tools/Scripts/make-data-pack.sh</c>.
/// </summary>
public class GameStyleTests
{
    /// <summary>Set to <c>write</c>: the game's style is written from the look file into Assets/Data.</summary>
    public const string WriteVariable = "STATVENTURE_STYLE_DATA";

    /// <summary>The game's look file (look.py's EMBER_HEARTH as <c>look.py write</c> wrote it).</summary>
    internal static string LookFile => Path.Combine(RaceOracle.Engine, "Tools", "Models", "humanoid", "looks", "ember-hearth.json");

    internal static FigureLook Look => FigureLook.Load(LookFile);

    static string Committed => Path.Combine(RaceOracle.Engine, "Assets", "Data", "render", "styles", "game.json");

    /// <summary>The style the look file gives: its style's and its sun's numbers.</summary>
    internal static RenderStyle FromTheLookFile => RenderStyle.Of(Look.Style, Look.Sun);

    /// <summary>The committed entry is the look file's style and sun, as the record's JSON form (the same bytes).</summary>
    [Fact]
    public void TheGamesStyleIsTheChosenLook()
    {
        var text = RecordSerializer.ToJson(FromTheLookFile);
        if (Environment.GetEnvironmentVariable(WriteVariable) == "write")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Committed)!);
            File.WriteAllText(Committed, text);
        }
        Assert.True(File.Exists(Committed) && File.ReadAllText(Committed) == text,
            $"Assets/Data/render/styles/game.json isn't the game's look file's style and sun: run the game's tests with {WriteVariable}=write (GameStyleTests), then Tools/Scripts/make-data-pack.sh");
        // The look the user chose, as GM2 step 3b wrote it: the ink, the shadow mix, the lines, the hatching, the sun.
        var style = RecordSerializer.FromJson<RenderStyle>(File.ReadAllBytes(Committed));
        Assert.Equal(("#080605", 0.58f, 0.36f, 3.0f, 1.8f, 5f, 1.2f, 0.5f), (style.Ink, style.ShadowMix, style.BandEdge, style.OutlineWidth, style.InnerWidth,
            style.HatchSpacing, style.HatchWidth, style.HatchInk));
        Assert.Equal(new SunLight(new(-0.8f, 0.6f, 0.75f), 3) { Angle = 3 }, style.SunLight);
        Assert.Equal(Look.Style, style.Cel);
    }

    /// <summary>The game finds its style in its packs, as it mounts them (data.pak), the look file's to the number, and it passes its checks.</summary>
    [Fact]
    public void TheGameLoadsItsStyleFromItsPacks()
    {
        using var folder = new ScratchFolder();
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], _ => null));
        var style = GameStyle.Load(data);
        Assert.Equal(FromTheLookFile, style);
        Assert.Equal((Look.Style, Look.Sun), (style.Cel, style.SunLight));
        Assert.Empty(RecordChecks.Check(style));
        Assert.Equal("render/styles/game", GameStyle.Path);
        // The only style the game ships.
        Assert.Equal(["render/styles/game"], data.List("render/styles"));
    }

    /// <summary>A broken style in the game's place (a mod's, say) is refused when the game loads it, naming the rule.</summary>
    [Fact]
    public void ABrokenStyleInTheGamesPlaceIsRefused()
    {
        using var folder = new ScratchFolder();
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        Directory.CreateDirectory(folder["dev/render/styles"]);
        File.WriteAllText(folder["dev/render/styles/game.json"], RecordSerializer.ToJson(FromTheLookFile with { Ink = "soot" }));
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions { DevelopmentFolder = folder["dev"] }, folder["game"], _ => null));
        var problem = Assert.Throws<DataFileException>(() => GameStyle.Load(data));
        Assert.Equal("not a valid render style: \"soot\" isn't a colour (#rrggbb)", problem.Problem);
    }
}
