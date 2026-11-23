using System.Security.Cryptography;
using System.Text.Json;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The clips' keying kit (GM2-P23; Tools/Models/humanoid/clips/kit/; Docs/Game/Hero.md, "How it works", "The keying kit"),
/// held where there is no Blender: its record of every clip's source and baked data (keyed.json, which make-humanoid.sh
/// keys writes and keys --check proves by baking again), and its measures (measures.json, from make-humanoid.sh measure,
/// proven by measure --check), are the committed clips' own, by their sha256; and every committed clip is within the
/// family's bounds (bounds.json): the worst the 34 approved clips measure, pinned here.
/// </summary>
public class HumanoidKeyingKitTests
{
    static string Kit => Path.Combine(HumanoidClipList.Folder, "kit");

    /// <summary>The family's bounds as measured on 2026-11-22 (Hero.md, "The keying kit"): a solved arm's wrist bend and its
    /// forearm's turn (whole degrees), the largest change of the turn, the bend and the elbow's swing between neighbouring
    /// keys, a grip out of reach (metres), an arm in the trunk, and how far under the ground the lowest point goes (mm).</summary>
    public const int Bend = 74, Turn = 128, JumpTurn = 33, JumpBend = 23, JumpSwing = 47;
    public const double Short = 0.101, Trunk = 0.04, Under = 2.21;

    static JsonElement Clips(string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Kit, file)));
        return document.RootElement.GetProperty("clips").Clone();
    }

    static string Sha(string clip) =>
        Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(HumanoidClipList.Folder, clip + ".json"))));

    static string[] Names(JsonElement clips) => [.. clips.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)];

    [Fact]
    public void TheKitsRecordsNameTheListsClips()
    {
        var listed = HumanoidClipList.All.Select(e => e.Name).Order(StringComparer.Ordinal).ToArray();
        var files = Directory.GetFiles(HumanoidClipList.Folder, "*.json").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(listed, files);
        Assert.Equal(listed, Names(Clips("keyed.json")));
        Assert.Equal(listed, Names(Clips("measures.json")));
    }

    [Fact]
    public void EveryCommittedClipIsTheOneTheKitBakedAndMeasured()
    {
        var keyed = Clips("keyed.json");
        var measured = Clips("measures.json");
        foreach (var entry in HumanoidClipList.All)
        {
            var sha = Sha(entry.Name);
            Assert.True(sha == keyed.GetProperty(entry.Name).GetProperty("clip").GetString(),
                $"{entry.Name}: clips/{entry.Name}.json isn't the one keyed.json records as baked from its source (make-humanoid.sh keys)");
            Assert.Matches("^[0-9a-f]{64}$", keyed.GetProperty(entry.Name).GetProperty("source").GetString());
            Assert.True(sha == measured.GetProperty(entry.Name).GetProperty("clip").GetString(),
                $"{entry.Name}: clips/{entry.Name}.json isn't the one measures.json measured (make-humanoid.sh measure --record)");
        }
    }

    [Fact]
    public void TheBoundsAreTheFamilysAsMeasured()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Kit, "bounds.json")));
        var bounds = document.RootElement;
        Assert.Equal(Bend, bounds.GetProperty("bend").GetInt32());
        Assert.Equal(Turn, bounds.GetProperty("turn").GetInt32());
        Assert.Equal(JumpTurn, bounds.GetProperty("jump").GetProperty("turn").GetInt32());
        Assert.Equal(JumpBend, bounds.GetProperty("jump").GetProperty("bend").GetInt32());
        Assert.Equal(JumpSwing, bounds.GetProperty("jump").GetProperty("swing").GetInt32());
        Assert.Equal(Short, bounds.GetProperty("short").GetDouble());
        Assert.Equal(Trunk, bounds.GetProperty("trunk").GetDouble());
        Assert.Equal(Under, bounds.GetProperty("under").GetDouble());
    }

    [Fact]
    public void EveryClipIsWithinTheFamilysBounds()
    {
        var problems = new List<string>();
        foreach (var clip in Clips("measures.json").EnumerateObject())
        {
            var m = clip.Value;
            foreach (var arm in m.GetProperty("arms").EnumerateObject())
            {
                var bend = arm.Value.GetProperty("bend").GetInt32();
                var turn = Math.Abs(arm.Value.GetProperty("turn").GetInt32());
                if (bend > Bend)
                    problems.Add($"{clip.Name} {arm.Name}: the wrist bent {bend} (the bound {Bend})");
                if (turn > Turn)
                    problems.Add($"{clip.Name} {arm.Name}: the forearm turned {turn} (the bound {Turn})");
            }
            var jump = m.GetProperty("largestJump");
            foreach (var (name, bound) in new[] { ("turn", JumpTurn), ("bend", JumpBend), ("swing", JumpSwing) })
                if (jump.GetProperty(name).GetInt32() > bound)
                    problems.Add($"{clip.Name}: a jump of the {name} of {jump.GetProperty(name).GetInt32()} (the bound {bound})");
            foreach (var (name, bound) in new[] { ("short", Short), ("trunk", Trunk) })
                foreach (var side in m.GetProperty(name).EnumerateObject())
                    if (side.Value.GetProperty("worst").GetDouble() > bound)
                        problems.Add($"{clip.Name} {side.Name}: {name} {side.Value.GetProperty("worst").GetDouble()} (the bound {bound})");
            var lowest = m.GetProperty("ground").GetProperty("lowest").GetDouble();
            if (lowest < -Under)
                problems.Add($"{clip.Name}: {-lowest} mm under the ground (the bound {Under})");
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }
}
