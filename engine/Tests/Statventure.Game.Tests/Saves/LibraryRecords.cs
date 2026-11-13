using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Statventure.Game.Data.Saves;
using Statventure.Records;

namespace Statventure.Game.Tests;

/// <summary>
/// The game data library's committed record data (Docs/Assets.md, "Guarding layouts", Q13 of M9; the engine's tests do
/// the same for the engine's records, <c>Tests/Statventure.Tests/Records/RecordFixtures.cs</c>): Data/Records/Layouts/
/// &lt;type&gt;.txt holds every version's layout signature of a record type of the library, Data/Records/Fixtures/
/// &lt;type&gt;/ its fixtures, each in both forms (&lt;label&gt;.rec binary, &lt;label&gt;.json). A missing fixture or layout
/// version fails its test; run the tests once with STATVENTURE_RECORD_FIXTURES=1 to write the missing ones (never an
/// existing one: a fixture is the frozen record of a version). Written through the serializer's public forms (the
/// current versions): a past version's fixture is the one committed while it was current.
/// </summary>
static class LibraryRecords
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string Folder => Path.Combine(ProjectDirectory(), "Data", "Records");
    public static string LayoutFolder => Path.Combine(Folder, "Layouts");
    public static string FixtureFolder => Path.Combine(Folder, "Fixtures");

    public static bool Recording => Environment.GetEnvironmentVariable("STATVENTURE_RECORD_FIXTURES") == "1";

    /// <summary>
    /// The development-only stand-in kind (GM1-P3) goes when GM2's first real kind arrives; it is left out of the guard,
    /// as it was before GM1 (no layout or fixture was ever committed for it).
    /// </summary>
    public const string StandInNamespace = "Statventure.Game.Data.StandIn";

    /// <summary>Every record type of the data library the guard covers, by name.</summary>
    public static IReadOnlyDictionary<string, Type> Types { get; } =
        RecordSerializer.FindTypes([typeof(SaveGame).Assembly], type => type.Namespace != StandInNamespace);

    public static string PathOf(string type, string label, string extension) => Path.Combine(FixtureFolder, type, label + extension);

    /// <summary>Makes sure the fixture pair of <paramref name="value"/> exists: written when recording, else a failure.</summary>
    public static void Ensure<T>(string label, T value) where T : notnull
    {
        var name = typeof(T).GetCustomAttribute<RecordAttribute>()!.Name;
        foreach (var (extension, write) in new (string, Func<byte[]>)[]
        {
            (".rec", () => RecordSerializer.ToBinary(value)),
            (".json", () => RecordSerializer.ToJsonUtf8(typeof(T), value)),
        })
        {
            var path = PathOf(name, label, extension);
            if (File.Exists(path))
                continue;
            Assert.True(Recording, $"no fixture {path}: run the tests with STATVENTURE_RECORD_FIXTURES=1 to write it");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, write());
        }
    }

    public static byte[] Read(string type, string label, string extension) => File.ReadAllBytes(PathOf(type, label, extension));

    /// <summary>Every fixture file: its root type's name, label and form.</summary>
    public static IEnumerable<(string Type, string Label, string Extension)> All() =>
        Directory.Exists(FixtureFolder)
            ? Directory.EnumerateFiles(FixtureFolder, "*", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(path => (Path.GetFileName(Path.GetDirectoryName(path)!), Path.GetFileNameWithoutExtension(path), Path.GetExtension(path)))
            : [];

    /// <summary>A fixture read as its root type (upgraded to the current version).</summary>
    public static object Load(string type, string label, string extension)
    {
        var bytes = Read(type, label, extension);
        var root = Types[type];
        return extension == ".rec" ? RecordSerializer.FromBinary(root, bytes) : RecordSerializer.FromJson(root, bytes);
    }

    /// <summary>
    /// A fixture's type table, the record types and versions it was written in: the binary form's leading table (a
    /// LEB128 count, then each name as a LEB128 length and UTF-8, and its version as LEB128), the JSON form's
    /// <c>"$types"</c>.
    /// </summary>
    public static IReadOnlyList<(string Name, int Version)> Table(string type, string label, string extension)
    {
        var bytes = Read(type, label, extension);
        var table = new List<(string, int)>();
        if (extension == ".json")
        {
            using var json = JsonDocument.Parse(bytes);
            foreach (var entry in json.RootElement.GetProperty("$types"u8).EnumerateObject())
                table.Add((entry.Name, entry.Value.GetInt32()));
            return table;
        }
        var at = 0;
        ulong Leb128()
        {
            ulong value = 0;
            for (var shift = 0; ; shift += 7)
            {
                var b = bytes[at++];
                value |= (ulong)(b & 0x7F) << shift;
                if (b < 0x80)
                    return value;
            }
        }
        var count = (int)Leb128();
        for (var i = 0; i < count; i++)
        {
            var length = (int)Leb128();
            var name = Encoding.UTF8.GetString(bytes, at, length);
            at += length;
            table.Add((name, (int)Leb128()));
        }
        return table;
    }

    /// <summary>A value of a library record type in its JSON form, to compare two values field by field.</summary>
    public static string Json(Type type, object value) => RecordSerializer.ToJson(type, value);
}
