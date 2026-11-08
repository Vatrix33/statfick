using Statventure.Editor.Preview;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using static Statventure.Tests.Editor.Checks.CheckTesting;

namespace Statventure.Tests.Editor.Preview;

/// <summary>
/// The cutscene preview's "Stand-ins" heading (Docs/Editor.md, "How it works", "Fixes after step 14"): shown only for a
/// cutscene with roles to stand in for; a cutscene without roles shows no heading with nothing under it.
/// </summary>
[Collection("GPU")]
public class StandInsHeadingTests(GpuFixture gpu)
{
    const string Intro = "world/cutscenes/intro";

    static bool HeadingShown(PreviewEditor p) =>
        p.Document.QuerySelector(".preview-part .preview-heading") is { } heading && !heading.HasAttribute("hidden");

    [Fact]
    public void TheHeadingShowsOnlyForACutsceneWithRoles()
    {
        using var p = new PreviewEditor(w =>
        {
            w.AddRecord("audio/effects/boom", Patch());
            w.AddRecord(Intro, Cutscene(Sound("boom")));
        }, sound: false, gpu: gpu.Device);
        p.Select(Intro);
        Assert.IsType<CutscenePreview>(p.Preview.Shown);
        Assert.Equal("", p.Note);
        Assert.Equal("Stand-ins", p.Document.QuerySelector(".preview-part .preview-heading")!.TextContent);
        Assert.False(HeadingShown(p));
        Assert.Empty(p.Document.QuerySelector(".preview-part .preview-stand-ins")!.ElementChildren);
        // A role given: the heading and its row; taken away again: hidden again.
        var withRole = Cutscene(Sound("boom")) with { Roles = [new("spot", RoleKind.Node)] };
        p.Commit(Intro, withRole);
        Assert.True(HeadingShown(p));
        Assert.Single(p.Document.QuerySelector(".preview-part .preview-stand-ins")!.ElementChildren);
        p.Commit(Intro, Cutscene(Sound("boom")));
        Assert.False(HeadingShown(p));
    }

    /// <summary>A cutscene the preview can't take from the start (its effect isn't in the data): no heading either.</summary>
    [Fact]
    public void NoHeadingBeforeACutsceneIsTaken()
    {
        using var p = new PreviewEditor(w => w.AddRecord(Intro, Cutscene(Sound("thud"))), sound: false, gpu: gpu.Device);
        p.Select(Intro);
        Assert.IsType<CutscenePreview>(p.Preview.Shown);
        Assert.NotEqual("", p.Note);
        Assert.False(HeadingShown(p));
    }
}
