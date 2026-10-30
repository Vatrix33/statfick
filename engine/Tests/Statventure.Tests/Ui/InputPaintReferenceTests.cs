using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// The input fixtures' screenshot steps must look as Chromium painted them at that moment
/// (Golden/input-&lt;fixture&gt;-&lt;name&gt;.png, from Tools/Scripts/make-input-references.sh): carets,
/// selections, focus rings after real input. Text compares with the text milestone's tolerance.
/// </summary>
[Collection("GPU")]
public class InputPaintReferenceTests(GpuFixture gpu)
{
    public static TheoryData<string> Fixtures =>
        new(InputFixtures.All.Where(f => f.Steps.Any(s => s.Kind == "screenshot")).Select(f => f.Name));

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheScreenshotsLookAsInChromium(string name)
    {
        var fixture = InputFixtures.All.Single(f => f.Name == name);
        var failures = new List<string>();
        InputReferenceTests.Play(fixture, (shot, document) =>
        {
            var image = PaintReferenceTests.Render(gpu.Device, document, fixture.Width, fixture.Height);
            try
            {
                Golden.Match($"input-{name}-{shot}", image, tolerance: 40);
            }
            catch (Exception exception)
            {
                failures.Add($"{shot}: {exception.Message}");
            }
        });
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
