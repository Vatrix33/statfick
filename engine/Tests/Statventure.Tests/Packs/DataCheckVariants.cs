using Statventure.Packs;
using Statventure.Records;

// Kinds whose checks ask for the data or the lookup without reading anything (GM1 step 6b, ModCheckerTests: a check that
// only asks whether it is given data still differs with and without it), and one that asks the data in each of its ways
// and reports the answer. In the variants' namespace, which the layout guard leaves out.
namespace Statventure.Tests.Records.Variants;

/// <summary>
/// With the data, reports what the data answers when asked about <see cref="At"/> in one way (<see cref="Way"/>): whether a
/// path is there, whether it is a record, its bytes' length, the record loaded as this kind or as any kind known, where it
/// comes from, the pack a problem with it names, a folder's paths, how many mounts there are. Alone, nothing.
/// </summary>
[Record("variant.asks-data", 1)]
public sealed record AsksData
{
    [Field(1)] public string Way { get; init; } = "exists";
    [Field(2)] public string At { get; init; } = "";

    /// <summary>The kinds the "kinds" way loads with: the engine's and this one.</summary>
    public static readonly RecordTypes Kinds = new(new Dictionary<string, Type>(RecordTypes.Engine.ByName) { ["variant.asks-data"] = typeof(AsksData) });

    [Check]
    static void Rules(AsksData value, CheckContext check)
    {
        if (check.Data is not { } data)
            return;
        string answer;
        try
        {
            answer = value.Way switch
            {
                "exists" => data.Exists(value.At).ToString(),
                "record" => data.IsRecord(value.At).ToString(),
                "bytes" => data.ReadAllBytes(value.At).Length.ToString(),
                "load" => ((AsksData)data.Load(typeof(AsksData), value.At)).Way,
                "kinds" => RecordTypes.NameOf(data.Load(Kinds, value.At).GetType()),
                "origin" => Path.GetFileName(data.Origin(value.At)) ?? "nowhere",
                "problem" => Path.GetFileName(data.Problem(value.At, "asked").File),
                "list" => string.Join(",", data.List(value.At)),
                "mounts" => data.Mounts.Count.ToString(),
                _ => "an unknown way",
            };
        }
        catch (Exception problem) when (problem is not OutOfMemoryException)
        {
            answer = $"refused ({problem.GetType().Name})";
        }
        check.Report($"{value.Way} {value.At}: {answer}");
    }
}

/// <summary>Refused when checked alone, without the data.</summary>
[Record("variant.wants-data", 1)]
public sealed record WantsData
{
    [Field(1)] public string Name { get; init; } = "";

    [Check]
    static void Rules(WantsData value, CheckContext check)
    {
        if (check.Data is null)
            check.Report("checked without the data");
    }
}

/// <summary>Refused when checked without a reference lookup.</summary>
[Record("variant.wants-lookup", 1)]
public sealed record WantsLookup
{
    [Field(1)] public string Name { get; init; } = "";

    [Check]
    static void Rules(WantsLookup value, CheckContext check)
    {
        if (check.References is null)
            check.Report("checked without the lookup");
    }
}
