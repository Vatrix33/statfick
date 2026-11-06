using System.Collections.Concurrent;
using Statventure.Packs;

namespace Statventure.Records;

/// <summary>
/// How a kind's entries are named in its folder (P9): the entry path a name is stored at, and the name
/// an entry path stores. A kind's own rule comes with its <see cref="KindAttribute"/>, a reference's with
/// its <see cref="ReferencesAttribute"/>; the music's rule (S6-1) is one a kind can bring. A rule is a
/// type with a public parameterless constructor, made once.
/// </summary>
public abstract class NameRule
{
    static readonly ConcurrentDictionary<Type, NameRule> Rules = new();

    /// <summary>A name is the entry's last path segment, as it is (<see cref="PlainNames"/>).</summary>
    public static NameRule Plain { get; } = Rules.GetOrAdd(typeof(PlainNames), _ => new PlainNames());

    /// <summary>A name is the entry's whole path, anywhere below the folder (<see cref="PathNames"/>).</summary>
    public static NameRule FullPath { get; } = Rules.GetOrAdd(typeof(PathNames), _ => new PathNames());

    /// <summary>The entry path that stores <paramref name="name"/> in <paramref name="folder"/>, or null when the rule can't store it.</summary>
    public abstract string? PathOf(string folder, string name);

    /// <summary>The name the entry at <paramref name="path"/> stores, or null when the rule makes no such path in <paramref name="folder"/>.</summary>
    public abstract string? NameOf(string folder, string path);

    /// <summary>The rule of a mark: the instance of <paramref name="type"/>, or <paramref name="otherwise"/> when it is null.</summary>
    internal static NameRule Of(Type? type, NameRule otherwise, string where)
    {
        if (type == null)
            return otherwise;
        if (!typeof(NameRule).IsAssignableFrom(type) || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
            throw new InvalidOperationException($"{where}: {type.Name} isn't a name rule (a NameRule type with a public parameterless constructor)");
        return Rules.GetOrAdd(type, t => (NameRule)Activator.CreateInstance(t)!);
    }

    /// <summary>The part of <paramref name="path"/> below <paramref name="folder"/>, or null when it isn't below it.</summary>
    protected static string? Below(string folder, string path) =>
        path.StartsWith(folder + "/", StringComparison.Ordinal) ? path[(folder.Length + 1)..] : null;
}

/// <summary>The default rule: an entry's name is its last path segment, directly in the folder (<c>models/effects/sparks</c> is "sparks").</summary>
public sealed class PlainNames : NameRule
{
    public override string? PathOf(string folder, string name) =>
        !name.Contains('/') && PackPath.IsValid(folder + "/" + name) ? folder + "/" + name : null;

    public override string? NameOf(string folder, string path) =>
        Below(folder, path) is { } name && !name.Contains('/') && PackPath.IsValid(path) ? name : null;
}

/// <summary>Names that are whole entry paths, at any depth below the folder (a sequence's stages, <c>models/stages/cave</c>).</summary>
public sealed class PathNames : NameRule
{
    public override string? PathOf(string folder, string name) => NameOf(folder, name);

    public override string? NameOf(string folder, string path) => Below(folder, path) != null && PackPath.IsValid(path) ? path : null;
}
