using System.Text;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs.Importing;
using Statventure.Tests.Records;
using Statventure.UI.Style;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// Updating a pack with a folder (<c>packer update</c>; Docs/Assets.md, "Later needs"; Docs/Editor.md, P30): the
/// folder's entries added or replacing the pack's, every other entry kept as it was stored; a stylesheet's urls
/// may name the pack's entries; what can't be kept stops it with the pack left as it was.
/// </summary>
public class PackUpdateTests
{
    static Importers TestImporters => Importers.Of(typeof(LinesImporter), typeof(TextImporter), typeof(PairImporter), typeof(ReaderImporter), typeof(FailingImporter));

    /// <summary>A pack of entries stored as the writer wouldn't store them now (an old record version raw, text at quality 5 and raw).</summary>
    static string OldPack(TempFolder folder)
    {
        var raw = new RawPack();
        raw.Raw("data/kept-old", RecordFixtures.Read("test.creature", "creature-1", ".rec"));
        raw.Entries[^1].Flags = 2;
        raw.Brotli("data/kept-q5", Prose(3000, 1), quality: 5);
        raw.Raw("data/kept-raw", Prose(2000, 2));
        raw.Raw("data/replaced-file", Prose(100, 3));
        raw.Raw("data/replaced-record", RecordSerializer.ToBinary(new Part { Name = "before" }));
        raw.Entries[^1].Flags = 2;
        raw.Raw("data/same", Prose(100, 4));
        raw.Raw("text/a.upper", "OLD"u8.ToArray());
        return folder.Write("p.pak", raw.Build());
    }

    static Dictionary<string, (PackEntryFlags Flags, string Hash, string Stored)> Stored(string file)
    {
        var bytes = File.ReadAllBytes(file);
        using var pack = PackFile.Open(file);
        return pack.Entries.ToDictionary(e => e.Path,
            e => (e.Flags, e.HashHex, Convert.ToHexStringLower(bytes.AsSpan((int)e.Offset, (int)e.StoredLength))), StringComparer.Ordinal);
    }

    [Fact]
    public void UpdateAddsAndReplacesAndKeepsTheRestAsItWasStored()
    {
        using var folder = new TempFolder();
        var pak = OldPack(folder);
        var before = Stored(pak);
        folder.Write("src/data/added", "new");
        folder.Write("src/data/replaced-file", "changed");
        // A file replaced by a record, and the content an entry already has.
        folder.Write("src/data/replaced-record.json", RecordSerializer.ToJson(new Part { Name = "after" }));
        folder.Write("src/data/same", Prose(100, 4));
        // An importer's entries: the record text/a and the file text/a.upper, which replaces the pack's.
        folder.Write("src/text/a.lines", "a horn");
        var update = PackWriter.Update(pak, folder["src"], TestTypes, TestImporters);
        Assert.Equal(["data/added", "text/a"], update.Added);
        Assert.Equal(["data/replaced-file", "data/replaced-record", "data/same", "text/a.upper"], update.Replaced);
        var after = Stored(pak);
        Assert.Equal(["data/added", "data/kept-old", "data/kept-q5", "data/kept-raw", "data/replaced-file", "data/replaced-record", "data/same", "text/a", "text/a.upper"],
            after.Keys.Order(StringComparer.Ordinal));
        foreach (var path in new[] { "data/kept-old", "data/kept-q5", "data/kept-raw", "data/same" })
            Assert.Equal(before[path], after[path]);
        // Three kept as they are, and data/same's content found in the pack.
        Assert.Equal((9, 4), (update.Written.Entries, update.Written.Reused));
        using var data = new VirtualFileSystem([Mount.Pack(pak)]);
        Assert.Equal("new", data.ReadText("data/added"));
        Assert.Equal("changed", data.ReadText("data/replaced-file"));
        Assert.Equal("after", data.Load<Part>("data/replaced-record").Name);
        Assert.Equal("a horn", data.Load<Part>("text/a").Name);
        Assert.Equal("A HORN", data.ReadText("text/a.upper"));
        using var pack = PackFile.Open(pak);
        Assert.Equal(RecordFixtures.Read("test.creature", "creature-1", ".rec"), pack.Read("data/kept-old"));

        // A record replaced by a file too; updating with an empty folder changes nothing.
        folder.Write("src2/data/replaced-record", "a file now");
        PackWriter.Update(pak, folder["src2"], TestTypes, TestImporters);
        using (var again = new VirtualFileSystem([Mount.Pack(pak)]))
            Assert.Equal("a file now", again.ReadText("data/replaced-record"));
        var bytes = File.ReadAllBytes(pak);
        Directory.CreateDirectory(folder["empty"]);
        var nothing = PackWriter.Update(pak, folder["empty"], TestTypes, TestImporters);
        Assert.Equal((0, 0, 9), (nothing.Added.Count, nothing.Replaced.Count, nothing.Written.Reused));
        Assert.Equal(bytes, File.ReadAllBytes(pak));
    }

    [Fact]
    public void AnUpdatedStylesheetMayNameThePacksEntries()
    {
        using var folder = new TempFolder();
        var pak = MakePack(folder["ui.pak"], ("ui/images/a.png", [1, 2, 3]), ("ui/fonts/a.ttf", [4]));
        folder.Write("src/ui/styles/game.css", """
            @font-face { font-family: A; src: url("ui/fonts/a.ttf") }
            .a { background: url(ui/images/a.png) } .b { background: url(ui/images/b.png) }
            """);
        folder.Write("src/ui/images/b.png", [5]);
        // Packing the folder alone, the pack's images aren't there.
        var alone = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["src"]));
        Assert.Contains("url(\"ui/images/a.png\") names no file in the data being packed", alone.Problem);
        var update = PackWriter.Update(pak, folder["src"]);
        Assert.Equal(["ui/images/b.png", "ui/styles/game"], update.Added);
        using (var data = new VirtualFileSystem([Mount.Pack(pak)]))
            Assert.Equal(2, Stylesheet.Load(data, "ui/styles/game").Rules.Count);
        // A url naming what neither has is still a problem, naming the stylesheet.
        var css = folder.Write("src/ui/styles/game.css", ".a { background: url(ui/images/gone.png) }");
        var bytes = File.ReadAllBytes(pak);
        var gone = Assert.Throws<DataFileException>(() => PackWriter.Update(pak, folder["src"]));
        Assert.Equal((css, "the ui importer: 1 problem: url(\"ui/images/gone.png\") names no file in the data being packed"), (gone.File, gone.Problem));
        Assert.Equal(bytes, File.ReadAllBytes(pak));
    }

    [Fact]
    public void UpdateNeedsAPackItCanRead()
    {
        using var folder = new TempFolder();
        folder.Write("src/a", "x");
        var missing = Assert.Throws<FileNotFoundException>(() => PackWriter.Update(folder["none.pak"], folder["src"]));
        Assert.Equal($"No pack {folder["none.pak"]} to update", missing.Message);
        Assert.False(File.Exists(folder["none.pak"]));
        var notAPack = folder.Write("bad.pak", "not a pack at all, longer than a pack's header is: sixty-four bytes"u8.ToArray());
        var refused = Assert.Throws<DataFileException>(() => PackWriter.Update(notAPack, folder["src"]));
        Assert.Equal((notAPack, "not a pack (it doesn't start with SVPK)"), (refused.File, refused.Problem));
        Assert.Throws<DirectoryNotFoundException>(() => PackWriter.Update(MakePack(folder["good.pak"], Entry("b", "y")), folder["nowhere"]));
    }

    [Fact]
    public void WhatCantBeKeptStopsItAndLeavesThePack()
    {
        using var folder = new TempFolder();
        var good = new RawPack().Raw("a", "file a"u8.ToArray()).Raw("m/damaged", "content"u8.ToArray()).Raw("x.json", "[1]"u8.ToArray()).Build();
        // m/damaged's stored bytes changed: its index (and hash) still match, its content doesn't.
        var damaged = (byte[])good.Clone();
        damaged[64 + "file a".Length] ^= 1;
        var pak = folder.Write("p.pak", damaged);

        // A damaged entry the folder doesn't replace.
        folder.Write("src/b", "b");
        var refused = Assert.Throws<DataFileException>(() => PackWriter.Update(pak, folder["src"], TestTypes, TestImporters));
        Assert.Equal((pak, "m/damaged", "the content doesn't match its hash (damaged)"), (refused.File, refused.Entry, refused.Problem));
        Assert.Equal(damaged, File.ReadAllBytes(pak));

        // Replaced, it isn't read; its content given again, the pack's stored bytes for it are damaged and not kept.
        folder.Write("src/m/damaged", "content");
        var update = PackWriter.Update(pak, folder["src"], TestTypes, TestImporters);
        Assert.Equal(["m/damaged"], update.Written.DamagedInOld);
        using (var data = new VirtualFileSystem([Mount.Pack(pak)]))
            Assert.Equal("content", data.ReadText("m/damaged"));
        var sound = File.ReadAllBytes(pak);

        // An entry the folder's entries leave no room for: a folder of one of them, a record beside its loose file.
        folder.Write("clash/a/b", "below a");
        var folderOf = Assert.Throws<DataFileException>(() => PackWriter.Update(pak, folder["clash"], TestTypes, TestImporters));
        Assert.Equal((pak, "a", "can't be kept beside the folder's entries: 'a' is already a folder of other entries (Parameter 'path')"),
            (folderOf.File, folderOf.Entry, folderOf.Problem));
        Assert.IsType<ArgumentException>(folderOf.InnerException);
        folder.Write("twin/x.json", RecordSerializer.ToJson(new Part { Name = "x" }));
        var twin = Assert.Throws<DataFileException>(() => PackWriter.Update(pak, folder["twin"], TestTypes, TestImporters));
        Assert.Equal((pak, "x.json"), (twin.File, twin.Entry));
        Assert.StartsWith("can't be kept beside the folder's entries: 'x.json' can't be a file beside the record 'x'", twin.Problem);
        // The folder's own problems name its file.
        var bad = folder.Write("badsrc/c.json", "{\"$types\": {\"test.nothing\": 1}}");
        Assert.Equal(bad, Assert.Throws<DataFileException>(() => PackWriter.Update(pak, folder["badsrc"], TestTypes, TestImporters)).File);
        Assert.Equal(sound, File.ReadAllBytes(pak));
        Assert.Empty(Directory.EnumerateFiles(folder.Root, "*.tmp"));
    }
}
