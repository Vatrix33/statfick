using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Statventure.Core;

namespace Statventure.Tests.Core;

/// <summary>
/// What a build records with the repo's Directory.Build.props (M12 step 1; Docs/Packaging.md, "The version", Q1–Q3,
/// Q8): a scratch program built with the SDK the tests run on, in a scratch git repository, as a release (given
/// <c>-p:Version</c>, as the release script will) and as a dev build, and once more without git (an unpacked source
/// archive); each read back through <see cref="ProgramInfo"/>, its runtime configuration in invariant globalization.
/// </summary>
public class ProgramBuildTests
{
    const string Project = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
          </PropertyGroup>
        </Project>
        """;

    [Fact]
    public void AReleaseRecordsItsVersionAndCommitADevBuildDevAndABuildWithoutGitNoCommit()
    {
        var scratch = Directory.CreateTempSubdirectory("statventure-version-").FullName;
        try
        {
            var repo = Directory.CreateDirectory(Path.Combine(scratch, "repo")).FullName;
            var archive = Directory.CreateDirectory(Path.Combine(scratch, "archive")).FullName;
            foreach (var folder in new[] { repo, archive })
            {
                File.WriteAllText(Path.Combine(folder, "VersionProbe.csproj"), Project);
                File.WriteAllText(Path.Combine(folder, "Program.cs"), "return 0;\n");
            }
            Git(repo, "-c", "init.defaultBranch=main", "init", "-q");
            Git(repo, "add", "VersionProbe.csproj", "Program.cs");
            Git(repo, "-c", "user.name=Statventure Tests", "-c", "user.email=tests@statventure.invalid", "-c", "commit.gpgsign=false",
                "commit", "-q", "-m", "probe");
            var commit = Git(repo, "rev-parse", "HEAD").Trim();
            Assert.Matches("^[0-9a-f]{40}$", commit);

            Assert.Equal(new ProgramInfo("VersionProbe", "2026.11.8.1", commit), Build(repo, "release", "-p:Version=2026.11.8.1"));
            Assert.Equal(new ProgramInfo("VersionProbe", "dev", commit), Build(repo, "dev"));
            Assert.Equal(new ProgramInfo("VersionProbe", "2026.11.8.2", commit), Build(repo, "second", "-p:Version=2026.11.8.2"));
            Assert.Equal(new ProgramInfo("VersionProbe", "dev", null), Build(archive, "dev"));
            Assert.Equal(new ProgramInfo("VersionProbe", "2026.11.8.1", null), Build(archive, "release", "-p:Version=2026.11.8.1"));
        }
        finally
        {
            try
            {
                Directory.Delete(scratch, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Left in the temporary folder.
            }
        }
    }

    /// <summary>
    /// Builds the scratch program in <paramref name="folder"/> into <c>out-&lt;name&gt;</c> with the repo's
    /// Directory.Build.props and <paramref name="properties"/>, checks its runtime configuration is invariant (Q8) and
    /// reads what it records.
    /// </summary>
    static ProgramInfo Build(string folder, string name, params string[] properties)
    {
        var output = Path.Combine(folder, "out-" + name);
        var props = Path.Combine(ProgramInfoTests.EngineFolder(), "Directory.Build.props");
        var (code, log) = Run(Dotnet(), folder, ["build", "--disable-build-servers", "-nologo", "-v", "quiet",
            $"-p:DirectoryBuildPropsPath={props}", "-o", output, .. properties]);
        Assert.True(code == 0, $"dotnet build {string.Join(' ', properties)} in {folder} failed ({code}):\n{log}");
        Assert.True(ProgramInfoTests.InvariantIn(Path.Combine(output, "VersionProbe.runtimeconfig.json")));
        var context = new AssemblyLoadContext("version-probe-" + name, isCollectible: true);
        try
        {
            using var file = new MemoryStream(File.ReadAllBytes(Path.Combine(output, "VersionProbe.dll")));
            return ProgramInfo.Of(context.LoadFromStream(file));
        }
        finally
        {
            context.Unload();
        }
    }

    static string Git(string folder, params string[] arguments)
    {
        var (code, output) = Run("git", folder, arguments);
        Assert.True(code == 0, $"git {string.Join(' ', arguments)} failed ({code}):\n{output}");
        return output;
    }

    /// <summary>The <c>dotnet</c> of the runtime the tests run on (its SDK builds the scratch program).</summary>
    static string Dotnet()
    {
        // <root>/shared/Microsoft.NETCore.App/<version>/
        var root = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", ".."));
        return Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
    }

    /// <summary>
    /// Runs <paramref name="file"/> in <paramref name="folder"/> and gives its exit code and its output (stdout; -1 and
    /// the error when it can't start). It gets none of git's or MSBuild's variables of the run around the tests, and no
    /// build server or node that would outlive it (the shared lock, Docs/Engine-Plan.md, "How each step is finished").
    /// </summary>
    internal static (int Code, string Output) Run(string file, string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo(file) { WorkingDirectory = folder, RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        foreach (var name in start.Environment.Keys.ToList())
            if (name.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("MSBUILD", StringComparison.OrdinalIgnoreCase))
                start.Environment.Remove(name);
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        start.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        try
        {
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }
        catch (Win32Exception exception)
        {
            return (-1, exception.Message);
        }
    }
}
