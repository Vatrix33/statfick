using System.Reflection;

namespace Statventure.Packs;

/// <summary>
/// Marks an importer (Docs/Assets.md, "Importers"; S3-3): a class implementing <see cref="IImporter"/>
/// with a public parameterless constructor, beside its area's code, that turns source files whose
/// names end in one of its <see cref="Endings"/> (e.g. <c>".glb"</c>) into pack entries. Packing a
/// folder (<see cref="PackWriter.AddFolder"/>, <c>packer pack</c>) sends each such file through it; the
/// packer finds importers by this attribute in the engine's assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ImporterAttribute(string name, params string[] endings) : Attribute
{
    /// <summary>The importer's name, in messages (e.g. "models").</summary>
    public string Name { get; } = name;

    /// <summary>The endings of the source files' names it takes, each starting with a dot (".glb", ".terrain.json").</summary>
    public IReadOnlyList<string> Endings { get; } = endings;
}

/// <summary>Turns one source file into pack entries (see <see cref="ImporterAttribute"/>).</summary>
public interface IImporter
{
    /// <summary>Imports <see cref="ImportContext.SourcePath"/>, adding its entries through the context.</summary>
    void Import(ImportContext context);
}

/// <summary>An importer found by its attribute.</summary>
public sealed class ImporterInfo
{
    internal ImporterInfo(Type type, string name, IReadOnlyList<string> endings)
    {
        Type = type;
        Name = name;
        Endings = endings;
    }

    public Type Type { get; }
    public string Name { get; }
    public IReadOnlyList<string> Endings { get; }

    internal IImporter Create() => (IImporter)Activator.CreateInstance(Type)!;

    public override string ToString() => $"{Name} ({Type.FullName})";
}

/// <summary>
/// A set of importers, each source file going to the one with the longest ending its name has (a
/// file named only the ending isn't taken). Two importers with one ending, or one name, are an error.
/// </summary>
public sealed class Importers
{
    static readonly Lazy<Importers> EngineImporters = new(() => Find(typeof(Importers).Assembly));

    readonly Dictionary<string, ImporterInfo> _byEnding = new(StringComparer.Ordinal);

    Importers(IEnumerable<ImporterInfo> importers)
    {
        All = [.. importers];
        var names = new Dictionary<string, ImporterInfo>(StringComparer.Ordinal);
        foreach (var importer in All)
        {
            if (!names.TryAdd(importer.Name, importer))
                throw new InvalidOperationException($"{importer} and {names[importer.Name]} are both named {importer.Name}");
            foreach (var ending in importer.Endings)
                if (!_byEnding.TryAdd(ending, importer))
                    throw new InvalidOperationException($"{importer} and {_byEnding[ending]} both take files ending in {ending}");
        }
    }

    /// <summary>The engine's own importers.</summary>
    public static Importers Engine => EngineImporters.Value;

    /// <summary>No importers: a folder packs as its files and records' JSON forms only.</summary>
    public static Importers None { get; } = new([]);

    /// <summary>Every importer of <paramref name="assemblies"/> (classes with <see cref="ImporterAttribute"/>).</summary>
    public static Importers Find(params Assembly[] assemblies) => Find(assemblies, null);

    /// <summary>The importers of <paramref name="assemblies"/> that <paramref name="include"/> accepts.</summary>
    public static Importers Find(IEnumerable<Assembly> assemblies, Func<Type, bool>? include) =>
        Of([.. assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<ImporterAttribute>(false) is not null && include?.Invoke(t) != false)]);

    /// <summary>These importer classes.</summary>
    public static Importers Of(params Type[] types) => new(types.Select(Describe));

    public IReadOnlyList<ImporterInfo> All { get; }

    /// <summary>The importer that takes a source file at <paramref name="path"/>, with the ending that chose it; null when none does.</summary>
    public (ImporterInfo Importer, string Ending)? For(string path)
    {
        var name = path[(path.LastIndexOf('/') + 1)..];
        (ImporterInfo, string)? best = null;
        foreach (var (ending, importer) in _byEnding)
            if (name.Length > ending.Length && name.EndsWith(ending, StringComparison.Ordinal) && (best is null || ending.Length > best.Value.Item2.Length))
                best = (importer, ending);
        return best;
    }

    static ImporterInfo Describe(Type type)
    {
        var attribute = type.GetCustomAttribute<ImporterAttribute>(false)
            ?? throw new InvalidOperationException($"{type.FullName} isn't an importer (it has no [Importer])");
        if (!typeof(IImporter).IsAssignableFrom(type) || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidOperationException($"{type.FullName}: an importer is a class implementing IImporter with a public parameterless constructor");
        if (attribute.Name.Length == 0)
            throw new InvalidOperationException($"{type.FullName}: an importer needs a name");
        if (attribute.Endings.Count == 0)
            throw new InvalidOperationException($"{type.FullName}: an importer takes files of at least one ending");
        foreach (var ending in attribute.Endings)
        {
            // The path rule refuses what would end a name in a dot (".", ".a.").
            if (ending.Length == 0 || ending[0] != '.' || ending.Contains('/') || PackPath.Problem("x" + ending) is not null)
                throw new InvalidOperationException($"{type.FullName}: \"{ending}\" isn't an ending of names under the path rule (a dot, then a–z, 0–9, '-', '_', '.')");
            if (ending == LooseRecord.Extension)
                throw new InvalidOperationException($"{type.FullName}: \"{ending}\" would take records' JSON forms");
        }
        return new ImporterInfo(type, attribute.Name, attribute.Endings);
    }
}

/// <summary>
/// What an importer gets for one source file: the file, the other files it may read (one it reads isn't
/// packed itself, so a terrain's description can take its maps), whether the data the entries go into
/// has a path, and the pack to add entries to. Packing a folder (<see cref="PackWriter.AddFolder(string, RecordTypes?, Importers?)"/>)
/// the other files are the folder's, and so is the data; importing one file on its own
/// (<see cref="PackWriter.ImportFile"/>; Docs/Editor.md, P28) they are what its <see cref="ImportSources"/> give.
/// </summary>
public sealed class ImportContext
{
    /// <summary>Where a file the importer asks for wasn't found, when packing a folder.</summary>
    internal const string InTheFolder = "in the folder being packed";

    /// <summary>Where a file the importer asks for wasn't found, when importing one file.</summary>
    internal const string ForTheImport = "among the files the import can read";

    readonly PackWriter _writer;
    readonly Func<string, string?> _fileOf;
    readonly string _missingWhere;
    readonly Func<string, string?> _sourceOf;
    readonly Func<string, bool> _exists;
    readonly HashSet<string> _used;

    /// <param name="fileOf">The file on disk for a data path, or null.</param>
    /// <param name="missingWhere">Where a file not found was looked for, in the error.</param>
    /// <param name="sourceOf">The name of another importer that takes a data path as its source, or null.</param>
    /// <param name="exists">Whether the data the entries go into has a path.</param>
    /// <param name="used">Collects the paths read besides the source (not packed themselves).</param>
    internal ImportContext(PackWriter writer, ImporterInfo importer, string sourcePath, string sourceFile, string ending, IReadOnlyList<string> sources,
        Func<string, string?> fileOf, string missingWhere, Func<string, string?> sourceOf, Func<string, bool> exists, HashSet<string> used)
    {
        _writer = writer;
        _fileOf = fileOf;
        _missingWhere = missingWhere;
        _sourceOf = sourceOf;
        _exists = exists;
        _used = used;
        Importer = importer;
        SourcePath = sourcePath;
        SourceFile = sourceFile;
        Ending = ending;
        Sources = sources;
    }

    /// <summary>The importer this is for.</summary>
    public ImporterInfo Importer { get; }

    /// <summary>The source file's data path ("models/hero.glb"): its path in the folder, or the path one file is imported as.</summary>
    public string SourcePath { get; }

    /// <summary>The source file on disk (a full path; for loaders that read by path).</summary>
    public string SourceFile { get; }

    /// <summary>The ending that chose the importer (".glb").</summary>
    public string Ending { get; }

    /// <summary>The source path without its ending ("models/hero"): the usual path of what it makes.</summary>
    public string PathWithoutEnding => SourcePath[..^Ending.Length];

    /// <summary>Every file's path in the folder being packed, in ordinal order; importing one file, only its own.</summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>The source file's bytes.</summary>
    public byte[] Read() => File.ReadAllBytes(SourceFile);

    /// <summary>
    /// Whether the data the entries go into has <paramref name="path"/> (what a stylesheet's urls are checked
    /// against): a file of the folder being packed (and, updating a pack, an entry of the pack); importing one
    /// file, what its <see cref="ImportSources.Exists"/> says.
    /// </summary>
    public bool Exists(string path)
    {
        PackPath.Check(path);
        return _exists(path);
    }

    /// <summary>Another file of the folder, by its data path; it isn't packed itself then.</summary>
    public byte[] ReadSource(string path) => File.ReadAllBytes(SourceFileOf(path));

    /// <summary>
    /// Another file of the folder on disk, by its data path (for loaders that read by path); it isn't
    /// packed itself then. A file another importer takes as its source can't be used. Importing one file,
    /// the files its <see cref="ImportSources.FileOf"/> gives.
    /// </summary>
    public string SourceFileOf(string path)
    {
        PackPath.Check(path);
        var file = path == SourcePath ? SourceFile : _fileOf(path)
            ?? throw new FileNotFoundException($"No '{path}' {_missingWhere}", path);
        if (path != SourcePath && _sourceOf(path) is { } other)
            throw new InvalidOperationException($"'{path}' is a source of the {other} importer, so the {Importer.Name} importer can't use it too");
        _used.Add(path);
        return file;
    }

    /// <summary>Adds a file entry.</summary>
    public void Add(string path, ReadOnlySpan<byte> content) => _writer.Add(path, content);

    /// <summary>Adds a record entry.</summary>
    public void AddRecord<T>(string path, T value) where T : notnull => _writer.AddRecord(path, value);

    /// <summary>Adds a record entry of type <paramref name="type"/>.</summary>
    public void AddRecord(string path, Type type, object value) => _writer.AddRecord(path, type, value);
}
