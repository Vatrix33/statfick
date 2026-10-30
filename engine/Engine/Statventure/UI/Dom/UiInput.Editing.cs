using System.Numerics;
using Statventure.Text;
using Statventure.UI.Paint;

namespace Statventure.UI.Dom;

/// <summary>
/// Selecting in text fields with the pointer (Blink's SelectionController): a press puts the caret
/// at the nearest caret stop (a press inside the selection waits for the release, so a drag could
/// start there); Shift extends; a double click selects the word there (or the run of spaces),
/// a triple click the whole field's paragraph; dragging extends by what the press selected
/// (characters, words, paragraphs), and the field scrolls to keep the moving end in view.
/// </summary>
public sealed partial class UiInput
{
    enum Granularity { Character, Word, Paragraph }

    UiElement? _selectField;
    Granularity _selectGranularity;
    int _selectBaseStart, _selectBaseEnd;
    int? _collapseOnRelease;

    FieldGeometry? GeometryOf(UiElement field)
    {
        _document.UpdateLayout();
        return field.Box is { } box ? FieldGeometry.Of(_document, box, field) : null;
    }

    /// <summary>The caret stop under a point in a field (the point taken into the field's own space).</summary>
    int FieldOffsetAt(UiElement field, FieldGeometry geometry, Vector2 position) => geometry.OffsetAt(LocalPoint(field, position));

    /// <summary>A main-button press reached a text field (after focus): the selection starts there.</summary>
    void FieldPress(UiElement field, Vector2 position, int clicks, bool shift, bool wasFocused)
    {
        if (GeometryOf(field) is not { } geometry)
            return;
        var text = field.EditorText;
        var offset = FieldOffsetAt(field, geometry, position);
        _selectField = field;
        _collapseOnRelease = null;
        _document.CloseTyping();
        var (start, end, _) = field.EditorSelection;
        switch (clicks)
        {
            case 1:
            {
                _selectGranularity = Granularity.Character;
                if (shift && wasFocused)
                {
                    // Shift extends from the selection's anchor (the end away from the press).
                    var (anchor, _) = field.EditorAnchorAndFocus;
                    if (start != end)
                        anchor = offset <= start ? end : offset >= end ? start : anchor;
                    _selectBaseStart = _selectBaseEnd = anchor;
                    field.SetEditorSelection(Math.Min(anchor, offset), Math.Max(anchor, offset), offset < anchor, byUser: false, reveal: SelectionReveal.IfNeeded);
                    return;
                }
                _selectBaseStart = _selectBaseEnd = offset;
                // A single press on the selection keeps it until the release (FrameSelection::Contains:
                // the position under the press lies in the selection, both ends included).
                if (wasFocused && start != end && start <= offset && offset <= end)
                {
                    _collapseOnRelease = offset;
                    return;
                }
                field.SetEditorSelection(offset, offset, false, byUser: false, reveal: SelectionReveal.IfNeeded);
                return;
            }
            case 2:
            {
                _selectGranularity = Granularity.Word;
                var (wordStart, wordEnd) = WordAround(text, offset);
                _selectBaseStart = wordStart;
                _selectBaseEnd = wordEnd;
                field.SetEditorSelection(wordStart, wordEnd, false, byUser: false, reveal: SelectionReveal.IfNeeded);
                return;
            }
            default:
            {
                _selectGranularity = Granularity.Paragraph;
                var (paragraphStart, paragraphEnd) = ParagraphAround(field, text, offset);
                _selectBaseStart = paragraphStart;
                _selectBaseEnd = paragraphEnd;
                field.SetEditorSelection(paragraphStart, paragraphEnd, false, byUser: false, reveal: SelectionReveal.IfNeeded);
                return;
            }
        }
    }

    /// <summary>
    /// The word a collapsed position expands to (GranularityAdjuster, kWord): the word it is in,
    /// the one after when on a boundary, the one before at the end of the text.
    /// </summary>
    static (int Start, int End) WordAround(string text, int offset)
    {
        var atEnd = offset >= text.Length;
        var start = TextEditing.StartOfWord(text, offset, nextWordIfOnBoundary: !atEnd);
        var endOfParagraph = offset >= text.Length || text[offset] == '\n';
        var end = endOfParagraph ? offset : TextEditing.EndOfWord(text, offset, previousWordIfOnBoundary: atEnd);
        return (Math.Min(start, end), Math.Max(start, end));
    }

    /// <summary>A paragraph (a textarea's text between line breaks; all of a one-line field's).</summary>
    static (int Start, int End) ParagraphAround(UiElement field, string text, int offset)
    {
        if (field.Tag != "textarea")
            return (0, text.Length);
        var start = offset <= 0 ? 0 : text.LastIndexOf('\n', Math.Min(offset, text.Length) - 1) + 1;
        var next = text.IndexOf('\n', Math.Min(offset, text.Length));
        return (start, next < 0 ? text.Length : next);
    }

    /// <summary>The pointer moved with the main button held after a press in a field: the selection follows.</summary>
    void FieldDrag(Vector2 position)
    {
        if (_selectField is not { } field || !InDocument(field) || GeometryOf(field) is not { } geometry)
            return;
        var text = field.EditorText;
        var offset = FieldOffsetAt(field, geometry, position);
        _collapseOnRelease = null;
        int start, end;
        bool backward;
        switch (_selectGranularity)
        {
            case Granularity.Word:
            {
                var (wordStart, wordEnd) = WordAround(text, offset);
                if (offset < _selectBaseStart)
                    (start, end, backward) = (wordStart, _selectBaseEnd, true);
                else
                    (start, end, backward) = (_selectBaseStart, Math.Max(wordEnd, _selectBaseEnd), false);
                break;
            }
            case Granularity.Paragraph:
            {
                var (paragraphStart, paragraphEnd) = ParagraphAround(field, text, offset);
                if (offset < _selectBaseStart)
                    (start, end, backward) = (paragraphStart, _selectBaseEnd, true);
                else
                    (start, end, backward) = (_selectBaseStart, Math.Max(paragraphEnd, _selectBaseEnd), false);
                break;
            }
            default:
                (start, end, backward) = offset < _selectBaseStart ? (offset, _selectBaseStart, true) : (_selectBaseStart, offset, false);
                break;
        }
        field.SetEditorSelection(start, end, backward, byUser: false, reveal: SelectionReveal.IfNeeded);
    }

    /// <summary>
    /// The main button went up: a press inside the selection that didn't drag puts the caret there;
    /// then the field holding the page's selection announces a selected range (select), as Blink's
    /// NotifyTextControlOfSelectionChange does on every release.
    /// </summary>
    void FieldRelease()
    {
        if (_selectField is { } field && _collapseOnRelease is { } offset && InDocument(field))
            field.SetEditorSelection(offset, offset, false, byUser: false, reveal: SelectionReveal.IfNeeded);
        _selectField = null;
        _collapseOnRelease = null;
        if (_document.SelectionField is { } holder && InDocument(holder) && holder.EditorSelection.Start != holder.EditorSelection.End)
            holder.DispatchEvent(new UiEvent("select", bubbles: true, cancelable: false) { IsTrusted = true });
    }
}

/// <summary>Selecting page text with the pointer (the page's selection, outside text fields).</summary>
public sealed partial class UiInput
{
    bool _pageSelecting;
    Granularity _pageGranularity;
    DomPosition _pageBaseStart, _pageBaseEnd;
    DomPosition? _pageCollapseOnRelease;

    /// <summary>A main-button press on content that can start a selection.</summary>
    void PagePress(HitResult? hit, Vector2 position, int clicks, bool shift)
    {
        _pageCollapseOnRelease = null;
        var at = hit is { } h ? _document.PositionAt(h, position) : null;
        if (at is not { } position0)
        {
            _document.SetPageSelection(null, null);
            _pageSelecting = false;
            return;
        }
        _pageSelecting = true;
        switch (clicks)
        {
            case 1:
                _pageGranularity = Granularity.Character;
                if (shift && _document.PageSelection is { } current)
                {
                    _pageBaseStart = _pageBaseEnd = current.Anchor;
                    _document.SetPageSelection(current.Anchor, position0);
                    return;
                }
                _pageBaseStart = _pageBaseEnd = position0;
                // A press inside the selection keeps it until the release.
                if (_document.PageSelectionRange is { } range && UiDocument.Compare(range.Start, range.End) != 0
                    && UiDocument.Compare(range.Start, position0) < 0 && UiDocument.Compare(position0, range.End) < 0)
                {
                    _pageCollapseOnRelease = position0;
                    return;
                }
                _document.SetPageSelection(position0, position0);
                return;
            case 2:
                _pageGranularity = Granularity.Word;
                if (UiDocument.WordAround(position0) is { } word)
                {
                    (_pageBaseStart, _pageBaseEnd) = word;
                    _document.SetPageSelection(word.Start, word.End);
                }
                else
                {
                    _pageBaseStart = _pageBaseEnd = position0;
                    _document.SetPageSelection(position0, position0);
                }
                return;
            default:
                _pageGranularity = Granularity.Paragraph;
                if (_document.ParagraphAround(position0) is { } paragraph)
                {
                    (_pageBaseStart, _pageBaseEnd) = paragraph;
                    _document.SetPageSelection(paragraph.Start, paragraph.End);
                }
                return;
        }
    }

    /// <summary>The pointer moved with the main button held after a press on the page: the selection follows (by words or paragraphs after double or triple presses).</summary>
    void PageDrag(Vector2 position)
    {
        if (!_pageSelecting || _document.HitTest(position) is not { } hit || _document.PositionAt(hit, position) is not { } at)
            return;
        _pageCollapseOnRelease = null;
        var before = UiDocument.Compare(at, _pageBaseStart) < 0;
        switch (_pageGranularity)
        {
            case Granularity.Word:
            {
                var word = UiDocument.WordAround(at);
                if (before)
                    _document.SetPageSelection(_pageBaseEnd, word?.Start ?? at);
                else
                    _document.SetPageSelection(_pageBaseStart, word?.End ?? at);
                return;
            }
            case Granularity.Paragraph:
            {
                var paragraph = _document.ParagraphAround(at);
                if (before)
                    _document.SetPageSelection(_pageBaseEnd, paragraph?.Start ?? at);
                else
                    _document.SetPageSelection(_pageBaseStart, paragraph?.End ?? at);
                return;
            }
            default:
                _document.SetPageSelection(_pageBaseStart, at);
                return;
        }
    }

    /// <summary>A press inside the page's selection that didn't move clears it (Blink collapses only in editable content).</summary>
    void PageRelease()
    {
        if (_pageCollapseOnRelease is not null)
            _document.SetPageSelection(null, null);
        _pageCollapseOnRelease = null;
        _pageSelecting = false;
    }
}
