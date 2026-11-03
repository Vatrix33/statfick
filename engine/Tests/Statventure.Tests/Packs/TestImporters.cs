using System.Text;
using Statventure.Packs;
using Statventure.Tests.Records;

// Importers of the pack tests (Docs/Assets.md, "Importers"; S3-3). The working ones are in
// Statventure.Tests.Packs.Importing; the deliberately broken declarations in
// Statventure.Tests.Packs.Importing.Broken, which the tests only name one by one.

namespace Statventure.Tests.Packs.Importing
{
    /// <summary>A ".lines" file: the record <c>&lt;stem&gt;</c> (a part named by its text) and the file <c>&lt;stem&gt;.upper</c> (its text in capitals).</summary>
    [Importer("test-lines", ".lines")]
    public sealed class LinesImporter : IImporter
    {
        public void Import(ImportContext context)
        {
            var text = Encoding.UTF8.GetString(context.Read());
            context.AddRecord(context.PathWithoutEnding, new Part { Name = text });
            context.Add(context.PathWithoutEnding + ".upper", Encoding.UTF8.GetBytes(text.ToUpperInvariant()));
        }
    }

    /// <summary>Any ".txt" file: the file <c>&lt;stem&gt;.seen</c> holding the ending that chose it and its text.</summary>
    [Importer("test-text", ".txt")]
    public sealed class TextImporter : IImporter
    {
        public void Import(ImportContext context) =>
            context.Add(context.PathWithoutEnding + ".seen", Encoding.UTF8.GetBytes($"{context.Ending}:{Encoding.UTF8.GetString(context.Read())}"));
    }

    /// <summary>
    /// A ".pair.txt" file (longer than ".txt", so it wins): the record <c>&lt;stem&gt;</c> made of the files
    /// <c>&lt;stem&gt;.left</c> and <c>&lt;stem&gt;.right</c> beside it, which aren't packed themselves then.
    /// </summary>
    [Importer("test-pair", ".pair.txt", ".pair")]
    public sealed class PairImporter : IImporter
    {
        public void Import(ImportContext context)
        {
            var left = Encoding.UTF8.GetString(context.ReadSource(context.PathWithoutEnding + ".left"));
            var right = File.ReadAllText(context.SourceFileOf(context.PathWithoutEnding + ".right"));
            context.AddRecord(context.PathWithoutEnding, typeof(Part), new Part { Name = left + right, Weight = context.Sources.Count });
        }
    }

    /// <summary>A ".reads" file names other files of the folder, one per line, and reads each; it adds the record <c>&lt;stem&gt;</c> of their lengths.</summary>
    [Importer("test-reader", ".reads")]
    public sealed class ReaderImporter : IImporter
    {
        public void Import(ImportContext context)
        {
            var total = 0;
            foreach (var path in Encoding.UTF8.GetString(context.Read()).Split('\n', StringSplitOptions.RemoveEmptyEntries))
                total += context.ReadSource(path).Length;
            context.AddRecord(context.PathWithoutEnding, new Part { Name = "read", Weight = total });
        }
    }

    /// <summary>Fails on every file.</summary>
    [Importer("test-failing", ".fail")]
    public sealed class FailingImporter : IImporter
    {
        public void Import(ImportContext context) => throw new InvalidDataException("this source can't be imported");
    }

    /// <summary>An <see cref="IImporter"/> without the attribute: not found as an importer.</summary>
    public sealed class UnmarkedHelper : IImporter
    {
        public void Import(ImportContext context) => throw new InvalidOperationException("not an importer");
    }

    /// <summary>Counts how many instances the packing of one folder makes.</summary>
    [Importer("test-counting", ".count")]
    public sealed class CountingImporter : IImporter
    {
        public static int Made;
        int _files;

        public CountingImporter() => Interlocked.Increment(ref Made);

        public void Import(ImportContext context) => context.Add(context.SourcePath + "ed", [(byte)++_files]);
    }
}

namespace Statventure.Tests.Packs.Importing.Broken
{
    [Importer("test-clash", ".txt")]
    public sealed class ClashImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-text", ".other")]
    public sealed class SameNameImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-json", ".json")]
    public sealed class JsonImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-upper", ".GLB")]
    public sealed class UppercaseImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-nodot", "glb")]
    public sealed class NoDotImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-dot", ".")]
    public sealed class DotImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-trailing", ".a.")]
    public sealed class TrailingDotImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-slash", ".a/b")]
    public sealed class SlashImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-empty", "")]
    public sealed class EmptyEndingImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-none")]
    public sealed class NoEndingImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("", ".nameless")]
    public sealed class NamelessImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }

    [Importer("test-not", ".not")]
    public sealed class NotAnImporter
    {
    }

    [Importer("test-ctor", ".ctor")]
    public sealed class ConstructorImporter(int value) : IImporter
    {
        public int Value => value;
        public void Import(ImportContext context) { }
    }

    [Importer("test-abstract", ".abstract")]
    public abstract class AbstractImporter : IImporter
    {
#pragma warning disable CA1012 // A public constructor, so only its being abstract refuses it.
        public AbstractImporter() { }
#pragma warning restore CA1012
        public void Import(ImportContext context) { }
    }

    public sealed class UnmarkedImporter : IImporter
    {
        public void Import(ImportContext context) { }
    }
}
