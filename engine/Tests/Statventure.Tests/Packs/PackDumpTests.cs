using System.Text;
using System.Text.Json;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>A pack's dump (Docs/Assets.md, "Reviewing packs"; Q14, S3-2): its exact text, problems shown in place, which files show their text.</summary>
public class PackDumpTests
{
    static PackFile Pack(TempFolder folder, PackWriter writer) => PackFile.Open(folder.Write("p.pak", Bytes(writer)));

    static JsonElement Entry(string dump, string path) =>
        JsonDocument.Parse(dump).RootElement.GetProperty("entries"u8).EnumerateArray().Single(e => e.GetProperty("path"u8).GetString() == path);

    [Fact]
    public void TheDumpIsThisText()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("a/record", new Part { Name = "horn", Shade = Shade.Golden });
        writer.Add("b/text.svg", "<svg title=\"é\">\r\n  <g/>\n</svg>\n"u8);
        writer.Add("c/ab", Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("ab", 100))));
        writer.Add("d/binary", [0, 1, 2]);
        writer.Add("e/empty", []);
        using var pack = Pack(folder, writer);
        PackEntry E(string path) => pack.Find(path)!;
        Assert.False(E("a/record").Compressed);
        Assert.True(E("c/ab").Compressed);

        var expected = $$"""
            {
              "format": 1,
              "count": 5,
              "entries": [
                {
                  "path": "a/record",
                  "kind": "record",
                  "length": {{E("a/record").ContentLength}},
                  "stored": "raw",
                  "storedLength": {{E("a/record").ContentLength}},
                  "sha256": "{{E("a/record").HashHex}}",
                  "record": {
                    "$types": {
                      "test.part": 1
                    },
                    "name": "horn",
                    "weight": 1,
                    "shade": "golden"
                  }
                },
                {
                  "path": "b/text.svg",
                  "kind": "file",
                  "length": 32,
                  "stored": "raw",
                  "storedLength": 32,
                  "sha256": "{{E("b/text.svg").HashHex}}",
                  "text": [
                    "<svg title=\"é\">\r",
                    "  <g/>",
                    "</svg>",
                    ""
                  ]
                },
                {
                  "path": "c/ab",
                  "kind": "file",
                  "length": 200,
                  "stored": "brotli",
                  "storedLength": {{E("c/ab").StoredLength}},
                  "sha256": "{{E("c/ab").HashHex}}",
                  "text": [
                    "{{string.Concat(Enumerable.Repeat("ab", 100))}}"
                  ]
                },
                {
                  "path": "d/binary",
                  "kind": "file",
                  "length": 3,
                  "stored": "raw",
                  "storedLength": 3,
                  "sha256": "{{E("d/binary").HashHex}}"
                },
                {
                  "path": "e/empty",
                  "kind": "file",
                  "length": 0,
                  "stored": "raw",
                  "storedLength": 0,
                  "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                  "text": [
                    ""
                  ]
                }
              ]
            }

            """;
        Assert.Equal(expected, PackDump.ToText(pack, TestTypes));
        Assert.Equal(Encoding.UTF8.GetBytes(expected), PackDump.ToUtf8(pack, TestTypes));
    }

    [Fact]
    public void ARecordShowsItsJsonFormAtAnyDepth()
    {
        Shape shape = new Circle { Radius = 1 };
        for (var i = 0; i < 40; i++)
            shape = new Square { Side = i, Inner = shape, Tint = $"tint {i}" };
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("deep", shape);
        writer.AddRecord("ogre", LooseRecordTests.Ogre);
        using var pack = Pack(folder, writer);
        var dump = PackDump.ToText(pack, TestTypes);
        Assert.True(JsonElement.DeepEquals(JsonDocument.Parse(RecordSerializer.ToJson(shape)).RootElement, Entry(dump, "deep").GetProperty("record"u8)));
        Assert.True(JsonElement.DeepEquals(JsonDocument.Parse(RecordSerializer.ToJson(LooseRecordTests.Ogre)).RootElement, Entry(dump, "ogre").GetProperty("record"u8)));
        // Every line of a record sits inside its entry (the entry's members are 6 spaces in).
        var record = dump[(dump.IndexOf("\"record\": {", StringComparison.Ordinal) + 11)..];
        var lines = record[..record.IndexOf("\n    }", StringComparison.Ordinal)].Split('\n').Skip(1).ToArray();
        Assert.True(lines.Length > 80);
        Assert.All(lines[..^1], line => Assert.StartsWith("        ", line));
        Assert.Equal("      }", lines[^1]);
    }

    [Fact]
    public void AnEntryThatCantBeShownShowsItsProblem()
    {
        var part = RecordSerializer.ToBinary(new Part { Name = "horn" });
        var raw = new RawPack().Raw("a/damaged", [1, 2, 3]).Raw("b/unknown", part).Raw("c/unreadable", [.. part, 7]).Raw("d/no-table", [0]).Raw("e/fine", "fine"u8.ToArray());
        raw.Entries[0].Hash = new byte[32];
        raw.Entries[1].Flags = raw.Entries[2].Flags = raw.Entries[3].Flags = 2;
        using var folder = new TempFolder();
        using var pack = PackFile.Open(folder.Write("p.pak", raw.Build()));
        // The engine's types don't know test.part.
        var dump = PackDump.ToText(pack);
        Assert.Equal("the content doesn't match its hash (damaged)", Entry(dump, "a/damaged").GetProperty("problem"u8).GetString());
        Assert.Equal("a record of type test.part, which isn't a record type known here", Entry(dump, "b/unknown").GetProperty("problem"u8).GetString());
        Assert.False(Entry(dump, "b/unknown").TryGetProperty("record"u8, out _));
        Assert.Equal("not a readable record: the type table is empty", Entry(dump, "d/no-table").GetProperty("problem"u8).GetString());
        Assert.Equal(["fine"], Entry(dump, "e/fine").GetProperty("text"u8).EnumerateArray().Select(l => l.GetString()));
        var known = PackDump.ToText(pack, TestTypes);
        Assert.Equal("horn", Entry(known, "b/unknown").GetProperty("record"u8).GetProperty("name"u8).GetString());
        Assert.Equal("not a readable test.part record: 1 bytes follow the record", Entry(known, "c/unreadable").GetProperty("problem"u8).GetString());
        // The fields before the problem are there.
        Assert.Equal("record", Entry(known, "c/unreadable").GetProperty("kind"u8).GetString());
        Assert.Equal(new byte[32].Length * 2, Entry(dump, "a/damaged").GetProperty("sha256"u8).GetString()!.Length);
    }

    public static TheoryData<string, byte[], bool> Texts => new()
    {
        { "ASCII", "plain"u8.ToArray(), true },
        { "tab, CR and LF", "a\tb\r\nc\n"u8.ToArray(), true },
        { "accents", "café"u8.ToArray(), true },
        { "beyond the BMP", "a 🜁 b"u8.ToArray(), true },
        { "a byte order mark", "\uFEFFtext"u8.ToArray(), true },
        { "empty", [], true },
        { "a C0 control", "a\u0001b"u8.ToArray(), false },
        { "NUL", "a\0b"u8.ToArray(), false },
        { "form feed", "a\fb"u8.ToArray(), false },
        { "DEL", "a\u007Fb"u8.ToArray(), false },
        { "a C1 control", "a\u0085b"u8.ToArray(), false },
        { "a lone UTF-8 lead byte", [(byte)'a', 0xC3], false },
        { "an encoded surrogate", [0xED, 0xA0, 0x80], false },
        { "an overlong form", [0xC0, 0xAF], false },
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void AFileShowsItsLinesWhenItIsText(string name, byte[] content, bool text)
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.Add("f", content);
        using var pack = Pack(folder, writer);
        var entry = Entry(PackDump.ToText(pack), "f");
        Assert.True(entry.TryGetProperty("text"u8, out var lines) == text, name);
        if (text)
            Assert.Equal(Encoding.UTF8.GetString(content).Split('\n'), lines.EnumerateArray().Select(l => l.GetString()));
    }

    [Fact]
    public void AnEmptyPacksDump()
    {
        using var folder = new TempFolder();
        using var pack = Pack(folder, new PackWriter());
        Assert.Equal("{\n  \"format\": 1,\n  \"count\": 0,\n  \"entries\": []\n}\n", PackDump.ToText(pack));
    }
}
