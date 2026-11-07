using Statventure.Editor.Form;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.UI.Dom;

namespace Statventure.Editor.References;

/// <summary>
/// The entries that refer to one being renamed or removed (M11-11, P19; Docs/Editor.md, "How it works", step 10), added
/// to the entry dialogs through <see cref="EntryActions.Asking"/>: the pack's entries (its own, new ones and the mod's
/// copies) naming it, listed, and an update in the same step as the rename or removal (each referrer's draft one commit
/// of its history). Rename: "Update them to the new name", ticked by default; names a reference's rule can't give the
/// new path are left. Remove: "Point them at:" one of the kind's other entries, or "Leave them" (the default); left ones
/// show their problems afterwards (P18). (The manager's decision Q-A, 2026-11-07.)
/// </summary>
public sealed class ReferrersPart : EditorPart
{
    /// <summary>The most referrers a dialog names (the rest counted).</summary>
    public const int Listed = 8;

    /// <summary>The value of the "Leave them" choice of a removal's select.</summary>
    public const string Leave = "";

    protected internal override void Attach(UiElement? element)
    {
        View.Part<EntryActions>().Asking += Asking;
    }

    void Asking(EntryDialog asking)
    {
        if (asking.Action is not (EntryAction.Rename or EntryAction.Remove) || asking.Entry is not { Kind: { } kind } target)
            return;
        var removing = asking.Action == EntryAction.Remove;
        var referrers = EntryReferences.To(Session, target).Where(r => !(removing && r.Entry == target)).ToList();
        if (referrers.Count == 0)
            return;
        var old = target.Path;
        var box = asking.Dialog.Content.Append("div", "referrers");
        box.Append("p", text: referrers.Count == 1 ? "1 entry refers to it:" : $"{referrers.Count} entries refer to it:");
        var list = box.Append("ul", "referrer-list");
        foreach (var (entry, _) in referrers.Take(Listed))
            list.Append("li", text: entry.Path);
        if (referrers.Count > Listed)
            list.Append("li", "more", $"and {referrers.Count - Listed} more");
        if (!removing)
        {
            var line = box.Append("label", "referrer-update");
            var update = line.Append("input");
            update.SetAttribute("type", "checkbox");
            update.Checked = true;
            line.Append("span", text: "Update them to the new name");
            asking.Confirmed.Add(path =>
            {
                if (update.Checked)
                    Update(referrers, path!, old);
            });
            return;
        }
        var choice = box.Append("label", "dialog-field referrer-target");
        choice.Append("span", "dialog-label", "Point them at:");
        var select = choice.Append("select");
        var leave = select.Append("option", text: "Leave them");
        leave.SetAttribute("value", Leave);
        // The kind's other entries, each by the name the first referrer's mark gives it (one its mark can't name isn't offered).
        var reference = referrers[0].References[0].Reference;
        foreach (var other in Session.Entries)
        {
            if (other == target || other.Kind?.Name != kind.Name || reference.NameOf(other.Path) is not { } name)
                continue;
            var option = select.Append("option", text: name);
            option.SetAttribute("value", other.Path);
        }
        asking.Confirmed.Add(_ =>
        {
            if (select.Value is { Length: > 0 } path)
                Update(referrers, path, old);
        });
    }

    /// <summary>Points every referrer's references at <paramref name="path"/> (one commit each) and says so in the status line.</summary>
    void Update(IReadOnlyList<(EditorEntry Entry, IReadOnlyList<ReferenceAt> References)> referrers, string path, string old)
    {
        var updated = 0;
        var left = 0;
        foreach (var (entry, references) in referrers)
        {
            var (moved, kept) = EntryReferences.PointAt(entry.Draft!, references, path);
            left += kept;
            if (Session.Commit(entry, moved))
                updated++;
        }
        View.Status = $"Pointed {(updated == 1 ? "1 entry" : $"{updated} entries")} that referred to {old} at {path}"
            + (left > 0 ? $"; {(left == 1 ? "1 name" : $"{left} names")} can't name it and stay as they were" : "");
    }
}
