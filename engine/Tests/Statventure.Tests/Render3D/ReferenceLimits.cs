namespace Statventure.Tests.Render3D;

/// <summary>
/// The limits a render is held to against Blender's (Docs/Renderer-3D.md, "Reference harness"): the shading's
/// on the geometry's edges and inside surfaces (<see cref="Render3DReferenceTests.CompareShadowed"/>) and the ink
/// lines' (<see cref="Render3DReferenceTests.CompareInk"/>). <see cref="M6"/> is M6's, measured over the test
/// scenes, for every scene but a detailed stage. <see cref="DetailedStage"/> (Docs/Scene.md, S7-12) is set from
/// the measurement of Ji'ruan's stage (step 7's ink study), where two things part the engine from Freestyle
/// that M6's scenes don't have: Freestyle drops the far scenery's sub-pixel detail (bark, branches and foliage
/// 30 m away: about 56 % of the extra lines), and it draws no line where separate objects meet or rest in each
/// other, where M6's rule outlines every model against anything else (trunks, roots, rocks and plates standing
/// in the ground, leaves lying on it: about 31 %). It widens only what those make differ; a line the engine
/// misses fails as under M6.
/// </summary>
internal sealed record ReferenceLimits
{
    /// <summary>Freestyle line pixels (coverage ≥ 0.5) without an engine line pixel within one pixel, as a share of them.</summary>
    public double Missed { get; init; } = 0.02;
    /// <summary>Engine line pixels without a Freestyle line pixel within one pixel, as a share of the engine's.</summary>
    public double Extra { get; init; } = 0.03;
    /// <summary>The engine's total ink off Freestyle's, as a share of Freestyle's.</summary>
    public double TotalInk { get; init; } = 0.1;
    /// <summary>The mean coverage difference over pixels either inks.</summary>
    public double Coverage { get; init; } = 0.12;
    /// <summary>Freestyle line pixels whose 7×7 pixels about them hold 0.75 to 1.33 times less or more of the engine's ink than Freestyle's, as a share of them.</summary>
    public double Unlike { get; init; } = 0.03;

    /// <summary>On the geometry's edges (as the engine draws the scene without shadows), one pixel in this many of the frame may differ beyond <see cref="Render3DReferenceTests.Compare"/>'s rule.</summary>
    public int StrayEvery { get; init; } = 10000;
    /// <summary>The mean difference over edge pixels, in levels.</summary>
    public double EdgeMean { get; init; } = 4;
    /// <summary>Inside surfaces, the share of pixels that may differ by more than 16 levels.</summary>
    public double FarInside { get; init; } = 0.002;
    /// <summary>Inside surfaces, the summed difference from Blender as a share of the summed difference between Blender and the render without shadows.</summary>
    public double LeftOver { get; init; } = 0.1;

    /// <summary>M6's limits (Render3DReferenceTests' comments give what each was measured at).</summary>
    public static readonly ReferenceLimits M6 = new();

    /// <summary>
    /// Detailed stages (S7-12): measured on Ji'ruan's stage (600 × 340) at its six sampled times on the PC's GPU
    /// and on lavapipe (2026-11-05, against the goldens rendered with the harness's halo rule, S7-8); each limit
    /// widened is the worst measured plus a margin of about a twentieth of it, rounded. The rest stay M6's: missed
    /// lines (measured 0.63–1.21 %), the edges' mean (0.8–1.5 levels), pixels inside surfaces off by more than 16
    /// (at most 7 of 77,000–109,000) and the shadows' difference left over (3.5–6.8 %). Approved by the user as
    /// set (2026-11-05, S7-13).
    /// </summary>
    public static readonly ReferenceLimits DetailedStage = M6 with
    {
        // Measured 8.98–11.91 % (the far and back trees' detail and the outlines where objects meet; worst at 2.5 s on the GPU).
        Extra = 0.125,
        // Measured 5.94–7.97 % (the same lines: Freestyle has fewer of them about its own).
        Unlike = 0.085,
        // Measured 7.23–10.08 % more ink than Freestyle's (worst at 2.5 s on the GPU).
        TotalInk = 0.105,
        // Measured 0.152–0.167.
        Coverage = 0.175,
        // Measured 63–246 pixels of 204,000 (one in 829; the ground at trunk bases, the plates, the mid trees, the
        // king): one in 800 allows 255.
        StrayEvery = 800,
    };
}
