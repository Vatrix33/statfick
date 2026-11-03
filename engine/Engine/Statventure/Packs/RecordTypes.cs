using System.Reflection;
using Statventure.Records;

namespace Statventure.Packs;

/// <summary>
/// The record types some data may hold, by their record names: for reading records whose type isn't
/// known in advance (a pack's dump, extracting a pack, packing records' JSON forms).
/// </summary>
public sealed class RecordTypes
{
    static readonly Lazy<RecordTypes> EngineTypes = new(() => Of(typeof(RecordTypes).Assembly));

    readonly IReadOnlyDictionary<string, Type> _byName;

    public RecordTypes(IReadOnlyDictionary<string, Type> byName) => _byName = byName;

    /// <summary>The engine's own record types.</summary>
    public static RecordTypes Engine => EngineTypes.Value;

    /// <summary>The current record types of <paramref name="assemblies"/>.</summary>
    public static RecordTypes Of(params Assembly[] assemblies) => new(RecordSerializer.FindTypes(assemblies));

    /// <summary>Every type by its record name.</summary>
    public IReadOnlyDictionary<string, Type> ByName => _byName;

    /// <summary>The current type named <paramref name="name"/>, or null.</summary>
    public Type? Find(string name) => _byName.GetValueOrDefault(name);

    /// <summary>The record name of a current record type.</summary>
    internal static string NameOf(Type type) =>
        type.GetCustomAttribute<RecordAttribute>(false)?.Name
        ?? throw new InvalidOperationException($"{type.FullName} isn't a record type (it has no [Record])");
}
