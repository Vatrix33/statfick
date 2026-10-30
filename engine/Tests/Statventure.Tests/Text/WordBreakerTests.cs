using System.Globalization;
using Statventure.Tests.Infrastructure;
using Statventure.Text;

namespace Statventure.Tests.Text;

/// <summary>
/// Word boundaries follow Unicode's word rules (Unicode 17's WordBreakTest.txt, for the cases ICU's
/// tailoring doesn't change) and ICU's tailoring (a full stop joins only numbers, a colon never).
/// </summary>
public class WordBreakerTests
{
    static string UnicodeDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference", "Unicode");

    [Fact]
    public void UnicodesTestCasesBreakAsTheRulesSay()
    {
        // Plain Word_Break values: lines where ICU's sets differ are ICU's to decide.
        var plain = new Dictionary<int, string>();
        foreach (var raw in File.ReadLines(Path.Combine(UnicodeDirectory, "WordBreakProperty.txt")))
        {
            var line = raw.Split('#')[0].Trim();
            if (line.Length == 0)
                continue;
            var parts = line.Split(';');
            var range = parts[0].Trim().Split("..");
            var first = int.Parse(range[0], NumberStyles.HexNumber);
            var last = range.Length > 1 ? int.Parse(range[1], NumberStyles.HexNumber) : first;
            for (var cp = first; cp <= last; cp++)
                plain[cp] = parts[1].Trim().Replace("_", "");
        }
        bool Tailored(int cp)
        {
            var icu = WordBreaker.ClassOf(cp, out _, out var kanaKanji).ToString();
            var uax = plain.TryGetValue(cp, out var value) ? value : "Other";
            if (kanaKanji || icu is "HangulSyllable" or "Hiragana" or "Ideographic")
                return true;
            return icu != uax;
        }
        var checkedCases = 0;
        var failures = new List<string>();
        foreach (var raw in File.ReadLines(Path.Combine(UnicodeDirectory, "WordBreakTest.txt")))
        {
            var line = raw.Split('#')[0].Trim();
            if (line.Length == 0)
                continue;
            var text = new System.Text.StringBuilder();
            var expected = new List<int>();
            var codePoints = new List<int>();
            foreach (var token in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (token == "÷")
                    expected.Add(text.Length);
                else if (token != "×")
                {
                    var cp = int.Parse(token, NumberStyles.HexNumber);
                    codePoints.Add(cp);
                    text.Append(char.ConvertFromUtf32(cp));
                }
            }
            if (codePoints.Any(Tailored))
                continue;
            checkedCases++;
            var actual = WordBreaker.Boundaries(text.ToString());
            if (!actual.SequenceEqual(expected))
                failures.Add($"{line}: got {string.Join(",", actual)}, expected {string.Join(",", expected)}");
        }
        Assert.True(checkedCases > 1000, $"only {checkedCases} cases checked");
        Assert.True(failures.Count == 0, $"{failures.Count} of {checkedCases} differ:\n" + string.Join("\n", failures.Take(20)));
    }

    [Theory]
    [InlineData("Don't stop: 3.14 apples, (big) foo-bar  end", new[] { 0, 5, 6, 10, 11, 12, 16, 17, 23, 24, 25, 26, 29, 30, 31, 34, 35, 38, 40, 43 })]
    [InlineData("e.g. a:b", new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 })]
    [InlineData("1,000.5 x", new[] { 0, 7, 8, 9 })]
    public void IcusTailoringSplitsFullStopsAndColons(string text, int[] expected) =>
        Assert.Equal(expected, WordBreaker.Boundaries(text));
}
