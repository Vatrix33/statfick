using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Statventure.Core;
using Statventure.Editor.Shell;

namespace Statventure.Tests.Core;

/// <summary>
/// The program's version and commit (M12 step 1; Docs/Packaging.md, "The version"): read from its program file's
/// informational version, <c>&lt;version&gt;+&lt;commit&gt;</c>; <c>dev</c> for a build that isn't a release (Q1). The tests'
/// own build is a dev build made from the repo's commit; a release build is <see cref="ProgramBuildTests"/>'. And
/// .NET's invariant globalization for the programs and the tests (Q8).
/// </summary>
public class ProgramInfoTests
{
    [Theory]
    [InlineData("2026.11.8.1+0123456789abcdef0123456789abcdef01234567", "2026.11.8.1", "0123456789abcdef0123456789abcdef01234567")]
    [InlineData("2026.11.8.2+abc", "2026.11.8.2", "abc")]
    [InlineData("dev+abc", "dev", "abc")]
    [InlineData("2026.11.8.1", "2026.11.8.1", null)]
    [InlineData("dev", "dev", null)]
    [InlineData("2026.11.8.1+", "2026.11.8.1", null)]
    [InlineData("+abc", "dev", "abc")]
    [InlineData("", "dev", null)]
    [InlineData(null, "dev", null)]
    public void TheVersionIsBeforeThePlusAndTheCommitAfterIt(string? informational, string version, string? commit)
    {
        var info = ProgramInfo.Parse("Statventure.Editor", informational);
        Assert.Equal(new ProgramInfo("Statventure.Editor", version, commit), info);
        Assert.Equal(version == "dev", info.IsDev);
    }

    [Fact]
    public void AReleasesVersionIsNotDev()
    {
        Assert.Equal("dev", ProgramInfo.Dev);
        Assert.False(ProgramInfo.Parse("p", "2026.11.8.1+abc").IsDev);
        Assert.True(ProgramInfo.Parse("p", "dev+abc").IsDev);
    }

    [Fact]
    public void TheTestsBuildIsADevBuildOfTheReposCommit()
    {
        // test.sh builds without a version (Directory.Build.props: "dev"); the SDK records the commit from git.
        var commit = HeadCommit();
        foreach (var (assembly, name) in new[]
        {
            (typeof(ProgramInfoTests).Assembly, "Statventure.Tests"),
            (typeof(EditorView).Assembly, "Statventure.Editor"),
            (typeof(ProgramInfo).Assembly, "Statventure"),
        })
            Assert.Equal(new ProgramInfo(name, "dev", commit), ProgramInfo.Of(assembly));
    }

    [Fact]
    public void TheTestsRunInInvariantGlobalization()
    {
        // Q8: the switch is in the tests' runtime configuration, and the runtime acts on it (no ICU: no culture but the
        // invariant one, comparisons by code point).
        Assert.True(AppContext.TryGetSwitch("System.Globalization.Invariant", out var invariant) && invariant);
        Assert.Equal("", CultureInfo.CurrentCulture.Name);
        Assert.Throws<CultureNotFoundException>(() => new CultureInfo("de-DE"));
        Assert.True(string.Compare("a", "B", StringComparison.InvariantCulture) > 0);
    }

    [Fact]
    public void TheEditorRunsInInvariantGlobalization()
    {
        // The editor's runtime configuration, beside the tests' as the build copies it (the sandbox's: ProgramBuildTests).
        var config = Path.Combine(AppContext.BaseDirectory, "Statventure.Editor.runtimeconfig.json");
        Assert.True(InvariantIn(config));
    }

    /// <summary>Whether the runtime configuration file <paramref name="path"/> turns invariant globalization on.</summary>
    internal static bool InvariantIn(string path)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        return json.RootElement.GetProperty("runtimeOptions"u8).TryGetProperty("configProperties"u8, out var properties)
            && properties.TryGetProperty("System.Globalization.Invariant"u8, out var value) && value.GetBoolean();
    }

    /// <summary>The engine folder (this file's project's, two up).</summary>
    internal static string EngineFolder([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", ".."));

    /// <summary>The commit the repo's work tree is at (git's full hash), or null when it isn't in a git repository.</summary>
    static string? HeadCommit()
    {
        var (code, output) = ProgramBuildTests.Run("git", EngineFolder(), "rev-parse", "HEAD");
        return code == 0 ? output.Trim() : null;
    }
}
