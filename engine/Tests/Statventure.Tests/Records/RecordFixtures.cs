using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Statventure.Records;

namespace Statventure.Tests.Records;

/// <summary>
/// The committed record data (Docs/Assets.md, "Guarding layouts"): Data/Records/Layouts/&lt;type&gt;.txt
/// holds every version's layout signature of a record type, Data/Records/Fixtures/&lt;type&gt;/ the
/// fixtures, each in both forms (&lt;label&gt;.rec binary, &lt;label&gt;.json). A missing fixture or
/// layout version fails its test; run the tests once with STATVENTURE_RECORD_FIXTURES=1 to write the
/// missing ones (never an existing one: a fixture is the frozen record of a version).
/// </summary>
public static class RecordFixtures
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string Folder => Path.Combine(ProjectDirectory(), "Data", "Records");
    public static string LayoutFolder => Path.Combine(Folder, "Layouts");
    public static string FixtureFolder => Path.Combine(Folder, "Fixtures");

    public static bool Recording => Environment.GetEnvironmentVariable("STATVENTURE_RECORD_FIXTURES") == "1";

    /// <summary>Namespaces of deliberately odd record types (signature variants, bad declarations) the guard leaves out.</summary>
    public const string VariantNamespace = "Statventure.Tests.Records.Variants";

    /// <summary>Every record type the guard covers: the engine's and the tests', by name.</summary>
    public static IReadOnlyDictionary<string, Type> Types { get; } = RecordSerializer
        .FindTypes([typeof(RecordSerializer).Assembly, typeof(RecordFixtures).Assembly], type => type.Namespace != VariantNamespace);

    public static string PathOf(string type, string label, string extension) => Path.Combine(FixtureFolder, type, label + extension);

    /// <summary>
    /// Makes sure the fixture pair exists: written from <paramref name="value"/> when recording (in
    /// the versions given, through <paramref name="downgrade"/>), else a failure.
    /// </summary>
    public static void Ensure<T>(string label, T value, IReadOnlyDictionary<string, int>? versions = null, Func<object, int, object>? downgrade = null)
        where T : notnull
    {
        var name = typeof(T).GetCustomAttribute<RecordAttribute>()!.Name;
        var options = new RecordWriteOptions { Versions = versions ?? new Dictionary<string, int>(), Downgrade = downgrade };
        foreach (var (extension, write) in new (string, Func<byte[]>)[]
        {
            (".rec", () => RecordSerializer.WriteBinary(typeof(T), value, options)),
            (".json", () => RecordSerializer.WriteJson(typeof(T), value, options)),
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

    /// <summary>A fixture file read as its root type, with the table entries it read values of.</summary>
    public static (object Value, IReadOnlyList<(string Name, int Version, bool Used)> Table) Load(string type, string label, string extension)
    {
        var bytes = Read(type, label, extension);
        var root = Types.TryGetValue(type, out var known) ? known : throw new InvalidOperationException($"{type} isn't a record type");
        var value = extension == ".rec"
            ? RecordSerializer.ReadBinary(root, bytes, out var session)
            : RecordSerializer.ReadJson(root, bytes, out session);
        return (value, session.Entries.Select(e => (e.Family.Name, e.Version, e.Used)).ToList());
    }

    public static string Text(byte[] utf8) => Encoding.UTF8.GetString(utf8);
}

/// <summary>Compares records field by field: floats by their bits, lists and dictionaries in order.</summary>
public static class RecordAssert
{
    /// <param name="anyNaN">NaNs equal whatever their payload (the JSON form keeps no payload).</param>
    public static void Equal(object? expected, object? actual, bool anyNaN = false) => Compare(expected, actual, "$", anyNaN);

    static void Compare(object? expected, object? actual, string path, bool anyNaN)
    {
        if (expected is null || actual is null)
        {
            Assert.True(expected is null && actual is null, $"{path}: expected {Show(expected)}, got {Show(actual)}");
            return;
        }
        switch (expected)
        {
            case float f:
                var g = Assert.IsType<float>(actual);
                Assert.True(BitConverter.SingleToInt32Bits(f) == BitConverter.SingleToInt32Bits(g) || anyNaN && float.IsNaN(f) && float.IsNaN(g),
                    $"{path}: expected {f:R} (0x{BitConverter.SingleToInt32Bits(f):X8}), got {g:R} (0x{BitConverter.SingleToInt32Bits(g):X8})");
                return;
            case double d:
                var e = Assert.IsType<double>(actual);
                Assert.True(BitConverter.DoubleToInt64Bits(d) == BitConverter.DoubleToInt64Bits(e) || anyNaN && double.IsNaN(d) && double.IsNaN(e),
                    $"{path}: expected {d:R} (0x{BitConverter.DoubleToInt64Bits(d):X16}), got {e:R} (0x{BitConverter.DoubleToInt64Bits(e):X16})");
                return;
            case string or bool or Enum or sbyte or byte or short or ushort or int or uint or long or ulong:
                Assert.True(expected.GetType() == actual.GetType() && expected.Equals(actual), $"{path}: expected {Show(expected)}, got {Show(actual)}");
                return;
            case IEnumerable items when actual is IEnumerable others:
                var a = items.Cast<object?>().ToList();
                var b = others.Cast<object?>().ToList();
                Assert.True(a.Count == b.Count, $"{path}: expected {a.Count} items, got {b.Count}");
                for (var i = 0; i < a.Count; i++)
                    Compare(a[i], b[i], $"{path}[{i}]", anyNaN);
                return;
        }
        var type = expected.GetType();
        Assert.True(type == actual.GetType(), $"{path}: expected a {type.Name}, got a {actual.GetType().Name}");
        // Vectors and matrices by their public fields, records and pairs by their public properties.
        if (type.Namespace == "System.Numerics")
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                Compare(field.GetValue(expected), field.GetValue(actual), $"{path}.{field.Name}", anyNaN);
        else
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (property.GetIndexParameters().Length == 0)
                    Compare(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}", anyNaN);
    }

    static string Show(object? value) => value switch
    {
        null => "null",
        string s => JsonSerializer.Serialize(s),
        _ => value.ToString() ?? "?",
    };
}
