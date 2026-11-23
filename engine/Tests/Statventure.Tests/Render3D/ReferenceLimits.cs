namespace Statventure.Tests.Render3D;

/// <summary>
/// The limits a render is held to against Blender's (Docs/Renderer-3D.md, "Reference harness"): the shading's
/// on the geometry's edges and inside surfaces (<see cref="Render3DReferenceTests.CompareShadowed"/>) and the ink
/// lines' (<see cref="Render3DReferenceTests.CompareInk"/>). <see cref="M6"/> is M6's, measured over the test
/// scenes, for every scene but a detailed stage or figure (<see cref="DetailedFigure"/>, GM2 step 4a: the humanoid
/// family's figures) or a race emblem (<see cref="Emblem"/>, GM2 step 10; the carousel's live emblem: <see cref="LiveEmblem"/>,
/// GM2 step 11). <see cref="DetailedStage"/> (Docs/Scene.md, S7-12) is set from
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
    /// <summary>A scene without shadows (Render3DReferenceTests.Compare with a profile): one pixel in this many of the frame may be an inside pixel off by more than 2 levels (M6: none).</summary>
    public int InsideEvery { get; init; } = int.MaxValue;
    /// <summary>A scene without shadows: one pixel in this many of the frame may be an edge pixel off beyond 0.7 of its contrast and 8 levels (M6: none).</summary>
    public int EdgeEvery { get; init; } = int.MaxValue;
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

    /// <summary>
    /// Detailed figures (GM2 step 4a; Docs/Game/Hero.md, "How it works", step 4a, "The detailed figures' limits"): the
    /// humanoid family's figures in the game's look (HumanoidReferenceTests' 14 scenes, 360 × 480 and the 320 × 320
    /// close-ups; step 5's six assembled looks are held to them too), measured on the PC's GPU and on lavapipe; a limit
    /// stays widened only where a scene measures past M6's, at the worst plus about a tenth, the rest M6's. First set on
    /// 2026-11-16 (the user's Q55, "approve the limits now, refine the lines after"), then tightened after GM2's ink task
    /// (the user's Q58 and Q60, 2026-11-18: each sample's sheet, a covered edge hidden as Freestyle hides it, the
    /// meshes' silhouettes; Docs/Renderer-3D.md, "Feature edges, how they're drawn"). What still parts the engine from
    /// Blender here: MSAA's edges on the figures' many small forms (the shading's stray pixels, untouched by the ink
    /// task), and lines Freestyle judges by a long edge's middle where the engine sees them pixel by pixel (the sword's
    /// and the shield's seen from the side). M6's: missed lines (measured 0–0.09 %), extra lines (0.09–1.82 %), the
    /// mean coverage difference (0.067–0.093), the total ink (0.10–6.56 % off Freestyle's) and the edges' mean
    /// (2.06–2.81 levels).
    /// </summary>
    public static readonly ReferenceLimits DetailedFigure = M6 with
    {
        // Measured 1–23 inside pixels off by more than 2 levels; the worst share 23 of 102,400 (the man's close-up, one
        // in 4,452): one in 4,200 allows 24 there, 41 in a full view.
        InsideEvery = 4200,
        // Measured 12–88 edge pixels beyond; the worst share 88 of 102,400 (the man's close-up on lavapipe, one in 1,164):
        // one in 1,100 allows 93 there, 157 in a full view.
        EdgeEvery = 1100,
        // Measured 0.13–7.56 % (the woman geared from the side, both drivers: the sword's and the shield's lines; 12.71 %
        // before the ink task).
        Unlike = 0.083,
    };

    /// <summary>
    /// The race emblems (GM2 step 10; Docs/Game/Hero.md, "How it works", step 10; EmblemReferenceTests' nine medallions at
    /// 640 × 640 from the pictures' camera, 5° up): the detailed figures' limits with three widened, the user's Q70
    /// ("Accept for now, fix the engine later", 2026-11-21), each the worst measured on the PC's GPU and on lavapipe plus
    /// about a tenth. What parts the engine from Blender: the engine draws a line along the top of every raised outline,
    /// where its side, seen nearly edge-on, rises from the field or from the outline under it; no mesh edge runs there, so
    /// Freestyle draws none (the engine "can't tell two surfaces crossing from a silhouette": Docs/Renderer-3D.md, "Ink
    /// comparison"). An engine task is to stop that line (Docs/Game-Plan.md, "Engine items the port will meet"); then these
    /// are measured again and tightened. The rest are DetailedFigure's: the shading (inside 20–34 pixels off by more than 2
    /// levels of 409,600, edge 20–33, the edges' mean 3.1–3.9), missed lines (0–1.54 %) and unlike ink (0.09–4.03 %).
    /// </summary>
    public static readonly ReferenceLimits Emblem = DetailedFigure with
    {
        // Measured 5.50–13.02 % (the Half-Beast's claw marks on lavapipe; 12.77 % on the GPU).
        Extra = 0.143,
        // Measured 6.01–16.95 % more ink than Freestyle's (the Half-Beast's, on the GPU).
        TotalInk = 0.186,
        // Measured 0.097–0.140 (the Half-Beast's).
        Coverage = 0.154,
    };

    /// <summary>
    /// The carousel's live emblems (GM2 step 11; Docs/Game/Hero.md, "How it works", step 11; LiveViewReferenceTests' nine
    /// scenes at 640 × 640 by step 10's pictures' camera, 5° up, the user's Q80, under the emblem's own light from the
    /// front, Q79, the pose 1 s into the sway, 24.7° to the right; the manager's GM2-P47 the size): the emblems' limits
    /// (<see cref="Emblem"/>) with two widened, the user's Q81 ("Accept for now, as before", 2026-11-22), each the worst
    /// measured on the PC's GPU and on lavapipe plus about a tenth. What parts the engine from Blender in the turned
    /// medallion: Freestyle draws a contour where a raised inner wall or the edge of a lit crescent is seen nearly edge-on
    /// (the dark elf's eclipse crescent: the engine draws none there), and the rim's beads have single pixels inside the
    /// gold a few levels apart at the light's band edge (most on lavapipe). Both joined the engine task the user accepted
    /// with Q70 (Docs/Game-Plan.md, "Engine items the port will meet": the engine's lines on raised relief); then these
    /// are measured again and tightened. The rest are the emblems'.
    /// </summary>
    public static readonly ReferenceLimits LiveEmblem = Emblem with
    {
        // Measured 0.01–3.28 % (the dark elf's crescent contour, the same on both drivers).
        Missed = 0.036,
        // Measured 82–120 inside pixels off by more than 2 levels of 409,600 (the elf's on lavapipe, one in 3,413; 97 on
        // the GPU): one in 3,100 allows 132.
        InsideEvery = 3100,
    };
}
