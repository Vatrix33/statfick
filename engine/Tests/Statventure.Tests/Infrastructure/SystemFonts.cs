using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Statventure.Tests.Infrastructure;

/// <summary>
/// The system fonts the tests use (generic families and fallbacks for characters the game's fonts
/// lack), as fontconfig chose them on the PC the Chromium references were captured on (Fedora,
/// 2026-10). Its answers to the tests' queries are recorded in Data/SystemFonts/fontconfig.json and
/// the font files they name are kept beside it, so every machine (another Linux, the cloud,
/// Windows) resolves fonts the same way without fontconfig. A query that isn't recorded fails: run
/// the tests once with STATVENTURE_RECORD_FONTS=1 on that PC, which asks the real fontconfig and
/// adds the answer and the file.
/// </summary>
public static class SystemFonts
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    static string FontDirectory => Path.Combine(ProjectDirectory(), "Data", "SystemFonts");

    static string TablePath => Path.Combine(FontDirectory, "fontconfig.json");

    static readonly bool Recording = Environment.GetEnvironmentVariable("STATVENTURE_RECORD_FONTS") == "1";
    static readonly Lock Guard = new();
    static SortedDictionary<string, string>? _table;

    /// <summary>fc-match -f %{file} &lt;pattern&gt;: the font file fontconfig picks ("" when none).</summary>
    public static string Match(string pattern) => Answer("match " + pattern, () => Run("fc-match", "-f", "%{file}", pattern).Trim());

    /// <summary>The installed font file with this file name, as fc-list lists it (null when none).</summary>
    public static string? File(string name) => Answer("file " + name, () =>
        Run("fc-list", "-f", "%{file}\n").Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(f => Path.GetFileName(f) == name) ?? "") is { Length: > 0 } path ? path : null;

    static string Answer(string query, Func<string> ask)
    {
        lock (Guard)
        {
            var table = _table ??= System.IO.File.Exists(TablePath)
                ? JsonSerializer.Deserialize<SortedDictionary<string, string>>(System.IO.File.ReadAllText(TablePath), new JsonSerializerOptions())!
                : new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (!table.TryGetValue(query, out var name))
            {
                if (!Recording)
                    throw new InvalidOperationException($"fontconfig's answer to \"{query}\" isn't recorded in {TablePath}: run the tests once with STATVENTURE_RECORD_FONTS=1 on the PC the references come from.");
                var file = ask();
                name = file.Length > 0 && System.IO.File.Exists(file) ? Path.GetFileName(file) : "";
                if (name.Length > 0)
                {
                    Directory.CreateDirectory(FontDirectory);
                    var copy = Path.Combine(FontDirectory, name);
                    if (!System.IO.File.Exists(copy))
                        System.IO.File.Copy(file, copy);
                    else if (!System.IO.File.ReadAllBytes(copy).AsSpan().SequenceEqual(System.IO.File.ReadAllBytes(file)))
                        throw new InvalidOperationException($"two different fonts are named {name} ({file})");
                }
                table[query] = name;
                System.IO.File.WriteAllText(TablePath, JsonSerializer.Serialize(table, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            }
            return name.Length == 0 ? "" : Path.Combine(FontDirectory, name);
        }
    }

    static string Run(string program, params string[] arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo(program) { RedirectStandardOutput = true };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}
