using System.Reflection;
using System.Reflection.Emit;
using Statventure.Editor.Game;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// Scratch game folders for the editor's lookup of the game (M12 step 6): the repo's packs and the stand-in data library
/// as the repo's build makes it (what <c>--repo</c> loads; the tests build it before they run, never referencing it:
/// the editor's loader is the only way in, the engine keeping one record kind per name), and data libraries of the
/// tests' own (another version, another build, not a library), made with <see cref="PersistedAssemblyBuilder"/> and
/// never loaded.
/// </summary>
static class GameTesting
{
    /// <summary>The stand-in data library as the repo's build of the tests' configuration makes it.</summary>
    public static string BuiltLibrary => GameLibrary.InRepo(EditorHarness.EngineFolder);

    /// <summary>The stand-in's test kind's record name.</summary>
    public const string StandInKind = "stand-in.item";

    /// <summary>
    /// A game's folder at <paramref name="folder"/> as its download lays it out: <c>packs/</c> with the repo's packs, the
    /// stand-in library beside (each left out when asked); gives the folder.
    /// </summary>
    public static string MakeGame(string folder, bool packs = true, bool library = true)
    {
        Directory.CreateDirectory(folder);
        if (packs)
        {
            Directory.CreateDirectory(Path.Combine(folder, "packs"));
            foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
                File.Copy(pack, Path.Combine(folder, "packs", Path.GetFileName(pack)), overwrite: true);
        }
        if (library)
            File.Copy(BuiltLibrary, Path.Combine(folder, GameLibrary.FileName), overwrite: true);
        return folder;
    }

    /// <summary>The repo's dev setup laid out under <paramref name="engine"/>: <c>Assets/Packs</c> the repo's packs, the library's build where <c>--repo</c> looks.</summary>
    public static string MakeRepo(string engine)
    {
        var packs = Path.Combine(engine, "Assets", "Packs");
        Directory.CreateDirectory(packs);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(packs, Path.GetFileName(pack)), overwrite: true);
        var library = GameLibrary.InRepo(engine);
        Directory.CreateDirectory(Path.GetDirectoryName(library)!);
        File.Copy(BuiltLibrary, library, overwrite: true);
        return engine;
    }

    /// <summary>
    /// A library of the tests' own at <paramref name="file"/>: an assembly named <paramref name="name"/> whose informational
    /// version is <paramref name="informational"/> (none: no attribute), one empty type in it; each one another build.
    /// </summary>
    public static string FakeLibrary(string file, string? informational, string name = GameLibrary.AssemblyName)
    {
        var assembly = new PersistedAssemblyBuilder(new AssemblyName(name), typeof(object).Assembly);
        if (informational is not null)
            assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!, [informational]));
        assembly.DefineDynamicModule(name).DefineType("Nothing", TypeAttributes.Public).CreateType();
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        assembly.Save(file);
        return file;
    }

    /// <summary>
    /// A library of the tests' own at <paramref name="file"/> whose one type derives from a type of an assembly named
    /// <paramref name="missing"/>, which isn't there: its kinds can't be loaded.
    /// </summary>
    public static string FakeLibraryNeeding(string file, string missing)
    {
        var part = new PersistedAssemblyBuilder(new AssemblyName(missing), typeof(object).Assembly);
        var baseType = part.DefineDynamicModule(missing).DefineType("Part", TypeAttributes.Public);
        baseType.CreateType();
        var assembly = new PersistedAssemblyBuilder(new AssemblyName(GameLibrary.AssemblyName), typeof(object).Assembly);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!, ["dev"]));
        assembly.DefineDynamicModule(GameLibrary.AssemblyName).DefineType("Needs", TypeAttributes.Public, baseType).CreateType();
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        assembly.Save(file);
        return file;
    }

    /// <summary>The stand-in kind's type as the editor loads it (the run's one build of the library).</summary>
    public static Type StandInType()
    {
        var library = GameLibrary.Read(BuiltLibrary);
        return GameLibraryLoader.Run.Kinds(library).Types.Find(StandInKind) ?? throw new InvalidOperationException("the stand-in library has no " + StandInKind);
    }

    /// <summary>A value of the stand-in kind.</summary>
    public static object StandIn(string title, int price, string rarity)
    {
        var type = StandInType();
        var value = Activator.CreateInstance(type)!;
        type.GetProperty("Title")!.SetValue(value, title);
        type.GetProperty("Price")!.SetValue(value, price);
        type.GetProperty("Rarity")!.SetValue(value, rarity);
        return value;
    }
}
