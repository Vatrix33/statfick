using System.Reflection;
using Statventure.Editor.Session;
using Statventure.UI.Dom;

namespace Statventure.Editor.Shell;

/// <summary>The shell's three panes (M11-6): the entry list left, the form in the middle, the preview right.</summary>
public enum EditorPane
{
    Entries,
    Form,
    Preview,
}

/// <summary>
/// A part of the editor (P2): the browser, the list, the form, checks, saving, import, the preview. The shell
/// finds every non-abstract subclass in the editor's assembly (no list to edit: each step adds its own folder),
/// makes one of each with its parameterless constructor and attaches them in order: by <see cref="Pane"/>
/// (none first), then <see cref="Order"/>, then the class's full name. A part with a pane gets its own element
/// in that pane (<c>&lt;div class="part"&gt;</c>, stacked in that order); it registers its commands in
/// <see cref="Attach"/>. Its stylesheet is any <c>.css</c> file in its folder (the shell adds them all).
/// </summary>
public abstract class EditorPart
{
    /// <summary>The session (the data). Set before <see cref="Attach"/>.</summary>
    public EditorSession Session { get; internal set; } = null!;

    /// <summary>The view (the document, commands, dialogs, status). Set before <see cref="Attach"/>.</summary>
    public EditorView View { get; internal set; } = null!;

    /// <summary>The pane the part shows in, or null when it has no element of its own (it opens dialogs, or acts through other parts).</summary>
    public virtual EditorPane? Pane => null;

    /// <summary>Its place among the parts of its pane (lower first).</summary>
    public virtual int Order => 0;

    /// <summary>
    /// Builds the part: <paramref name="element"/> is its own element in its pane (null without a pane); it
    /// registers its commands (<see cref="EditorView.Commands"/>) and its sources of unsaved work.
    /// </summary>
    protected internal abstract void Attach(UiElement? element);

    /// <summary>The parts' classes in <paramref name="assembly"/>: every non-abstract subclass of <see cref="EditorPart"/>.</summary>
    public static IReadOnlyList<Type> Find(Assembly assembly) =>
        [.. assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(EditorPart)) && !t.IsAbstract).OrderBy(t => t.FullName, StringComparer.Ordinal)];
}
