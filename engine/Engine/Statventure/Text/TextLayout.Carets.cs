namespace Statventure.Text;

/// <summary>
/// Caret positions in laid-out text, for editing: a caret stops at each grapheme cluster boundary;
/// inside a shaped cluster that holds several graphemes (a ligature) the stops share its width
/// evenly, as in Blink. Coordinates are the layout's own (lines' Left and Top included).
/// </summary>
public sealed partial class TextLayout
{
    /// <summary>The caret stops of a line: offsets into the text and their x, in order (a line's newline isn't one).</summary>
    public (int[] Offsets, float[] X) CaretStops(int lineIndex)
    {
        var line = _lines[lineIndex];
        var starts = new List<(int Start, float X)>();
        var runEnds = new List<float>();
        foreach (var run in line.Runs)
        {
            var previous = -1;
            foreach (var glyph in run.Glyphs)
            {
                if (glyph.Cluster == previous)
                    continue;
                previous = glyph.Cluster;
                starts.Add((glyph.Cluster, line.Left + run.X + glyph.X));
            }
        }
        var end = line.TextEnd;
        if (end > line.TextStart && end <= Text.Length && Text[end - 1] == '\n')
            end--;
        var offsets = new List<int>();
        var xs = new List<float>();
        var lineRight = line.Left + line.Width;
        for (var i = 0; i < starts.Count; i++)
        {
            var (start, x) = starts[i];
            if (start >= end)
                break;
            var clusterEnd = i + 1 < starts.Count ? Math.Min(starts[i + 1].Start, end) : end;
            var nextX = i + 1 < starts.Count ? starts[i + 1].X : lineRight;
            // The graphemes in this shaped cluster.
            var boundaries = new List<int> { start };
            for (var b = TextBoundaries.NextGrapheme(Text, start); b < clusterEnd; b = TextBoundaries.NextGrapheme(Text, b))
                boundaries.Add(b);
            for (var k = 0; k < boundaries.Count; k++)
            {
                offsets.Add(boundaries[k]);
                xs.Add(x + (nextX - x) * k / boundaries.Count);
            }
        }
        offsets.Add(end);
        xs.Add(offsets.Count > 1 ? lineRight : line.Left);
        // An empty line (or one with no glyphs) has its one stop at its start.
        if (starts.Count == 0)
        {
            offsets.Clear();
            xs.Clear();
            offsets.Add(line.TextStart);
            xs.Add(line.Left);
        }
        return (offsets.ToArray(), xs.ToArray());
    }

    /// <summary>The line an offset is on (an offset where a line wraps belongs to the next one).</summary>
    public int LineOf(int offset)
    {
        if (_lines.Count == 0)
            return -1;
        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            var end = line.TextEnd;
            var endsWithNewline = end > line.TextStart && end <= Text.Length && Text[end - 1] == '\n';
            if (offset < end || (offset == end && i == _lines.Count - 1) || (endsWithNewline && offset == end - 1))
                return i;
        }
        return _lines.Count - 1;
    }

    /// <summary>Where the caret before <paramref name="offset"/> is drawn: its x and its line.</summary>
    public (float X, int Line) CaretPosition(int offset)
    {
        var lineIndex = LineOf(offset);
        if (lineIndex < 0)
            return (0, -1);
        var (offsets, xs) = CaretStops(lineIndex);
        for (var i = 0; i < offsets.Length; i++)
            if (offsets[i] >= offset)
                return (xs[i], lineIndex);
        return (xs[^1], lineIndex);
    }

    /// <summary>The caret stop nearest to a point (the line under it, the stop nearest its x).</summary>
    public int OffsetAt(float x, float y)
    {
        if (_lines.Count == 0)
            return 0;
        var lineIndex = _lines.Count - 1;
        for (var i = 0; i < _lines.Count; i++)
            if (y < _lines[i].Top + _lines[i].Height)
            {
                lineIndex = i;
                break;
            }
        return OffsetInLine(lineIndex, x);
    }

    /// <summary>The caret stop on a line nearest to an x.</summary>
    public int OffsetInLine(int lineIndex, float x)
    {
        var (offsets, xs) = CaretStops(lineIndex);
        var best = 0;
        for (var i = 1; i < offsets.Length; i++)
            if (MathF.Abs(xs[i] - x) < MathF.Abs(xs[best] - x))
                best = i;
        return offsets[best];
    }
}
