using Statventure.Packs;

namespace Statventure.Tests.Packs;

/// <summary>The path rule (Docs/Assets.md, Q4, Q28, Q31): its characters, its names, and every place that checks it.</summary>
public class PackPathTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("audio/themes/alder-vale")]
    [InlineData("ui/fonts/lato-regular.ttf")]
    [InlineData("a_b-c.d/0123456789")]
    [InlineData(".gitkeep")]
    [InlineData("a/.hidden/b")]
    [InlineData("a..b")]
    [InlineData("con-x")]
    [InlineData("auxiliary")]
    [InlineData("xcon")]
    [InlineData("com")]
    [InlineData("com10")]
    [InlineData("comx")]
    [InlineData("lpt")]
    [InlineData("lpt12")]
    [InlineData("nul_")]
    [InlineData(".aux")]
    [InlineData("a.con")]
    [InlineData("classes/black-knight.json")]
    public void TheseFollowTheRule(string path)
    {
        Assert.Null(PackPath.Problem(path));
        Assert.True(PackPath.IsValid(path));
        PackPath.Check(path);
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("Audio", "'A' isn't allowed")]
    [InlineData("classes/Black Knight.json", "'B' isn't allowed")]
    [InlineData("a b", "' ' isn't allowed")]
    [InlineData("a\\b", "'\\' isn't allowed")]
    [InlineData("a:b", "':' isn't allowed")]
    [InlineData("x.json~", "'~' isn't allowed")]
    [InlineData("café", "'é' isn't allowed")]
    [InlineData("/a", "a name is empty")]
    [InlineData("a/", "a name is empty")]
    [InlineData("a//b", "a name is empty")]
    [InlineData("/", "a name is empty")]
    [InlineData(".", "ends in a dot")]
    [InlineData("..", "ends in a dot")]
    [InlineData("a/./b", "ends in a dot")]
    [InlineData("a/../b", "ends in a dot")]
    [InlineData("a.", "ends in a dot")]
    [InlineData("a/b./c", "ends in a dot")]
    [InlineData("con", "Windows keeps")]
    [InlineData("prn", "Windows keeps")]
    [InlineData("aux", "Windows keeps")]
    [InlineData("nul", "Windows keeps")]
    [InlineData("aux.json", "Windows keeps")]
    [InlineData("con.tar.gz", "Windows keeps")]
    [InlineData("audio/nul.txt/b", "Windows keeps")]
    public void TheseBreakIt(string path, string why)
    {
        var problem = PackPath.Problem(path);
        Assert.NotNull(problem);
        Assert.Contains(why, problem);
        Assert.False(PackPath.IsValid(path));
        var refused = Assert.Throws<ArgumentException>(() => PackPath.Check(path));
        Assert.Contains(why, refused.Message);
    }

    [Fact]
    public void OnlyTheRulesCharactersAreAllowed()
    {
        for (var c = (char)0; c < 0x250; c++)
        {
            var allowed = c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.' or '/';
            Assert.True(allowed == PackPath.IsValid($"x{c}y"), $"U+{(int)c:X4}");
        }
        Assert.True(PackPath.IsValid("x.y"));
        Assert.True(PackPath.IsValid("x/y"));
    }

    [Fact]
    public void EveryNumberedDeviceNameIsRefused()
    {
        for (var digit = '0'; digit <= '9'; digit++)
            foreach (var stem in new[] { "com", "lpt" })
            {
                Assert.Contains("Windows keeps", PackPath.Problem($"{stem}{digit}"));
                Assert.Contains("Windows keeps", PackPath.Problem($"a/{stem}{digit}.txt"));
                Assert.Null(PackPath.Problem($"{stem}{digit}x"));
            }
    }

    [Fact]
    public void ANameIsAtMost255Characters()
    {
        var longest = new string('a', 255);
        Assert.True(PackPath.IsValid(longest));
        Assert.True(PackPath.IsValid($"b/{longest}/{longest}"));
        Assert.Contains("longer than 255", PackPath.Problem(longest + "a"));
        Assert.Contains("longer than 255", PackPath.Problem($"b/{longest}a/c"));
    }

    [Fact]
    public void EveryCallChecksIt()
    {
        using var folder = new TempFolder();
        var pack = PackTestData.MakePack(folder["a.pak"], PackTestData.Entry("a", "x"));
        using var file = PackFile.Open(pack);
        using var data = new VirtualFileSystem([Mount.Pack(pack)]);
        foreach (var bad in new[] { "A", "a/", "aux", "a." })
        {
            Assert.Throws<ArgumentException>(() => file.Find(bad));
            Assert.Throws<ArgumentException>(() => file.Read(bad));
            Assert.Throws<ArgumentException>(() => data.Exists(bad));
            Assert.Throws<ArgumentException>(() => data.ReadAllBytes(bad));
            Assert.Throws<ArgumentException>(() => data.Open(bad));
            Assert.Throws<ArgumentException>(() => data.Origin(bad));
            Assert.Throws<ArgumentException>(() => data.List(bad));
            Assert.Throws<ArgumentException>(() => Mount.Folder(folder.Root, bad));
            Assert.Throws<ArgumentException>(() => new PackWriter().Add(bad, [1]));
        }
    }
}
