using System.Runtime.CompilerServices;
using Statventure.Imaging;

namespace Statventure.Tests.Infrastructure;

/// <summary>
/// Compares a render with a reviewed reference image in Tests/Statventure.Tests/Golden. When the
/// reference is missing or differs, the render is written to Tests/Statventure.Tests/Actual for
/// review; Tools/Scripts/accept-golden.sh copies accepted renders over the references.
/// </summary>
public static class Golden
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string GoldenPath(string name) => Path.Combine(ProjectDirectory(), "Golden", name + ".png");
    public static string ActualPath(string name) => Path.Combine(ProjectDirectory(), "Actual", name + ".png");

    /// <param name="tolerance">How far (0-255) a channel may differ before the pixel counts as different.</param>
    /// <param name="maxDifferentRatio">The share of pixels allowed to differ.</param>
    public static void Match(string name, RgbaImage actual, int tolerance = 3, double maxDifferentRatio = 0.002)
    {
        var goldenPath = GoldenPath(name);
        var actualPath = ActualPath(name);
        if (!File.Exists(goldenPath))
        {
            ImageFile.SavePng(actualPath, actual);
            Assert.Fail($"no reference image for {name}; the render is in {actualPath} for review");
        }
        var golden = ImageFile.Load(goldenPath);
        if (golden.Width != actual.Width || golden.Height != actual.Height)
        {
            ImageFile.SavePng(actualPath, actual);
            Assert.Fail($"{name}: size {actual.Width}x{actual.Height}, reference {golden.Width}x{golden.Height}; render in {actualPath}");
        }
        var different = 0;
        var worst = 0;
        for (var i = 0; i < actual.Pixels.Length; i += 4)
        {
            var pixelWorst = 0;
            for (var c = 0; c < 4; c++)
                pixelWorst = Math.Max(pixelWorst, Math.Abs(actual.Pixels[i + c] - golden.Pixels[i + c]));
            worst = Math.Max(worst, pixelWorst);
            if (pixelWorst > tolerance)
                different++;
        }
        var ratio = different / (double)(actual.Width * actual.Height);
        if (ratio > maxDifferentRatio)
        {
            ImageFile.SavePng(actualPath, actual);
            Assert.Fail($"{name}: {different} pixels ({ratio:P2}) differ from the reference (worst {worst}); render in {actualPath}");
        }
        // STATVENTURE_KEEP_RENDERS=1 keeps passing renders in Actual/ for review.
        if (Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        else if (File.Exists(actualPath))
            File.Delete(actualPath);
    }
}
