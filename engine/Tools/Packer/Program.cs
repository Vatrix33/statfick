// The packer (Docs/Assets.md, "Packs" and "The packer"; update: "Packs for the editor"): the engine's pack
// functions from the command line. Record types and importers are the engine's own.
using Statventure.Packs;

const string Usage = """
    Usage:
      packer pack <folder> <file.pak>      every file under <folder> into <file.pak>: files an importer takes (by the
                                           ending of their names) through it, <path>.json files holding a record's JSON
                                           form as the record <path>, the rest as they are; an existing <file.pak>'s
                                           unchanged entries keep their stored bytes
      packer update <folder> <file.pak>    the existing <file.pak> with <folder>'s entries (packed as pack packs them)
                                           added or replacing its entries at their paths; its other entries kept as
                                           they are, stored bytes and all; a stylesheet's urls may name its entries
      packer list <file.pak>               the entries: path, record or file, content and stored length, raw or
                                           compressed, SHA-256
      packer verify <file.pak>             reads every entry and checks its hash; exit code 1 when one is damaged
      packer extract <file.pak> <folder>   every entry in its loose form under <folder> (made if missing; it must be
                                           empty): files as they are, records as <path>.json in their JSON form
      packer dump <file.pak>               the pack as JSON text for review (what git's diff driver for packs shows)
    """;

try
{
    switch (args)
    {
        case ["pack", var folder, var pak]:
        {
            var writer = new PackWriter();
            writer.AddFolder(folder);
            var result = writer.Write(pak);
            if (result.OldProblem is not null)
                Console.Error.WriteLine($"Not reusing the old pack: {result.OldProblem}");
            foreach (var damaged in result.DamagedInOld)
                Console.Error.WriteLine($"Not reusing the old pack's damaged entry '{damaged}'");
            Console.WriteLine($"{Path.GetFullPath(pak)}: {result.Entries} entries, {result.Compressed} compressed, {result.Reused} kept from the old pack");
            return 0;
        }
        case ["update", var folder, var pak]:
        {
            var update = PackWriter.Update(pak, folder);
            var result = update.Written;
            foreach (var damaged in result.DamagedInOld)
                Console.Error.WriteLine($"Not reusing the old pack's damaged entry '{damaged}'");
            Console.WriteLine($"{Path.GetFullPath(pak)}: {result.Entries} entries, {update.Added.Count} added, {update.Replaced.Count} replaced, " +
                $"{result.Compressed} compressed, {result.Reused} kept from the old pack");
            return 0;
        }
        case ["list", var pak]:
        {
            using var pack = PackFile.Open(pak);
            foreach (var entry in pack.Entries)
                Console.WriteLine($"{entry.Path}\t{(entry.IsRecord ? "record" : "file")}\t{entry.ContentLength}\t{entry.StoredLength}\t{(entry.Compressed ? "brotli" : "raw")}\t{entry.HashHex}");
            return 0;
        }
        case ["verify", var pak]:
        {
            using var pack = PackFile.Open(pak);
            var problems = pack.Verify();
            foreach (var problem in problems)
                Console.Error.WriteLine(problem.Message);
            Console.WriteLine(problems.Count == 0
                ? $"{pack.FilePath}: all {pack.Entries.Count} entries sound"
                : $"{pack.FilePath}: {problems.Count} of {pack.Entries.Count} entries damaged");
            return problems.Count == 0 ? 0 : 1;
        }
        case ["extract", var pak, var folder]:
        {
            using var pack = PackFile.Open(pak);
            pack.ExtractTo(folder);
            Console.WriteLine($"{pack.Entries.Count} entries into {Path.GetFullPath(folder)}");
            return 0;
        }
        case ["dump", var pak]:
        {
            using var pack = PackFile.Open(pak);
            using var output = Console.OpenStandardOutput();
            output.Write(PackDump.ToUtf8(pack));
            return 0;
        }
        default:
            Console.Error.WriteLine(Usage);
            return 2;
    }
}
catch (Exception problem) when (problem is IOException or ArgumentException or UnauthorizedAccessException or InvalidOperationException)
{
    Console.Error.WriteLine(problem.Message);
    return 1;
}
