using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Statventure.Core;
using Statventure.Packs;

namespace Statventure.Editor.Game;

/// <summary>The game's data library as its file says, read without loading it: its file, name, version and commit (its build's <see cref="ProgramInfo"/>) and its build's id (the module's version id, the same for every copy of one build).</summary>
public sealed record GameLibraryFile(string File, ProgramInfo Program, Guid Build)
{
    /// <summary>Its version (a release's <c>2026.11.8.1</c>, else <c>dev</c>).</summary>
    public string Version => Program.Version;
}

/// <summary>A data library that can't be used: its file can't be read as one, or its kinds can't be loaded.</summary>
public sealed class GameLibraryException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// The game's data library (M12-9, Q19; Docs/Packaging.md, "The editor on its own"): <c>Statventure.Game.Data.dll</c>
/// in the game's folder, the record kinds the game links, which the editor loads to take its kinds and importers (the
/// editor's download has none of its own). Its version is read from its metadata first (<see cref="Read"/>, Q18's check:
/// a library the editor refuses is never loaded); then it is loaded (<see cref="GameLibraryLoader"/>) into a load context
/// of its own whose engine is the editor's (the same engine, hence Q18).
/// </summary>
public static class GameLibrary
{
    /// <summary>The library's assembly name.</summary>
    public const string AssemblyName = "Statventure.Game.Data";

    /// <summary>Its file's name in the game's folder.</summary>
    public const string FileName = AssemblyName + ".dll";

    /// <summary>
    /// The library's project in the repo, from the engine's folder (the dev setup, <c>--repo</c>, takes the library from
    /// its build; the manager's decision Q-A, M12 step 6). Until the port the stand-in; the port changes this when it
    /// makes its own.
    /// </summary>
    public const string RepoProject = "Samples/Statventure.Game.Data";


    /// <summary>
    /// The library as the repo's build makes it for the dev setup: <c>&lt;engine&gt;/Samples/Statventure.Game.Data/bin/&lt;the
    /// editor's configuration&gt;/&lt;its target&gt;/Statventure.Game.Data.dll</c> (<c>editor.sh</c> builds both in Release, the tests
    /// in Debug).
    /// </summary>
    public static string InRepo(string engineFolder) =>
        Path.GetFullPath(Path.Combine(engineFolder, RepoProject, "bin", Configuration, Target, FileName));

    /// <summary>The configuration the editor was built in (Debug, Release).</summary>
    internal static string Configuration =>
        typeof(GameLibrary).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Release";

    /// <summary>The editor's target framework as a build folder names it (<c>net10.0</c>).</summary>
    internal static string Target
    {
        get
        {
            var name = typeof(GameLibrary).Assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName ?? "";
            var version = name[(name.IndexOf("Version=v", StringComparison.Ordinal) + "Version=v".Length)..];
            return "net" + version;
        }
    }

    /// <summary>
    /// What <paramref name="file"/>'s metadata says (it isn't loaded): its name, its informational version as
    /// <see cref="ProgramInfo"/> reads one (<c>&lt;version&gt;+&lt;commit&gt;</c>, none: <c>dev</c>) and its build id. Throws a
    /// <see cref="GameLibraryException"/> for a file that isn't a .NET assembly or not the game's data library, an
    /// <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> for one that can't be read.
    /// </summary>
    public static GameLibraryFile Read(string file)
    {
        using var stream = File.OpenRead(file);
        try
        {
            // A file that isn't a .NET assembly (no PE headers, no metadata, a module without an assembly) throws: caught below.
            using var pe = new PEReader(stream);
            var metadata = pe.GetMetadataReader();
            var assembly = metadata.GetAssemblyDefinition();
            var name = metadata.GetString(assembly.Name);
            if (name != AssemblyName)
                throw new GameLibraryException($"it is the assembly {name}, not {AssemblyName}");
            string? informational = null;
            foreach (var handle in assembly.GetCustomAttributes())
            {
                var attribute = metadata.GetCustomAttribute(handle);
                if (attribute.Constructor.Kind != HandleKind.MemberReference)
                    continue;
                var constructor = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                if (constructor.Parent.Kind != HandleKind.TypeReference)
                    continue;
                var type = metadata.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                if (metadata.GetString(type.Namespace) != "System.Reflection" || metadata.GetString(type.Name) != nameof(AssemblyInformationalVersionAttribute))
                    continue;
                // Its one string argument after the custom attribute blob's prolog.
                var blob = metadata.GetBlobReader(attribute.Value);
                if (blob.ReadUInt16() == 1)
                    informational = blob.ReadSerializedString();
            }
            var build = metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
            return new GameLibraryFile(Path.GetFullPath(file), ProgramInfo.Parse(name, informational), build);
        }
        catch (Exception problem) when (problem is BadImageFormatException or InvalidOperationException)
        {
            throw new GameLibraryException("it isn't a .NET assembly", problem);
        }
    }
}

/// <summary>
/// Loads the game's data library for its kinds (<see cref="GameLibrary"/>), once per run: the engine keeps one record
/// kind per record name for the whole process, so a run uses one build of the library; another copy of that build is
/// the same library (its build id), a different build can only be used by starting the editor again (the manager's
/// decision Q-C, M12 step 6). <see cref="Run"/> is the program's; a test may make its own to see the loading's rules
/// apart from what the run has loaded (its kinds then unused: their names would be the run's library's).
/// </summary>
public sealed class GameLibraryLoader
{
    readonly Lock _gate = new();
    (GameLibraryFile File, Assembly Assembly)? _loaded;

    /// <summary>The run's loader, the one the editor uses.</summary>
    public static GameLibraryLoader Run { get; } = new();

    /// <summary>The library this run has loaded and uses the kinds of, or null.</summary>
    public GameLibraryFile? Loaded
    {
        get
        {
            lock (_gate)
                return _loaded?.File;
        }
    }

    /// <summary>
    /// Whether this run can use <paramref name="library"/>: none is loaded yet, or the one loaded is the same build
    /// (another copy of it is the same library). False: another build is loaded (<see cref="Loaded"/>), and only a new
    /// start of the editor can use this one.
    /// </summary>
    public bool CanUse(GameLibraryFile library)
    {
        lock (_gate)
            return _loaded is not { } loaded || loaded.File.Build == library.Build;
    }

    /// <summary>
    /// The library's record kinds and importers, the engine's with them: loaded into a load context of its own (its
    /// engine, BRUTAL and .NET the editor's) unless the same build already is (then that one's). Throws an
    /// <see cref="InvalidOperationException"/> when another build is loaded (<see cref="CanUse"/>), a
    /// <see cref="GameLibraryException"/> when its kinds can't be loaded (built against another engine, a part
    /// missing, two kinds of one name).
    /// </summary>
    public (RecordTypes Types, Importers Importers) Kinds(GameLibraryFile library)
    {
        lock (_gate)
        {
            if (_loaded is { } loaded)
            {
                if (loaded.File.Build != library.Build)
                    throw new InvalidOperationException($"another build of the game's data library is loaded (from {loaded.File.File})");
                return KindsOf(loaded.Assembly);
            }
            Assembly assembly;
            (RecordTypes, Importers) kinds;
            try
            {
                assembly = new LibraryContext(library.File).LoadFromAssemblyPath(library.File);
                if (assembly.ManifestModule.ModuleVersionId != library.Build)
                    throw new GameLibraryException("it changed while it was being read");
                kinds = KindsOf(assembly);
            }
            catch (Exception problem) when (problem is not GameLibraryException
                and (ReflectionTypeLoadException or TypeLoadException or FileLoadException or FileNotFoundException or BadImageFormatException
                    or InvalidOperationException or MissingMemberException or ArgumentException))
            {
                var why = problem is ReflectionTypeLoadException types && types.LoaderExceptions.FirstOrDefault(e => e is not null) is { } first ? first.Message : problem.Message;
                throw new GameLibraryException($"its kinds can't be loaded: {why}", problem);
            }
            _loaded = (library, assembly);
            Log.Info("Editor", $"the game's data library {library.File}: {library.Version}{(library.Program.Commit is { } commit ? ", commit " + commit : "")}, {kinds.Item1.ByName.Count} record kinds");
            return kinds;
        }
    }

    static (RecordTypes, Importers) KindsOf(Assembly library)
    {
        var engine = typeof(RecordTypes).Assembly;
        return (RecordTypes.Of(engine, library), Importers.Find(engine, library));
    }

    /// <summary>The library's own load context: what it names beyond itself (the engine, BRUTAL, .NET) is the editor's.</summary>
    sealed class LibraryContext(string file) : AssemblyLoadContext("the game's data library " + file)
    {
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }
}
