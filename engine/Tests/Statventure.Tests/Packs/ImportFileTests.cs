using System.Text;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Packs.Importing;
using Statventure.Tests.Packs.SingleImporting;
using Statventure.Tests.Records;
using Statventure.Tests.Render3D;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs.SingleImporting
{
    /// <summary>A ".probe" file: the file <c>&lt;stem&gt;.seen</c> telling what its context gave, and whether the data has each path its source names (one per line).</summary>
    [Importer("test-probe", ".probe")]
    public sealed class ProbeImporter : IImporter
    {
        public void Import(ImportContext context)
        {
            var seen = new StringBuilder()
                .Append($"source {context.SourcePath}\nfile {context.SourceFile}\nending {context.Ending}\nstem {context.PathWithoutEnding}\n")
                .Append($"sources {string.Join(",", context.Sources)}\n");
            foreach (var path in Encoding.UTF8.GetString(context.Read()).Split('\n', StringSplitOptions.RemoveEmptyEntries))
                seen.Append($"exists {path} {context.Exists(path)}\n");
            context.Add(context.PathWithoutEnding + ".seen", Encoding.UTF8.GetBytes(seen.ToString()));
        }
    }
}

namespace Statventure.Tests.Packs
{
    /// <summary>
    /// One file imported on its own (Docs/Editor.md, "Packs for the editor", step 2; M11-8, P28): the same entries
    /// as packing the file in a folder at its source path (each of the engine's importers, a record's JSON form,
    /// a plain file), the files an importer reads besides found beside it on disk or through the caller's lookup
    /// (a terrain's images), and the data paths a stylesheet's urls name checked against the caller's lookup.
    /// </summary>
    public class ImportFileTests
    {
        static Importers TestImporters => Importers.Of(typeof(LinesImporter), typeof(TextImporter), typeof(PairImporter), typeof(ReaderImporter),
            typeof(FailingImporter), typeof(ProbeImporter));

        static string Describe(IEnumerable<(string Path, bool Record, byte[] Content)> entries) =>
            string.Join("\n", entries.Select(e => $"{e.Path} {(e.Record ? "record" : "file")} {Convert.ToHexStringLower(e.Content)}"));

        static string Describe(IReadOnlyList<ImportedEntry> entries) => Describe(entries.Select(e => (e.Path, e.IsRecord, e.Content)));

        /// <summary>The entries of <paramref name="folder"/> packed by AddFolder, those whose paths <paramref name="keep"/> accepts.</summary>
        static string Packed(TempFolder scratch, string folder, Func<string, bool>? keep = null, RecordTypes? types = null, Importers? importers = null)
        {
            var writer = new PackWriter();
            writer.AddFolder(scratch[folder], types, importers);
            using var pack = PackFile.Open(scratch.Write(folder + ".pak", Bytes(writer)));
            return Describe(pack.Entries.Where(e => keep?.Invoke(e.Path) != false).Select(e => (e.Path, e.IsRecord, pack.Read(e))));
        }

        [Fact]
        public void AModelImportedEqualsTheSameFilePacked()
        {
            using var folder = new TempFolder();
            foreach (var name in new[] { "blocks", "figure" })
            {
                // The file on disk under any name; the data path chosen.
                var file = folder.Write($"downloads/{name} (Final).GLB", File.ReadAllBytes(TestModelPack.SourceOf(name)));
                folder.Write($"src-{name}/models/stages/{name}.glb", File.ReadAllBytes(TestModelPack.SourceOf(name)));
                var imported = PackWriter.ImportFile(file, $"models/stages/{name}.glb");
                Assert.Equal($"models/stages/{name}", Assert.Single(imported).Path);
                Assert.True(imported[0].IsRecord);
                Assert.Equal(Packed(folder, $"src-{name}"), Describe(imported));
            }
            // A model that doesn't import names the file, as packing it does.
            var broken = folder.Write("downloads/broken.glb", File.ReadAllBytes(TestModelPack.SourceOf("blocks"))[..100]);
            folder.Write("src-broken/models/broken.glb", File.ReadAllBytes(broken));
            var problem = Assert.Throws<DataFileException>(() => PackWriter.ImportFile(broken, "models/broken.glb"));
            var packing = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["src-broken"]));
            Assert.Equal((broken, null), (problem.File, problem.Entry));
            Assert.Equal(packing.Problem.Replace(folder["src-broken/models/broken.glb"], broken), problem.Problem);
            Assert.StartsWith("the models importer: ", problem.Problem);
        }

        [Fact]
        public void ATerrainImportedTakesItsImagesFromBesideIt()
        {
            using var folder = new TempFolder();
            // Its images beside it on disk, and in a folder below it.
            TestTerrainPack.Copy(folder, "downloads", "My Vale");
            TestTerrainPack.Copy(folder, "src/world/terrain", "vale");
            var imported = PackWriter.ImportFile(folder["downloads/My Vale.terrain.json"], "world/terrain/vale.terrain.json");
            Assert.Equal(Packed(folder, "src", path => !path.EndsWith(".png", StringComparison.Ordinal)), Describe(imported));
            Assert.Equal("world/terrain/vale", Assert.Single(imported).Path);
            using (var data = new VirtualFileSystem([Mount.Custom("imported", new MemoryEntries().Record(imported[0].Path, imported[0].Content))]))
                RecordAssert.Equal(TerrainData.Load(TestTerrainPack.Description), TerrainData.Load(data, "world/terrain/vale"));

            TestTerrainPack.Copy(folder, "deep/maps", "unused");
            File.Delete(folder["deep/maps/unused.terrain.json"]);
            var below = TestTerrainPack.DescriptionObject();
            below["height"] = "maps/test-height.png";
            below["cover"] = "maps/test-cover.png";
            var deep = folder.Write("deep/deep.terrain.json", below.ToJsonString());
            Assert.Equal(Describe(imported).Replace("world/terrain/vale", "world/deep"), Describe(PackWriter.ImportFile(deep, "world/deep.terrain.json")));
            // At the top of the data, every path is below its folder.
            Assert.Equal("top", Assert.Single(PackWriter.ImportFile(deep, "top.terrain.json")).Path);

            // An image that isn't beside it, or that is outside the source path's folder, isn't found.
            File.Delete(folder["downloads/test-cover.png"]);
            var missing = Assert.Throws<DataFileException>(() => PackWriter.ImportFile(folder["downloads/My Vale.terrain.json"], "world/terrain/vale.terrain.json"));
            Assert.Equal((folder["downloads/My Vale.terrain.json"], "the world importer: No 'world/terrain/test-cover.png' among the files the import can read"),
                (missing.File, missing.Problem));
            var description = TestTerrainPack.DescriptionObject();
            description["cover"] = "../maps/test-cover.png";
            var outside = folder.Write("deep/sub/outside.terrain.json", description.ToJsonString());
            TestTerrainPack.Copy(folder, "deep/sub", "unused");
            File.Delete(folder["deep/sub/test-cover.png"]);
            Assert.StartsWith("the world importer: 'world/sub/../maps/test-cover.png' isn't a valid data path",
                Assert.Throws<DataFileException>(() => PackWriter.ImportFile(outside, "world/sub/outside.terrain.json")).Problem);
        }

        [Fact]
        public void TheCallerCanSayWhereTheFilesAnImporterReadsAre()
        {
            using var folder = new TempFolder();
            TestTerrainPack.Copy(folder, "elsewhere", "unused");
            var description = folder.Write("downloads/vale.terrain.json", File.ReadAllBytes(TestTerrainPack.Description));
            var asked = new List<string>();
            var sources = new ImportSources
            {
                FileOf = path =>
                {
                    asked.Add(path);
                    return path.StartsWith("world/terrain/", StringComparison.Ordinal) ? folder["elsewhere/" + path["world/terrain/".Length..]] : null;
                },
            };
            var imported = PackWriter.ImportFile(description, "world/terrain/vale.terrain.json", sources);
            Assert.Equal(["world/terrain/test-height.png", "world/terrain/test-cover.png"], asked);
            using var data = new VirtualFileSystem([Mount.Custom("imported", new MemoryEntries().Record(imported[0].Path, imported[0].Content))]);
            RecordAssert.Equal(TerrainData.Load(TestTerrainPack.Description), TerrainData.Load(data, "world/terrain/vale"));
            // Its lookup's null is a file not there.
            Assert.Equal("the world importer: No 'world/terrain/test-height.png' among the files the import can read",
                Assert.Throws<DataFileException>(() => PackWriter.ImportFile(description, "world/terrain/vale.terrain.json", new ImportSources { FileOf = _ => null })).Problem);
        }

        const string Sheet = """
            @font-face { font-family: A; src: url("ui/fonts/a.ttf") }
            .a { background: url(ui/images/a.png) no-repeat; cursor: url("data:image/png;base64,AA=="), auto }
            .b { --icon: url("ui/images/b.png") }
            """;

        [Fact]
        public void AStylesheetsUrlsAreCheckedAgainstTheGivenPaths()
        {
            using var folder = new TempFolder();
            var css = folder.Write("downloads/menu.css", Sheet);
            folder.Write("src/ui/styles/menu.css", Sheet);
            string[] files = ["ui/fonts/a.ttf", "ui/images/a.png", "ui/images/b.png"];
            foreach (var path in files)
                folder.Write("src/" + path, [1]);
            var asked = new List<string>();
            var imported = PackWriter.ImportFile(css, "ui/styles/menu.css", new ImportSources
            {
                Exists = path =>
                {
                    asked.Add(path);
                    return files.Contains(path);
                },
            });
            Assert.Equal(files, asked.Order(StringComparer.Ordinal));
            Assert.Equal(Packed(folder, "src", path => path == "ui/styles/menu"), Describe(imported));

            // Without a lookup no path exists; with one that lacks a url, that url is the problem.
            var none = Assert.Throws<DataFileException>(() => PackWriter.ImportFile(css, "ui/styles/menu.css"));
            Assert.Equal(css, none.File);
            Assert.StartsWith("the ui importer: 3 problems: ", none.Problem);
            foreach (var path in files)
                Assert.Contains($"url(\"{path}\") names no file in the data being packed", none.Problem);
            Assert.Equal("the ui importer: 1 problem: url(\"ui/images/b.png\") names no file in the data being packed",
                Assert.Throws<DataFileException>(() => PackWriter.ImportFile(css, "ui/styles/menu.css",
                    new ImportSources { Exists = path => path != "ui/images/b.png" })).Problem);
            // Files beside it on disk aren't the data: they aren't imported with it.
            folder.Write("downloads/ui/images/a.png", [1]);
            Assert.Contains("url(\"ui/images/a.png\") names no file", Assert.Throws<DataFileException>(() => PackWriter.ImportFile(css, "ui/styles/menu.css")).Problem);
        }

        [Fact]
        public void ARecordsJsonFormAndAnyOtherFileImportAsPackingThemDoes()
        {
            using var folder = new TempFolder();
            var ogre = folder.Write("downloads/Ogre.json", RecordSerializer.ToJson(LooseRecordTests.Ogre));
            var old = folder.Write("downloads/old.json", RecordFixtures.Read("test.creature", "creature-1", ".json"));
            var plain = folder.Write("downloads/plain.json", "{\"name\": \"a file\"}");
            var font = folder.Write("downloads/Font.TTF", Noise(64));
            var dotTxt = folder.Write("downloads/dot.txt", "only the ending");
            folder.Write("src/data/ogre.json", File.ReadAllBytes(ogre));
            folder.Write("src/data/old.json", File.ReadAllBytes(old));
            folder.Write("src/data/plain.json", File.ReadAllBytes(plain));
            folder.Write("src/ui/fonts/a.ttf", File.ReadAllBytes(font));
            folder.Write("src/f/.txt", File.ReadAllBytes(dotTxt));
            var all = new List<ImportedEntry>();
            foreach (var (file, path) in new[] { (ogre, "data/ogre.json"), (old, "data/old.json"), (plain, "data/plain.json"), (font, "ui/fonts/a.ttf"), (dotTxt, "f/.txt") })
                all.AddRange(PackWriter.ImportFile(file, path, types: TestTypes, importers: TestImporters));
            Assert.Equal(Packed(folder, "src", types: TestTypes, importers: TestImporters), Describe(all.OrderBy(e => e.Path, StringComparer.Ordinal).ToList()));
            Assert.Equal([("data/ogre", true), ("data/old", true), ("data/plain.json", false), ("ui/fonts/a.ttf", false), ("f/.txt", false)],
                all.Select(e => (e.Path, e.IsRecord)));
            // The older version comes in the current versions, as packing gives it.
            Assert.Equal(RecordSerializer.ToBinary(RecordSerializer.FromJson<Creature>(RecordFixtures.Read("test.creature", "creature-1", ".json"))), all[1].Content);

            // The engine's types by default; problems name the file.
            var unknown = Assert.Throws<DataFileException>(() => PackWriter.ImportFile(ogre, "data/ogre.json"));
            Assert.Equal((ogre, "a record of type test.creature, which isn't a record type known here"), (unknown.File, unknown.Problem));
            var cut = folder.Write("downloads/cut.json", "{\"$types\": {\"test.creature\"");
            Assert.Equal(cut, Assert.Throws<DataFileException>(() => PackWriter.ImportFile(cut, "data/cut.json", types: TestTypes)).File);
            Assert.Equal("a record's JSON form, but '' isn't a path under the rule",
                Assert.Throws<DataFileException>(() => PackWriter.ImportFile(ogre, ".json", types: TestTypes)).Problem);
            Assert.Throws<ArgumentException>(() => PackWriter.ImportFile(ogre, "Data/ogre.json", types: TestTypes));
            Assert.Throws<ArgumentException>(() => PackWriter.ImportFile(ogre, "data/Ogre.lines", importers: TestImporters));
            // A file that isn't there, whether an importer would take it or not.
            Assert.Equal($"No file {folder["downloads/gone.json"]}",
                Assert.Throws<FileNotFoundException>(() => PackWriter.ImportFile(folder["downloads/gone.json"], "data/gone.json", types: TestTypes)).Message);
            Assert.Equal($"No file {folder["downloads/gone.lines"]}",
                Assert.Throws<FileNotFoundException>(() => PackWriter.ImportFile(folder["downloads/gone.lines"], "data/gone.lines", importers: TestImporters)).Message);
        }

        [Fact]
        public void AnImporterSeesItsOwnFileAndTheCallersLookups()
        {
            using var folder = new TempFolder();
            var probe = folder.Write("downloads/Probe File.probe", "ui/a.png\nui/b.png\n");
            var seen = PackWriter.ImportFile(probe, "x/y.probe", new ImportSources { Exists = path => path == "ui/a.png" }, importers: TestImporters);
            Assert.Equal(($"source x/y.probe\nfile {probe}\nending .probe\nstem x/y\nsources x/y.probe\nexists ui/a.png True\nexists ui/b.png False\n"),
                Encoding.UTF8.GetString(Assert.Single(seen, e => e.Path == "x/y.seen").Content));
            // Packing a folder, its files are the data.
            folder.Write("src/x/y.probe", "ui/a.png\nui/b.png\n");
            folder.Write("src/ui/b.png", [1]);
            var packed = new PackWriter();
            packed.AddFolder(folder["src"], TestTypes, TestImporters);
            using var pack = PackFile.Open(folder.Write("src.pak", Bytes(packed)));
            Assert.Equal($"source x/y.probe\nfile {folder["src/x/y.probe"]}\nending .probe\nstem x/y\nsources ui/b.png,x/y.probe\nexists ui/a.png False\nexists ui/b.png True\n",
                Encoding.UTF8.GetString(pack.Read("x/y.seen")));
            // A path outside the rule is the importer's error.
            var bad = folder.Write("downloads/bad.probe", "UI/a.png");
            Assert.StartsWith("the test-probe importer: 'UI/a.png' isn't a valid data path",
                Assert.Throws<DataFileException>(() => PackWriter.ImportFile(bad, "x/bad.probe", importers: TestImporters)).Problem);
        }

        [Fact]
        public void AnImporterReadsTheFilesBesideItButNotAnotherImportersSources()
        {
            using var folder = new TempFolder();
            // Several entries from one file.
            var lines = folder.Write("downloads/horn.lines", "a horn");
            folder.Write("src/a/horn.lines", "a horn");
            Assert.Equal(Packed(folder, "src", types: TestTypes, importers: TestImporters),
                Describe(PackWriter.ImportFile(lines, "a/horn.lines", importers: TestImporters)));

            // Files beside it read by their data paths, whatever the source file's own name.
            var pair = folder.Write("downloads/c/Twins Source.pair.txt", "ignored");
            folder.Write("downloads/c/twins.left", "lef");
            folder.Write("downloads/c/twins.right", "tie");
            var twins = Assert.Single(PackWriter.ImportFile(pair, "c/twins.pair.txt", importers: TestImporters));
            Assert.Equal(("c/twins", true), (twins.Path, twins.IsRecord));
            // Its sources are its own path only.
            Assert.Equal(new Part { Name = "leftie", Weight = 1 }, RecordSerializer.FromBinary<Part>(twins.Content));

            // A reader of paths below its folder (its own file by its data path, whatever its name on disk), and
            // not above it or beside its folder.
            var reads = folder.Write("downloads/r/The Sum.reads", "data/one\ndata/two\nsum.reads\n");
            folder.Write("downloads/r/data/one", Noise(5));
            folder.Write("downloads/r/data/two", Noise(7));
            Assert.Equal(40, RecordSerializer.FromBinary<Part>(Assert.Single(PackWriter.ImportFile(reads, "sum.reads", importers: TestImporters)).Content).Weight);
            var nested = folder.Write("downloads/r/n/sum.reads", "n/one\n");
            folder.Write("downloads/r/n/one", Noise(3));
            Assert.Equal(3, RecordSerializer.FromBinary<Part>(Assert.Single(PackWriter.ImportFile(nested, "n/sum.reads", importers: TestImporters)).Content).Weight);
            Assert.Equal("the test-reader importer: No 'data/one' among the files the import can read",
                Assert.Throws<DataFileException>(() => PackWriter.ImportFile(folder.Write("downloads/r/n/up.reads", "data/one"), "n/up.reads", importers: TestImporters)).Problem);
            Assert.Equal("the test-reader importer: No 'm/one' among the files the import can read",
                Assert.Throws<DataFileException>(() => PackWriter.ImportFile(folder.Write("downloads/r/n/side.reads", "m/one"), "n/side.reads", importers: TestImporters)).Problem);
            // Another importer's source can't be read, as packing a folder refuses it.
            var taken = folder.Write("downloads/t/sum.reads", "data/note.txt");
            folder.Write("downloads/t/data/note.txt", "taken");
            Assert.Equal("the test-reader importer: 'data/note.txt' is a source of the test-text importer, so the test-reader importer can't use it too",
                Assert.Throws<DataFileException>(() => PackWriter.ImportFile(taken, "sum.reads", importers: TestImporters)).Problem);

            // An importer's failure names the file.
            var fail = folder.Write("downloads/bad.fail", "x");
            var failed = Assert.Throws<DataFileException>(() => PackWriter.ImportFile(fail, "bad.fail", importers: TestImporters));
            Assert.Equal((fail, null, "the test-failing importer: this source can't be imported"), (failed.File, failed.Entry, failed.Problem));
            Assert.IsType<InvalidDataException>(failed.InnerException);
        }
    }
}
