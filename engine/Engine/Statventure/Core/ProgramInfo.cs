using System.Reflection;

namespace Statventure.Core;

/// <summary>
/// A program's name, version and commit as its build recorded them in its program file (M12-6, Q1–Q3;
/// Docs/Packaging.md, "The version"). The name is the assembly's; the version and the commit are the two halves of
/// its informational version, <c>&lt;version&gt;+&lt;commit&gt;</c>. A release is built with <c>-p:Version=2026.11.8.1</c> (the
/// release script's, Q1, Q2); any other build is a dev build, its version <see cref="Dev"/> (Directory.Build.props).
/// The SDK adds the commit from git as it builds (Source Link's <c>SourceRevisionId</c>); a build without git (an
/// unpacked source archive) records none, so its <see cref="Commit"/> is null. Where it shows (Q3): the editor's
/// title (the version), the first lines of every log and crash file (the version and the commit).
/// </summary>
/// <param name="Name">The program's assembly name (<c>Statventure.Editor</c>, <c>Statventure.Sandbox</c>).</param>
/// <param name="Version">The release's version (<c>2026.11.8.1</c>), or <see cref="Dev"/>.</param>
/// <param name="Commit">The commit the build was made from (git's full hash), or null when the build recorded none.</param>
public sealed record ProgramInfo(string Name, string Version, string? Commit)
{
    /// <summary>The version of a build that isn't a release (Q1).</summary>
    public const string Dev = "dev";

    /// <summary>Whether this is a dev build, not a release.</summary>
    public bool IsDev => Version == Dev;

    /// <summary>
    /// What <paramref name="program"/>'s file records. A program passes its own assembly, the one holding its
    /// <c>Main</c> or its <see cref="Application"/> (in a single-file program it is inside the executable).
    /// </summary>
    public static ProgramInfo Of(Assembly program) =>
        Parse(program.GetName().Name ?? "", program.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>
    /// The program <paramref name="name"/> whose informational version is <paramref name="informationalVersion"/>:
    /// the version before the first <c>+</c> and the commit after it (none without a <c>+</c>); no version (no
    /// attribute) is <see cref="Dev"/>.
    /// </summary>
    public static ProgramInfo Parse(string name, string? informationalVersion)
    {
        var text = informationalVersion ?? "";
        var plus = text.IndexOf('+');
        var version = plus < 0 ? text : text[..plus];
        var commit = plus < 0 ? "" : text[(plus + 1)..];
        return new ProgramInfo(name, version.Length > 0 ? version : Dev, commit.Length > 0 ? commit : null);
    }
}
