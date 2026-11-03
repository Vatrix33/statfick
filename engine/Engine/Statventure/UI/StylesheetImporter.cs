using System.Text;
using Statventure.Core;
using Statventure.Packs;
using Statventure.UI.Css;
using Statventure.UI.Style;

namespace Statventure.UI;

/// <summary>
/// The UI importer (Docs/Assets.md, "Importers"; Q23, S3-3, S7-1–S7-3): packing a folder, each stylesheet
/// (<c>.css</c>, UTF-8) becomes one record entry at its path without the ending
/// (<c>ui/styles/game.css</c> → <c>ui/styles/game</c>) holding its compiled form
/// (<see cref="Stylesheet.Compile"/>), checked: a rule the engine can't parse, an at-rule it doesn't
/// handle, an unknown property, a value it can't parse, a @font-face it can't honour, or a url() that
/// isn't a data: url or a file of the folder by its data path stops the packing, every problem named.
/// Rules Chromium drops too (other browsers' selectors) are dropped and logged. Fonts and images are files
/// the packer packs as they are (<c>ui/fonts/…</c>, <c>ui/images/…</c>): this importer takes only
/// <c>.css</c> (step 8's terrain importer reads its PNG maps as its own sources).
/// </summary>
[Importer("ui", ".css")]
public sealed class StylesheetImporter : IImporter
{
    const int ProblemsShown = 20;

    static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public void Import(ImportContext context)
    {
        var bytes = context.Read();
        var start = bytes.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? 3 : 0;
        string text;
        try
        {
            text = Utf8.GetString(bytes, start, bytes.Length - start);
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidDataException("the stylesheet isn't UTF-8 text");
        }
        var sources = context.Sources.ToHashSet(StringComparer.Ordinal);
        var compiled = Stylesheet.Compile(text, sources.Contains);
        if (compiled.Problems.Count > 0)
        {
            var shown = compiled.Problems.Take(ProblemsShown).ToList();
            if (compiled.Problems.Count > ProblemsShown)
                shown.Add($"and {compiled.Problems.Count - ProblemsShown} more");
            throw new InvalidDataException($"{compiled.Problems.Count} problem{(compiled.Problems.Count == 1 ? "" : "s")}: {string.Join("; ", shown)}");
        }
        foreach (var rule in compiled.Dropped)
            Log.Info("Data", $"{context.SourcePath}: dropped, as Chromium drops it (another browser's selector): {rule}");
        context.AddRecord(context.PathWithoutEnding, compiled.Record);
    }
}
