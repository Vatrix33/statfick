using System.Text;
using System.Text.Json;
using Statventure.Storage;

namespace Statventure.Tests.Storage;

/// <summary>
/// The Documents folder (Docs/Assets.md, "Character files") against what Electron 44.5.0's
/// app.getPath("documents") gave on Linux in each case of Data/Storage/documents-reference.json
/// (Tools/Scripts/make-documents-reference.sh): the user-dirs file, HOME and XDG_CONFIG_HOME, and the
/// folders Chromium made (mode 0700) or its failure when it couldn't.
/// </summary>
public class DocumentsFolderTests
{
    public sealed record ReferenceCase(string Name, Dictionary<string, string?> Env, Dictionary<string, (string? Text, byte[]? Bytes)> Files, string? Cwd,
        string? Documents, string? Error, (string Folder, string Mode)[] Created, string Dotnet)
    {
        public override string ToString() => Name;
    }

    static readonly Lazy<ReferenceCase[]> Reference = new(() =>
    {
        using var document = JsonDocument.Parse(StorageTestData.Read("documents-reference.json"));
        var root = document.RootElement;
        Assert.Equal("44.5.0", root.GetProperty("electron"u8).GetString());
        return [.. root.GetProperty("cases"u8).EnumerateArray().Select(c => new ReferenceCase(
            c.GetProperty("name"u8).GetString()!,
            c.GetProperty("env"u8).EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()),
            c.GetProperty("files"u8).EnumerateObject().ToDictionary(p => p.Name, p => p.Value.TryGetProperty("text"u8, out var text)
                ? (text.GetString(), (byte[]?)null)
                : (null, Convert.FromBase64String(p.Value.GetProperty("base64"u8).GetString()!))),
            c.TryGetProperty("cwd"u8, out var cwd) ? cwd.GetString() : null,
            c.TryGetProperty("documents"u8, out var documents) ? documents.GetString() : null,
            c.TryGetProperty("error"u8, out var error) ? error.GetString() : null,
            [.. c.GetProperty("created"u8).EnumerateArray().Select(f => (f.GetProperty("folder"u8).GetString()!, f.GetProperty("mode"u8).GetString()!))],
            c.GetProperty("dotnet"u8).GetString()!))];
    });

    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var c in Reference.Value)
            data.Add(c.Name);
        return data;
    }

    static string Sub(string text, string home) => text.Replace("<home>", home);

    static HashSet<string> FoldersBelow(string folder) =>
        [.. Directory.EnumerateDirectories(folder, "*", SearchOption.AllDirectories)];

    [Fact]
    public void TheReferenceCoversTheCasesThatMatter()
    {
        var names = Reference.Value.Select(c => c.Name).ToHashSet();
        foreach (var name in new[] { "no-user-dirs-file", "standard", "localized", "home-unset", "config-home", "last-wins", "escapes", "long-line-piece" })
            Assert.Contains(name, names);
        // Where .NET's MyDocuments isn't Electron's folder: why the engine has its own lookup.
        var differ = Reference.Value.Where(c => c.Documents != c.Dotnet).Select(c => c.Name).ToList();
        Assert.Contains("no-user-dirs-file", differ);
        Assert.True(differ.Count >= 20, string.Join(", ", differ));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheFolderIsElectronsOnLinux(string name)
    {
        var c = Reference.Value.Single(r => r.Name == name);
        using var temp = new StorageFolder();
        var home = temp["home"];
        Directory.CreateDirectory(home);
        foreach (var (relative, content) in c.Files)
        {
            var file = Path.Combine(home, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllBytes(file, content.Text is { } text ? Encoding.UTF8.GetBytes(Sub(text, home)) : content.Bytes!);
        }
        var environment = new Dictionary<string, string?> { ["HOME"] = home };
        foreach (var (variable, value) in c.Env)
            environment[variable] = value == null ? null : Sub(value, home);
        var cwd = c.Cwd == null ? temp.Root : Sub(c.Cwd, home);
        byte[]? ReadFile(string path)
        {
            try
            {
                return File.ReadAllBytes(Path.IsPathRooted(path) ? path : Path.Combine(cwd, path));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        var found = DocumentsFolder.Locate(StorageSystem.Linux, v => environment.GetValueOrDefault(v), ReadFile, windowsDocuments: @"C:\Users\Player\Documents");
        if (c.Error != null)
        {
            // Chromium found a folder it couldn't make: so can't the engine.
            var target = found.StartsWith(home) || found.Length == 0 ? found : throw new InvalidOperationException($"{name}: {found}");
            Assert.Throws<IOException>(() => DocumentsFolder.Ensure(target));
            return;
        }
        Assert.Equal(Sub(c.Documents!, home), found);
        if (!found.StartsWith(home))
            return;
        var before = FoldersBelow(home);
        Assert.Equal(found, DocumentsFolder.Ensure(found));
        Assert.True(Path.Exists(found));
        var made = FoldersBelow(home).Except(before).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(c.Created.Select(f => Sub(f.Folder, home)).Order(StringComparer.Ordinal), made);
        if (!OperatingSystem.IsWindows())
            foreach (var (folder, mode) in c.Created)
                Assert.Equal(mode, Convert.ToString((int)File.GetUnixFileMode(Sub(folder, home)) & 0x1FF, 8));
    }

    [Fact]
    public void OnlyTheUserDirsFileIsRead()
    {
        var read = new List<string>();
        Func<string, string?> environment = v => v switch { "HOME" => "/home/p", "XDG_CONFIG_HOME" => "/cfg", _ => null };
        DocumentsFolder.Locate(StorageSystem.Linux, environment, f => { read.Add(f); return null; }, null);
        Assert.Equal(["/cfg/user-dirs.dirs"], read);
        read.Clear();
        DocumentsFolder.Locate(StorageSystem.Linux, v => v == "HOME" ? "/home/p" : null, f => { read.Add(f); return null; }, null);
        Assert.Equal(["/home/p/.config/user-dirs.dirs"], read);
        // Without HOME nothing is read: "/tmp".
        read.Clear();
        Assert.Equal("/tmp", DocumentsFolder.Locate(StorageSystem.Linux, _ => null, f => { read.Add(f); return null; }, null));
        Assert.Empty(read);
    }

    [Fact]
    public void OnWindowsItIsTheDocumentsKnownFolder()
    {
        Assert.Equal(@"C:\Users\Player\Documents", DocumentsFolder.Locate(StorageSystem.Windows, _ => "/home/p", _ => "XDG_DOCUMENTS_DIR=\"/x\""u8.ToArray(), @"C:\Users\Player\Documents"));
        Assert.Throws<InvalidOperationException>(() => DocumentsFolder.Locate(StorageSystem.Windows, _ => null, _ => null, null));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("/", "/")]
    [InlineData("//", "//")]
    [InlineData("///", "/")]
    [InlineData("////", "/")]
    [InlineData("/a/", "/a")]
    [InlineData("/a///", "/a")]
    [InlineData("//a//", "//a")]
    [InlineData("a/", "a")]
    [InlineData("a//b/", "a//b")]
    [InlineData("a", "a")]
    public void TrailingSeparatorsAreStrippedAsChromiumsFilePath(string path, string expected) =>
        Assert.Equal(expected, DocumentsFolder.StripTrailingSeparators(path));

    [Fact]
    public void EnsureLeavesWhatIsThereAndFailsWhereNothingCanBeMade()
    {
        using var temp = new StorageFolder();
        Assert.Equal(temp.Root, DocumentsFolder.Ensure(temp.Root));
        var file = temp.Write("taken", "x"u8.ToArray());
        Assert.Equal(file, DocumentsFolder.Ensure(file));
        Assert.Throws<IOException>(() => DocumentsFolder.Ensure(Path.Combine(file, "below")));
        Assert.Equal("the Documents folder can't be made: its path is empty", Assert.Throws<IOException>(() => DocumentsFolder.Ensure("")).Message);
        Assert.Throws<IOException>(() => DocumentsFolder.Ensure(Path.Combine(temp.Root, new string('n', 256))));
    }
}
