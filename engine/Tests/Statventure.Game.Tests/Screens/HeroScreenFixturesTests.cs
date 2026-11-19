using System.Numerics;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The premise of the step's fixtures (<see cref="HeroScreenFixtures"/>): Chromium's references are captured inside a
/// headless compositor whose pointer rests at its output's middle (1200 device px down and right of the page's corner),
/// which the engine's fixtures have no pointer for; so wherever it falls on a fixture's page (at 1.65 the page's 727 px;
/// at 1 it lies below every fixture) nothing may light: the fixture drawn with the pointer there is the fixture drawn
/// without it, pixel for pixel.
/// </summary>
[Collection("GPU")]
public class HeroScreenFixturesTests(GpuFixture gpu)
{
    const float CompositorPointer = 1200;

    public static TheoryData<string> Names
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var fixture in new HeroScreenFixtures().Fixtures)
                data.Add(fixture.Name);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TheCompositorsPointerLightsNothing(string name)
    {
        var fixture = new HeroScreenFixtures().Fixtures.Single(f => f.Name == name);
        foreach (var scale in GameScreenFixtures.Scales)
        {
            var at = new Vector2(CompositorPointer / scale);
            if (at.X >= fixture.Width || at.Y >= fixture.Height)
                continue;
            using var harness = GameScreenFixtures.Build(fixture, scale);
            var (width, height) = ((int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
            harness.Wait(0.5);
            var without = GameScreenTests.Render(gpu.Device, harness, width, height);
            harness.Move(at);
            harness.Wait(0.5);
            var with = GameScreenTests.Render(gpu.Device, harness, width, height);
            var differing = 0;
            for (var i = 0; i < with.Pixels.Length; i += 4)
                if (with.Pixels[i] != without.Pixels[i] || with.Pixels[i + 1] != without.Pixels[i + 1] || with.Pixels[i + 2] != without.Pixels[i + 2])
                    differing++;
            Assert.True(differing == 0, $"{name} at {scale}: the compositor's pointer at {at} (CSS px) lights {differing} pixels");
        }
    }
}
