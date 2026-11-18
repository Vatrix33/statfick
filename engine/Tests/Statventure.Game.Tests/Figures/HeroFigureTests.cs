using System.Numerics;
using Statventure.Animation;
using Statventure.Game.Data.Looks;
using Statventure.Game.Figures;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Sandbox.Figure;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The game's figure builder (GM2 step 5, GM2-P8; <see cref="HeroFigure"/>): what a look comes to in the data (the body's
/// model, each part's model for that body in the maker's order, the colours of the race's swatches by name, the gear's
/// garments, items and colours), what it refuses (all a look names that the data hasn't, GM2-P18), and the figure it
/// builds from the game's own models (the body, its parts as garments bound to its bones, the gear's items on its sockets,
/// the palette).
/// </summary>
[Collection("GPU")]
public class HeroFigureTests(GpuFixture gpu)
{
    static readonly FigureGear SampleGear = new()
    {
        Garments = ["models/humanoid/bodies/human-male-strong"],
        Items = [new FigureItem("models/humanoid/parts/face/human-male-1/human-male", "socket.hand.R")],
        Colours = new Dictionary<string, string> { ["metal.base"] = "#80848c", ["wood"] = "#5a2a1e" },
    };

    /// <summary>A look's plan: its body's model, each part with its model for that body (none for a part drawn as nothing), the race's swatches' colours by slot.</summary>
    [Fact]
    public void APlanIsTheBodyThePartsForItAndTheSwatchesColours()
    {
        using var looks = new TestLooks();
        var plan = HeroFigure.Resolve(looks.Data, TestLooks.WayfarerLook);
        Assert.Equal("models/humanoid/bodies/human-male-average", plan.Body);
        Assert.Equal(
        [
            new FigurePart("face", "face-human-male-1", "models/humanoid/parts/face/human-male-1/human-male"),
            new FigurePart("hair", "hair-braid", "models/humanoid/parts/hair/braid/human-male"),
            new FigurePart("beard", "beard-none", null),
        ], plan.Parts);
        Assert.Equal(["models/humanoid/parts/face/human-male-1/human-male", "models/humanoid/parts/hair/braid/human-male"], plan.GarmentModels);
        Assert.Equal(new Dictionary<string, string> { ["hair"] = "#8a8a8a", ["skin"] = "#f0c8a0" }, plan.Colours);
        Assert.Empty(plan.Garments);
        Assert.Empty(plan.Items);
        // Other swatches by their names; the woman's braid is her body's model of the part.
        var woman = HeroFigure.Resolve(looks.Data, new LookHero
        {
            Race = TestLooks.Human, Sex = LookNames.Female, Body = TestLooks.HumanFemale,
            Parts = new Dictionary<string, string> { ["hair"] = "hair-braid" },
            Colours = new Dictionary<string, string> { ["skin"] = "Umber", ["hair"] = "Raven" },
        });
        Assert.Equal(("models/humanoid/bodies/human-female-average", "models/humanoid/parts/hair/braid/human-female"), (woman.Body, woman.Parts.Single().Model));
        Assert.Equal(new Dictionary<string, string> { ["hair"] = "#1f1a17", ["skin"] = "#8a5a3c" }, woman.Colours);
    }

    /// <summary>The parts in the maker's order of categories whatever the look's map's order, a key that isn't a category after them by name.</summary>
    [Fact]
    public void ThePartsComeInTheMakersOrder()
    {
        using var looks = new TestLooks();
        var look = TestLooks.WayfarerLook with
        {
            Parts = new Dictionary<string, string> { ["zz"] = "beard-none", ["beard"] = "beard-none", ["aa"] = "hair-bald", ["hair"] = "hair-braid", ["face"] = "face-human-male-1" },
        };
        Assert.Equal(["face", "hair", "beard", "aa", "zz"], HeroFigure.Resolve(looks.Data, look).Parts.Select(p => p.Category));
    }

    /// <summary>A category the body offers and the look doesn't choose draws nothing (a mod adding a category mustn't refuse an older hero); no colours, no palette.</summary>
    [Fact]
    public void WhatALookLeavesOutIsNotDrawn()
    {
        using var looks = new TestLooks();
        var plan = HeroFigure.Resolve(looks.Data, TestLooks.WayfarerLook with { Parts = new Dictionary<string, string> { ["hair"] = "hair-bald" }, Colours = new Dictionary<string, string>() });
        Assert.Equal([new FigurePart("hair", "hair-bald", null)], plan.Parts);
        Assert.Empty(plan.GarmentModels);
        Assert.Empty(plan.Colours);
        // No colours: the race isn't read, so even a race the data hasn't builds.
        Assert.Empty(HeroFigure.Resolve(looks.Data, TestLooks.WayfarerLook with { Race = "nobody", Colours = new Dictionary<string, string>() }).Colours);
    }

    public static TheoryData<string, string[], string> Refusals => new()
    {
        { "no body", [], "body=nobody" },
        { "the body's model", ["models/humanoid/bodies/human-male-average"], "" },
        { "no part", [], "hair=hair-mohawk" },
        { "a part with no model for the body", [], "body=human-male-strong" },
        { "the part's model", ["models/humanoid/parts/hair/braid/human-male"], "" },
        { "no race for the colours", [], "race=nobody" },
        { "no such swatch", [], "skin=Pale" },
        { "a swatch of another slot's", [], "skin=Ash" },
    };

    static readonly Dictionary<string, string> Expected = new()
    {
        ["no body"] = "no look body \"nobody\" (data/looks/bodies/nobody)",
        ["the body's model"] = "the body \"human-male-average\": no model \"models/humanoid/bodies/human-male-average\"",
        ["no part"] = "no look part \"hair-mohawk\" (data/looks/parts/hair-mohawk)",
        ["a part with no model for the body"] = "\"hair-braid\" has no model for the body \"human-male-strong\"",
        ["the part's model"] = "\"hair-braid\": no model \"models/humanoid/parts/hair/braid/human-male\"",
        ["no race for the colours"] = "no race \"nobody\" (data/races/nobody) for the colours",
        ["no such swatch"] = "the human has no skin colour \"Pale\"",
        ["a swatch of another slot's"] = "the human has no skin colour \"Ash\"",
    };

    /// <summary>A look naming what the data hasn't is refused (GM2-P18), naming it: each kind of missing thing alone.</summary>
    [Theory]
    [MemberData(nameof(Refusals))]
    public void ALookNamingWhatTheDataHasntIsRefused(string what, string[] leaveOut, string change)
    {
        using var looks = new TestLooks(leaveOut);
        var look = TestLooks.WayfarerLook;
        if (change.Split('=') is [var key, var value])
            look = key switch
            {
                "body" => look with { Body = value },
                "race" => look with { Race = value },
                "hair" => look with { Parts = new Dictionary<string, string>(look.Parts) { ["hair"] = value } },
                "skin" => look with { Colours = new Dictionary<string, string>(look.Colours) { ["skin"] = value } },
                _ => throw new ArgumentException(key),
            };
        var refused = Assert.Throws<LookRefusedException>(() => HeroFigure.Resolve(looks.Data, look));
        // The strong body: its face is there, the braid isn't fitted to it.
        Assert.Equal([Expected[what]], refused.Problems);
        Assert.Equal("the look can't be built: " + Expected[what], refused.Message);
    }

    /// <summary>Everything missing at once, each named: nothing is drawn with a piece left out.</summary>
    [Fact]
    public void EverythingMissingIsNamedAtOnce()
    {
        using var looks = new TestLooks("models/humanoid/bodies/human-male-average", "models/humanoid/parts/face/human-male-1/human-male");
        var look = TestLooks.WayfarerLook with
        {
            Parts = new Dictionary<string, string>(TestLooks.WayfarerLook.Parts) { ["beard"] = "beard-gone" },
            Colours = new Dictionary<string, string> { ["skin"] = "Fair", ["hair"] = "Moss" },
        };
        var refused = Assert.Throws<LookRefusedException>(() => HeroFigure.Resolve(looks.Data, look, SampleGear with { Garments = ["models/nothing"] }));
        Assert.Equal(
        [
            "the body \"human-male-average\": no model \"models/humanoid/bodies/human-male-average\"",
            "\"face-human-male-1\": no model \"models/humanoid/parts/face/human-male-1/human-male\"",
            "no look part \"beard-gone\" (data/looks/parts/beard-gone)",
            "the human has no hair colour \"Moss\"",
            "a garment: no model \"models/nothing\"",
            "an item on socket.hand.R: no model \"models/humanoid/parts/face/human-male-1/human-male\"",
        ], refused.Problems);
    }

    /// <summary>Gear: its garments after the parts', its items on their sockets, its colours beside the look's; a gear colour in a look's slot, or not a colour, is the caller's mistake.</summary>
    [Fact]
    public void GearAddsGarmentsItemsAndColoursOfItsOwnSlots()
    {
        using var looks = new TestLooks();
        var plan = HeroFigure.Resolve(looks.Data, TestLooks.WayfarerLook, SampleGear);
        Assert.Equal(["models/humanoid/parts/face/human-male-1/human-male", "models/humanoid/parts/hair/braid/human-male", "models/humanoid/bodies/human-male-strong"], plan.GarmentModels);
        Assert.Equal(SampleGear.Items, plan.Items);
        Assert.Equal(new Dictionary<string, string> { ["hair"] = "#8a8a8a", ["metal.base"] = "#80848c", ["skin"] = "#f0c8a0", ["wood"] = "#5a2a1e" }, plan.Colours);
        var slot = Assert.Throws<ArgumentException>(() => HeroFigure.Resolve(looks.Data, TestLooks.WayfarerLook, SampleGear with { Colours = new Dictionary<string, string> { ["cloth"] = "#000000" } }));
        Assert.StartsWith("the gear's colour for \"cloth\": that slot is a look's", slot.Message);
        var colour = Assert.Throws<ArgumentException>(() => HeroFigure.Resolve(looks.Data, TestLooks.WayfarerLook, SampleGear with { Colours = new Dictionary<string, string> { ["wood"] = "brown" } }));
        Assert.StartsWith("the gear's colour for \"wood\": \"brown\" isn't a colour (#rrggbb)", colour.Message);
    }

    /// <summary>The game's packs mounted as the game mounts them.</summary>
    static VirtualFileSystem GameData(ScratchFolder folder)
    {
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        return new VirtualFileSystem(Core.Application.DataMounts(new Core.ApplicationOptions(), folder["game"], _ => null));
    }

    /// <summary>
    /// A preset of the game's data built: an actor of the body's model, the face and the hair as garments on the body's
    /// bones (each joint the body's bone of its name, at the body's rest: following it posed), no items, the palette the
    /// look's colours.
    /// </summary>
    [Fact]
    public void AGamePresetIsTheBodyWithItsPartsOnItsBones()
    {
        using var folder = new ScratchFolder();
        using var data = GameData(folder);
        var look = LookData.Load(data).Presets["human-female-2"].Look;
        using var figure = HeroFigure.Build(gpu.Device, data, look);
        Assert.Equal("models/humanoid/bodies/human-female-average", figure.Plan.Body);
        Assert.Equal(figure.Models[0], figure.Actor.Model);
        Assert.Equal(ModelData.Load(data, "models/humanoid/bodies/human-female-average").Nodes.Select(n => n.Name), figure.Actor.Model.Data.Nodes.Select(n => n.Name));
        Assert.Equal(["models/humanoid/parts/face/human-female-1/human-female-average", "models/humanoid/parts/hair/long-tied/human-female-average"], figure.Plan.GarmentModels);
        Assert.Equal(figure.Models.Skip(1), figure.Actor.Garments.Select(g => g.Model));
        Assert.Empty(figure.Actor.Items);
        // The palette the preset's colours, whatever they are (none until the human has swatches, GM2 step 9a).
        Assert.Equal(figure.Plan.Colours.Keys.Order(StringComparer.Ordinal), figure.Actor.Palette!.Slots.Keys.Order(StringComparer.Ordinal));
        Assert.All(figure.Plan.Colours, c => Assert.Equal(Colors.LinearFromHex(c.Value), figure.Actor.Palette.Slots[c.Key].Lit));
        Assert.Same(figure.Skeleton.Model, figure.Actor.Model.Data);
        // Posed (each bone turned its own way), each garment's joint follows the body's bone of its name: its world the body's posed world.
        var head = figure.Skeleton.Find("head");
        var bodyRest = figure.Actor.Model.Data.WorldMatrices();
        var posed = Posed(bodyRest);
        foreach (var garment in figure.Actor.Garments)
        {
            var world = garment.Garment.WorldMatrices(bodyRest, posed);
            var garmentHead = Index(garment.Model.Data, "head");
            Assert.True(garmentHead >= 0);
            Assert.True(Close(posed[head], world[garmentHead]), $"{garment.Model}: its head doesn't follow the body's");
            Assert.False(Close(bodyRest[head], world[garmentHead]));
        }
    }

    /// <summary>
    /// A part whose model lists its nodes in another order than the body's (a mod's export, say: here the swept-back hair
    /// with a node of its own first) still follows the body's bones by their names, not by their places in its file.
    /// </summary>
    [Fact]
    public void APartsJointsAreTheBodysBonesByNameWhateverTheirOrder()
    {
        using var folder = new ScratchFolder();
        using var gameFolder = new ScratchFolder();
        using (var game = GameData(gameFolder))
        {
            var hair = ModelData.Load(game, "models/humanoid/parts/hair/swept-back/human-male-average");
            ModelData shifted = new()
            {
                Nodes = [new ModelNode("extra", -1, [], Matrix4x4.Identity, Vector3.Zero, Quaternion.Identity, Vector3.One, -1, -1),
                    .. hair.Nodes.Select(n => n with { Parent = n.Parent < 0 ? -1 : n.Parent + 1, Children = [.. n.Children.Select(c => c + 1)] })],
                Meshes = hair.Meshes, Materials = hair.Materials,
                Skins = [.. hair.Skins.Select(s => s with { Joints = [.. s.Joints.Select(j => j + 1)], Skeleton = s.Skeleton < 0 ? -1 : s.Skeleton + 1 })],
                Roots = [0, .. hair.Roots.Select(r => r + 1)],
            };
            Directory.CreateDirectory(folder["dev/models/test"]);
            File.WriteAllText(folder["dev/models/test/hair-shifted.json"], RecordSerializer.ToJson(shifted));
            var part = LookData.Load(game).Parts["hair-swept-back"];
            Directory.CreateDirectory(folder["dev/data/looks/parts"]);
            File.WriteAllText(folder["dev/data/looks/parts/hair-swept-back.json"],
                RecordSerializer.ToJson(part with { Models = [new() { Model = "models/test/hair-shifted", Bodies = ["human-male-average"] }] }));
        }
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        using var data = new VirtualFileSystem(Core.Application.DataMounts(new Core.ApplicationOptions { DevelopmentFolder = folder["dev"] }, folder["game"], _ => null));
        using var figure = HeroFigure.Build(gpu.Device, data, LookData.Load(data).Presets["human-male-1"].Look);
        var hairGarment = figure.Actor.Garments.Single(g => g.Model.Data.Nodes[0].Name == "extra");
        Assert.NotEqual(Index(figure.Actor.Model.Data, "head"), Index(hairGarment.Model.Data, "head"));
        var bodyRest = figure.Actor.Model.Data.WorldMatrices();
        var posed = Posed(bodyRest);
        var world = hairGarment.Garment.WorldMatrices(bodyRest, posed);
        Assert.True(Close(posed[figure.Skeleton.Find("head")], world[Index(hairGarment.Model.Data, "head")]), "the shifted hair's head doesn't follow the body's");
    }

    /// <summary>A pose turning each of the body's nodes its own way about its own place (no two alike), so a joint matched to the wrong bone shows.</summary>
    static Matrix4x4[] Posed(Matrix4x4[] rest) =>
        [.. rest.Select((m, i) => m * Matrix4x4.CreateRotationY(0.05f * (i + 1), m.Translation))];

    static int Index(ModelData model, string name)
    {
        for (var i = 0; i < model.Nodes.Count; i++)
            if (model.Nodes[i].Name == name)
                return i;
        return -1;
    }

    static bool Close(Matrix4x4 a, Matrix4x4 b)
    {
        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                if (MathF.Abs(a[r, c] - b[r, c]) > 1e-5f)
                    return false;
        return true;
    }

    /// <summary>Gear on a game figure: the chain shirt a garment on the body's bones, the sword and the shield on their sockets, the palette the look's and the gear's colours (linear).</summary>
    [Fact]
    public void GearSitsOnItsSocketsAndColoursItsOwnSlots()
    {
        using var folder = new ScratchFolder();
        using var data = AssemblyTests.Data(folder);
        var sample = AssemblyTests.Samples.Single(s => s.Name == "assembly-human-male-geared");
        using var figure = HeroFigure.Build(gpu.Device, data, sample.Look, sample.Gear);
        Assert.Equal(3, figure.Actor.Garments.Count);
        Assert.Equal("models/humanoid/gear/chain-shirt/human-male-average", figure.Plan.GarmentModels.Last());
        Assert.Equal([("socket.hand.R", figure.Skeleton.Find("socket.hand.R")), ("socket.shield.L", figure.Skeleton.Find("socket.shield.L"))],
            figure.Actor.Items.Select(i => (i.Socket.Name, i.Socket.Node)));
        Assert.All(figure.Actor.Items, i => Assert.True(i.Socket.Node >= 0));
        Assert.Equal(figure.Models.Skip(4), figure.Actor.Items.Select(i => i.Model));
        var palette = figure.Actor.Palette!;
        Assert.Equal(Colors.LinearFromHex("#6a4430"), palette.Slots["skin"].Lit);
        Assert.Equal(Colors.LinearFromHex("#80848c"), palette.Slots["metal.base"].Lit);
        Assert.Equal(figure.Plan.Colours.Keys.Order(StringComparer.Ordinal), palette.Slots.Keys.Order(StringComparer.Ordinal));
        Assert.All(palette.Slots.Values, t => Assert.Null(t.Shadow));
        // An item on a socket the body hasn't is the caller's mistake, and nothing is left made.
        Assert.Throws<ArgumentException>(() => HeroFigure.Build(gpu.Device, data, sample.Look, sample.Gear with { Items = [new FigureItem("models/humanoid/gear/sword", "socket.tail")] }));
        // A refused look builds nothing either.
        Assert.Throws<LookRefusedException>(() => HeroFigure.Build(gpu.Device, data, sample.Look with { Body = "nobody" }));
    }
}
