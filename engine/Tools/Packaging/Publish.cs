// Makes the packages (M12; Docs/Packaging.md, "How it works", step 3): a .NET file-based program, shared by
// Tools/Scripts/publish.sh and publish.ps1, which compile the shaders and fetch the Visual C++ runtime first.
//   dotnet run Tools/Packaging/Publish.cs -- --engine <engine folder> --version <v|dev> --time <unix seconds>
//       [--vc-runtime <folder>] <package>...
// A package is stat-fick-linux, stat-fick-windows, statventure-editor-linux or statventure-editor-windows. Each is
// published self-contained and single-file (the program's project, M12-3, Q5, Q6) with embedded symbols whose paths
// start at the repo's root (Q7), its executable renamed (M12-2), laid out with packs/ (the game's), NOTICE.txt and
// licenses/ (Q10) and on Windows the Visual C++ runtime (Q9), no .pdb files, and archived into release/ at the repo's
// root as <program>-<version>-<platform>.tar.gz or .zip holding the folder <program>-<version>/ (Q4).
#:property PublishAot=false
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

string? engine = null, version = null, vcRuntime = null;
long? time = null;
var packages = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        // Its real path (GM1-P18): MSBuild names the projects by their real path, and a restore that met them under two
        // paths (the repo reached through a symbolic link, /home to /var/home) gave the program no project references.
        case "--engine": engine = RealPath(args[++i]); break;
        case "--version": version = args[++i]; break;
        case "--time": time = long.Parse(args[++i]); break;
        case "--vc-runtime": vcRuntime = args[++i]; break;
        default: packages.Add(args[i]); break;
    }
}
if (engine is null || version is null || time is null || packages.Count == 0)
    return Fail("usage: Publish.cs --engine <folder> --version <v|dev> --time <unix seconds> [--vc-runtime <folder>] <package>...");
// Q1: four numbers, year, month, day and the release of that day (2026.11.8.1); a build that isn't a release is dev.
if (version != "dev" && !Regex.IsMatch(version, @"^[1-9][0-9]{3}\.(1[0-2]|[1-9])\.(3[01]|[12][0-9]|[1-9])\.[1-9][0-9]*$"))
    return Fail($"the version '{version}' isn't of the form 2026.11.8.1 (Q1)");

var repo = Path.GetDirectoryName(engine)!;
var packaging = Path.Combine(engine, "Tools", "Packaging");
var release = Path.Combine(repo, "release");
// The work folder is inside the repo (release/ is git-ignored), so the files the build generates are under the repo's
// root too and their paths in the embedded symbols start at it like the sources' (Q7).
var work = Path.Combine(release, ".publish");
var texts = LicenceTexts.Read(packaging);
var runtime = VcRuntime.Read(packaging);
var problems = new List<string>();
DeleteFolder(work);
Directory.CreateDirectory(work);
try
{
    foreach (var name in packages)
    {
        var package = Package.Parse(name);
        if (package is null)
            return Fail($"'{name}' isn't a package (stat-fick-linux, stat-fick-windows, statventure-editor-linux, statventure-editor-windows)");
        var folderName = $"{package.Program.Name}-{version}";
        var folder = Path.Combine(work, name, folderName);
        var artifacts = Path.Combine(work, "artifacts");
        var command = new List<string>
        {
            "publish", Path.Combine(engine, package.Program.Project), "-c", "Release", "-r", package.Runtime,
            // For every assembly of the program, the engine's too (its project isn't the program's): symbols embedded
            // (Q7) with paths from the repo's root (DeterministicSourcePaths), and no Source Link entry in them: it would
            // name the repo's address (raw.githubusercontent.com/<owner>/<repo>/<commit>), which stays out of the
            // programs (the user, 2026-11-08); the commit stays in the version all the same.
            "-p:DebugType=embedded", "-p:ContinuousIntegrationBuild=true", "-p:EnableSourceLink=false",
            "--artifacts-path", artifacts, "-o", folder, "-nologo", "-v", "quiet",
        };
        if (version != "dev")
            command.Add($"-p:Version={version}");
        Console.WriteLine($"{name}: dotnet publish");
        if (Run("dotnet", command) is { } output)
            return Fail($"{name}: the publish failed:\n{output}");

        // M12-2: the program's executable renamed; the program's own files keep their names (the apphost starts the
        // program it carries whatever it is called).
        File.Move(Path.Combine(folder, package.Program.Assembly + package.Executable), Path.Combine(folder, package.ProgramFile));
        // Q7: the managed symbols are embedded; native symbols (libHarfBuzzSharp.pdb, 23 MB) are left out.
        foreach (var pdb in Directory.EnumerateFiles(folder, "*.pdb", SearchOption.AllDirectories))
            File.Delete(pdb);
        // packs/: exactly the committed packs, as build.sh lays them out (M9 Q2, S9-A); the game's only.
        DeleteFolder(Path.Combine(folder, "packs"));
        if (package.Program.Packs)
        {
            Directory.CreateDirectory(Path.Combine(folder, "packs"));
            foreach (var pack in Directory.EnumerateFiles(Path.Combine(engine, "Assets", "Packs"), "*.pak"))
                File.Copy(pack, Path.Combine(folder, "packs", Path.GetFileName(pack)));
        }
        // Q10: NOTICE.txt beside the program and every licence text in licenses/.
        foreach (var text in texts.Shipped)
        {
            var target = Path.Combine(folder, text);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(packaging, text), target);
        }
        // Q9: the Visual C++ runtime beside the Windows program.
        if (package.Windows)
        {
            if (vcRuntime is null)
                return Fail($"{name}: no --vc-runtime folder (publish.sh and publish.ps1 fetch it)");
            foreach (var file in runtime.Files)
            {
                // The scripts' fetch checks each DLL's SHA-256 against vc-runtime.txt, and check-packages.sh again.
                var source = Path.Combine(vcRuntime, file.Name);
                if (!File.Exists(source))
                    return Fail($"{name}: the Visual C++ runtime has no {source}");
                File.Copy(source, Path.Combine(folder, file.Name));
            }
        }
        // The texts against the packages the program was built from (its project's restore).
        var assets = Path.Combine(artifacts, "obj", Path.GetFileNameWithoutExtension(package.Program.Project), "project.assets.json");
        problems.AddRange(texts.Check(assets, package.Runtime, repo, packaging).Select(p => $"{name}: {p}"));
        if (problems.Count > 0)
            return Fail(string.Join("\n", problems));

        var archive = Path.Combine(release, $"{folderName}-{package.Platform}{(package.Windows ? ".zip" : ".tar.gz")}");
        var (files, bytes) = Archive.Write(folder, folderName, archive, package.Windows, DateTimeOffset.FromUnixTimeSeconds(time.Value));
        Console.WriteLine($"{Path.GetRelativePath(repo, archive)}: {bytes / 1048576.0:F1} MB ({bytes} bytes), {files} files");
    }
}
finally
{
    DeleteFolder(work);
}
return 0;

static int Fail(string message)
{
    Console.Error.WriteLine($"publish: {message}");
    return 1;
}

static void DeleteFolder(string path)
{
    if (Directory.Exists(path))
        Directory.Delete(path, true);
}

/// <summary>The full path of an existing folder with every symbolic link in it resolved (a link to a link too).</summary>
static string RealPath(string path, int depth = 0)
{
    if (depth > 40)
        throw new IOException($"{path}: too many symbolic links");
    var full = Path.GetFullPath(path);
    var root = Path.GetPathRoot(full)!;
    var real = root;
    foreach (var part in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
    {
        var next = Path.Combine(real, part);
        if (new DirectoryInfo(next).LinkTarget is { } target)
            next = RealPath(Path.IsPathRooted(target) ? target : Path.Combine(real, target), depth + 1);
        real = next;
    }
    return real;
}

/// <summary>Runs a command; null when it succeeded, else its output.</summary>
static string? Run(string program, IEnumerable<string> arguments)
{
    var start = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var argument in arguments)
        start.ArgumentList.Add(argument);
    using var process = Process.Start(start)!;
    var error = process.StandardError.ReadToEndAsync();
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return process.ExitCode == 0 ? null : output + error.Result;
}

/// <summary>A program packaged (M12-1, M12-2): its project, its assembly's name, its executables' names.</summary>
sealed record Packaged(string Name, string Project, string Assembly, string LinuxFile, string WindowsFile, bool Packs)
{
    public static readonly Packaged[] All =
    [
        // The game's own program since GM1 step 2 (GM1-P1; the sandbox stood in for it before, M12-12, Q13).
        new("stat-fick", "Game/Statventure.Game/Statventure.Game.csproj", "Statventure.Game", "stat-fick", "Stat Fick.exe", true),
        new("statventure-editor", "Tools/Editor/Editor.csproj", "Statventure.Editor", "statventure-editor", "Statventure Editor.exe", false),
    ];
}

sealed record Package(Packaged Program, string Platform)
{
    public bool Windows => Platform == "windows";
    public string Runtime => Windows ? "win-x64" : "linux-x64";
    public string Executable => Windows ? ".exe" : "";
    public string ProgramFile => Windows ? Program.WindowsFile : Program.LinuxFile;

    public static Package? Parse(string name)
    {
        foreach (var program in Packaged.All)
            foreach (var platform in new[] { "linux", "windows" })
                if (name == $"{program.Name}-{platform}")
                    return new(program, platform);
        return null;
    }
}

/// <summary>Tools/Packaging/licenses.txt's text lines: what each shipped text is copied from.</summary>
sealed class LicenceTexts
{
    readonly List<(string Shipped, string Source)> _lines = [];

    public IEnumerable<string> Shipped => _lines.Select(l => l.Shipped).Distinct();

    public static LicenceTexts Read(string packaging)
    {
        var texts = new LicenceTexts();
        foreach (var raw in File.ReadAllLines(Path.Combine(packaging, "licenses.txt")))
        {
            var line = raw.Trim();
            if (line.StartsWith("text "))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                texts._lines.Add((parts[1], parts[2]));
            }
        }
        return texts;
    }

    /// <summary>Each text against every source named for it, at the versions the program's restore resolved; and every
    /// text in the folder named by a line.</summary>
    public IEnumerable<string> Check(string assetsPath, string runtime, string repo, string packaging)
    {
        using var assets = JsonDocument.Parse(File.ReadAllBytes(assetsPath));
        var root = assets.RootElement;
        var packagesFolder = root.GetProperty("project").GetProperty("restore").GetProperty("packagesPath").GetString()!;
        var resolved = new List<(string Id, string Version)>();
        foreach (var library in root.GetProperty("libraries").EnumerateObject())
            if (library.Value.GetProperty("type").GetString() == "package")
            {
                var slash = library.Name.IndexOf('/');
                resolved.Add((library.Name[..slash], library.Name[(slash + 1)..]));
            }
        foreach (var framework in root.GetProperty("project").GetProperty("frameworks").EnumerateObject())
            if (framework.Value.TryGetProperty("downloadDependencies", out var downloads))
                foreach (var download in downloads.EnumerateArray())
                    resolved.Add((download.GetProperty("name").GetString()!, download.GetProperty("version").GetString()!.Trim('[', ']').Split(',')[0].Trim()));

        foreach (var (shipped, source) in _lines)
        {
            var text = Path.Combine(packaging, shipped);
            if (!File.Exists(text))
            {
                yield return $"{shipped} is named in licenses.txt but isn't in Tools/Packaging";
                continue;
            }
            if (source.StartsWith("repo:"))
            {
                var file = Path.Combine(repo, source["repo:".Length..]);
                if (!File.Exists(file) || !SameText(text, file))
                    yield return $"{shipped} isn't the text of {source}";
                continue;
            }
            // A text from neither a package nor the repo (Chromium's licence for its images in the engine) is kept as
            // committed.
            if (source.StartsWith("fixed:"))
                continue;
            if (!source.StartsWith("nuget:"))
            {
                yield return $"{shipped}: '{source}' isn't a source (nuget:…, repo:… or fixed:…)";
                continue;
            }
            var slash = source.IndexOf('/');
            var package = source["nuget:".Length..slash].Replace("{rid}", runtime);
            var pattern = "^" + Regex.Escape(package).Replace(@"\*", ".*") + "$";
            var path = source[(slash + 1)..];
            var matches = resolved.Where(p => Regex.IsMatch(p.Id, pattern, RegexOptions.IgnoreCase)).ToList();
            if (matches.Count == 0)
                yield return $"{shipped}: the program has no package {package}";
            foreach (var (id, packageVersion) in matches)
            {
                var file = Path.Combine(packagesFolder, id.ToLowerInvariant(), packageVersion.ToLowerInvariant(), path);
                if (!File.Exists(file))
                    yield return $"{shipped}: {id} {packageVersion} has no {path}";
                else if (!SameText(text, file))
                    yield return $"{shipped} isn't the text of {id} {packageVersion}'s {path}";
            }
        }
        var named = new HashSet<string>(Shipped);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(packaging, "licenses")).Append(Path.Combine(packaging, "NOTICE.txt")))
        {
            var relative = Path.GetRelativePath(packaging, file).Replace('\\', '/');
            if (!named.Contains(relative))
                yield return $"{relative} is in Tools/Packaging but licenses.txt names no source for it";
        }
    }

    /// <summary>The same text, line ends aside (a package may give it with CRLF, another with LF).</summary>
    static bool SameText(string a, string b)
    {
        var x = File.ReadAllBytes(a);
        var y = File.ReadAllBytes(b);
        return x.AsSpan().SequenceEqual(y) || Encoding.UTF8.GetString(x).Replace("\r\n", "\n") == Encoding.UTF8.GetString(y).Replace("\r\n", "\n");
    }
}

/// <summary>Tools/Packaging/vc-runtime.txt: the Visual C++ runtime's DLLs (Q9).</summary>
sealed record VcRuntime(List<VcRuntime.File> Files)
{
    public sealed record File(string Name);

    public static VcRuntime Read(string packaging)
    {
        var files = new List<File>();
        foreach (var raw in System.IO.File.ReadAllLines(Path.Combine(packaging, "vc-runtime.txt")))
        {
            var parts = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts is ["file", var name, _, _])
                files.Add(new(name));
        }
        return new(files);
    }
}

/// <summary>The archives (Q4): a .tar.gz for Linux, a .zip for Windows, each holding one folder; entries sorted, owned by
/// 0, dated at the commit's time, so one commit gives the same archive.</summary>
static class Archive
{
    public static (int Files, long Bytes) Write(string folder, string folderName, string archive, bool zip, DateTimeOffset time)
    {
        var entries = new List<(string Name, string? Path)> { (folderName + "/", null) };
        foreach (var path in Directory.EnumerateFileSystemEntries(folder, "*", SearchOption.AllDirectories))
        {
            var name = folderName + "/" + System.IO.Path.GetRelativePath(folder, path).Replace('\\', '/');
            entries.Add(Directory.Exists(path) ? (name + "/", null) : (name, path));
        }
        entries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(archive)!);
        var temporary = archive + ".tmp";
        try
        {
            using (var stream = File.Create(temporary))
            {
                if (zip)
                    WriteZip(stream, entries, time);
                else
                    WriteTar(stream, entries, time);
            }
            File.Move(temporary, archive, true);
        }
        finally
        {
            File.Delete(temporary);
        }
        return (entries.Count(e => e.Path is not null), new FileInfo(archive).Length);
    }

    static void WriteTar(Stream stream, List<(string Name, string? Path)> entries, DateTimeOffset time)
    {
        using var gzip = new GZipStream(stream, CompressionLevel.SmallestSize);
        using var writer = new TarWriter(gzip, TarEntryFormat.Ustar);
        foreach (var (name, path) in entries)
        {
            var entry = new UstarTarEntry(path is null ? TarEntryType.Directory : TarEntryType.RegularFile, name)
            {
                // The executable bit for the program and the native libraries (every ELF file); 0644 for the rest.
                Mode = path is null || IsElf(path) ? (UnixFileMode)0b111_101_101 : (UnixFileMode)0b110_100_100,
                Uid = 0,
                Gid = 0,
                UserName = "",
                GroupName = "",
                ModificationTime = time,
            };
            using var data = path is null ? null : File.OpenRead(path);
            if (data is not null)
                entry.DataStream = data;
            writer.WriteEntry(entry);
        }
    }

    static void WriteZip(Stream stream, List<(string Name, string? Path)> entries, DateTimeOffset time)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (name, path) in entries)
        {
            var entry = zip.CreateEntry(name, path is null ? CompressionLevel.NoCompression : CompressionLevel.SmallestSize);
            entry.LastWriteTime = time;
            if (path is null)
                continue;
            using var target = entry.Open();
            using var source = File.OpenRead(path);
            source.CopyTo(target);
        }
    }

    static bool IsElf(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> magic = stackalloc byte[4];
        return stream.Read(magic) == 4 && magic[0] == 0x7f && magic[1] == 'E' && magic[2] == 'L' && magic[3] == 'F';
    }
}
