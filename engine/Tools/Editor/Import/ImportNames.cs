using System.Globalization;
using System.Text;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Editor.Import;

/// <summary>
/// The path an import suggests (P28; Docs/Editor.md, "Import (step 12)"): the kind's folder and the file's name made to
/// fit the path rule. A record goes into its kind's folder by the kind's name rule (P9), the file's name tried as it is
/// first, so a theme exported as <c>combat@alder-vale.json</c> comes back to its own path, then made to fit; a kind
/// without a folder at the top of the data, except the mod's info, which has one place (<see cref="ModInfo.Path"/>);
/// a file by its ending (the manager's decision of 2026-11-07): fonts in <c>ui/fonts</c> (as <c>ui.pak</c> holds them),
/// images in <c>ui/images</c> (where a stylesheet's urls point), texts and anything else at the top.
/// </summary>
public static class ImportNames
{
    /// <summary>A name used when nothing of the file's name fits the path rule.</summary>
    public const string Fallback = "imported";

    /// <summary>The longest name made from a file's name (the path rule allows 255 characters for the whole path).</summary>
    public const int MaxLength = 120;

    /// <summary>The folder a file (not a record) is suggested in, by its ending (lower case, with its dot): "" for the top.</summary>
    public static string FolderOf(string ending) => ending switch
    {
        ".ttf" or ".otf" => "ui/fonts",
        ".png" or ".svg" => "ui/images",
        _ => "",
    };

    /// <summary>
    /// <paramref name="name"/> made to fit the path rule as one name: accents dropped, small letters, every character the
    /// rule doesn't take (spaces, brackets, <c>@</c>, letters outside a–z) made a <c>-</c>, runs of <c>-</c> made one,
    /// <c>-</c> and dots taken off both ends, at most <see cref="MaxLength"/> characters, and a Windows device name
    /// (<c>con</c>, <c>aux.txt</c>) given <c>-1</c> after it; <see cref="Fallback"/> when nothing is left.
    /// </summary>
    public static string Fit(string name)
    {
        var text = new StringBuilder(name.Length);
        foreach (var c in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            var small = char.ToLowerInvariant(c);
            var kept = small is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '.' or '-' ? small : '-';
            if (kept == '-' && text.Length > 0 && text[^1] == '-')
                continue;
            text.Append(kept);
        }
        var fitted = text.ToString().Trim('-', '.');
        if (fitted.Length > MaxLength)
            fitted = fitted[..MaxLength].TrimEnd('-', '.');
        if (fitted.Length == 0)
            return Fallback;
        if (PackPath.Problem(fitted) is not null)
        {
            // Only a device name is left for the rule to refuse (alone or before the first dot).
            var dot = fitted.IndexOf('.');
            fitted = dot < 0 ? fitted + "-1" : fitted[..dot] + "-1" + fitted[dot..];
        }
        return fitted;
    }

    /// <summary>
    /// The path suggested for a record of <paramref name="kind"/> imported from a file whose name without its ending is
    /// <paramref name="stem"/>: in the kind's folder by its rule (the name as it is, else fitted), the mod's info at
    /// <c>mod/info</c>, a kind without a folder (or one whose rule takes neither) the fitted name at the top.
    /// </summary>
    public static string ForRecord(RecordShape kind, string stem)
    {
        if (kind.Type == typeof(ModInfo))
            return ModInfo.Path;
        var fitted = Fit(stem);
        return kind.PathOf(stem) ?? kind.PathOf(fitted) ?? fitted;
    }

    /// <summary>The path suggested for a file (not a record) named <paramref name="stem"/> and <paramref name="ending"/> (lower case): its ending's folder.</summary>
    public static string ForFile(string stem, string ending)
    {
        var folder = FolderOf(ending);
        var name = Fit(stem) + ending;
        return folder.Length == 0 ? name : folder + "/" + name;
    }
}
