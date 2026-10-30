using System.Numerics;
using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Statventure.Graphics;

namespace Statventure.Render3D;

/// <summary>
/// Where a 3D view is drawn: an MSAA 4× colour and depth target, resolved into
/// <see cref="Resolved"/>, a single-sample image the window shows or the canvas composites. Colour
/// is sRGB: shaders work with linear values and the hardware encodes them (and averages the MSAA
/// samples), as Blender converts its linear render for display. The ink lines read every sample's
/// surface record (<see cref="Surface"/>; integer MSAA images are sampled directly, which Vulkan 1.3
/// GPUs support for this format) and mark line boundaries in <see cref="Edges"/>.
/// </summary>
public sealed class RenderTarget3D : IDisposable
{
    public const VkFormat ColorFormat = VkFormat.R8G8B8A8SRGB;
    public const VkFormat DepthFormat = VkFormat.D32SFloat;
    public const VkFormat SurfaceFormat = VkFormat.R32G32UInt;
    public const VkFormat EdgeFormat = VkFormat.R16UInt;
    public const VkSampleCountFlags Samples = VkSampleCountFlags._4Bit;

    public int Width { get; }
    public int Height { get; }
    public GpuImage Color { get; }
    public GpuImage Depth { get; }
    public GpuImage Resolved { get; }
    public GpuImage Surface { get; }
    public GpuImage Edges { get; }

    public RenderTarget3D(GpuDevice device, int width, int height)
    {
        Width = width;
        Height = height;
        Color = GpuImage.CreateRenderTarget(device, width, height, ColorFormat, Samples);
        Depth = GpuImage.CreateDepth(device, width, height, DepthFormat, Samples);
        Resolved = GpuImage.CreateRenderTarget(device, width, height, ColorFormat);
        Surface = GpuImage.Create(device, new ImageDesc
        {
            Width = width,
            Height = height,
            Format = SurfaceFormat,
            Samples = Samples,
            Usage = VkImageUsageFlags.ColorAttachmentBit | VkImageUsageFlags.SampledBit,
        });
        Edges = GpuImage.CreateRenderTarget(device, width, height, EdgeFormat);
    }

    public void Dispose()
    {
        Color.Dispose();
        Depth.Dispose();
        Resolved.Dispose();
        Surface.Dispose();
        Edges.Dispose();
    }
}

/// <summary>How a mesh is lit.</summary>
public enum Shading
{
    /// <summary>One colour, no light.</summary>
    Flat,
    /// <summary>Two tones from the sun (Docs/Renderer-3D.md, "Cel shading").</summary>
    Cel,
}

/// <summary>
/// One mesh to draw: where it is (its model matrix), its colour (linear, as shaders use it; the lit
/// tone when cel-shaded) and whether its back faces show (glTF's doubleSided). A cel-shaded mesh
/// also has its shadow tone. For the ink lines: the model it belongs to (<see cref="Group"/>: items
/// of one model share it; an item without one is a model of its own), its part within the model
/// (a node: the crease rule applies within a part) and whether it has lines.
/// </summary>
public readonly record struct DrawItem(Mesh Mesh, Matrix4x4 Model, Vector4 Color, bool DoubleSided = false)
{
    public Shading Shading { get; init; }
    public Vector4 Shadow { get; init; }
    public object? Group { get; init; }
    public int Part { get; init; }
    public bool Ink { get; init; }
}

/// <summary>
/// The sun: the direction toward it (world space, normalised when drawn) and its strength as a
/// Blender sun's (irradiance; a white diffuse surface facing it shows strength / π).
/// </summary>
public readonly record struct SunLight(Vector3 Direction, float Strength);

/// <summary>
/// How cel shading turns light into tones and draws ink (Docs/Renderer-3D.md, "Cel shading", "Ink
/// lines"; the user's decisions by default): the ink colour (sRGB), how far a shadow tone leans
/// toward it, the band edge on the diffuse light from which a surface takes its lit tone, the
/// outline's and inner lines' widths in pixels at device scale 1 (0 for none), and the crease angle
/// (faces meeting at less than it get a line), in degrees.
/// </summary>
public sealed record CelStyle(Vector4 Ink, float ShadowMix, float BandEdge)
{
    public static readonly CelStyle Default = new(Colors.FromHex("#130e0b"), 0.42f, 0.3f);

    public float OutlineWidth { get; init; } = 3.5f;
    public float InnerWidth { get; init; } = 2f;
    public float CreaseAngle { get; init; } = 120f;

    /// <summary>The shadow tone (linear) of a lit tone (linear): mixed toward the ink in sRGB, as the lizardman script mixed.</summary>
    public Vector4 ShadowOf(Vector4 lit)
    {
        var srgb = Colors.ToSrgb(lit);
        return Colors.ToLinear(srgb + (Ink - srgb) * ShadowMix) with { W = lit.W };
    }
}

/// <summary>
/// Draws meshes into a <see cref="RenderTarget3D"/>: depth tested (reversed depth), back faces
/// culled unless double-sided, MSAA 4×, flat or cel-shaded, then the ink lines
/// (Docs/Renderer-3D.md).
/// </summary>
public sealed class Renderer3D(GpuDevice device)
{
    /// <summary>Model ids in a surface record have 10 bits (0 is the background).</summary>
    const int MaxModels = 1023;

    [StructLayout(LayoutKind.Sequential)]
    struct DrawData
    {
        public Matrix4x4 Model;
        public Matrix4x4 NormalMatrix;
        public Vector4 Color;
        public Vector4 Shadow;
        public uint Surface;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SceneData
    {
        public Matrix4x4 ViewProjection;
        public Matrix4x4 View;
        public Vector4 Sun;
        public float Band;
        public uint Orthographic;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct EdgesPush
    {
        public uint Surfaces;
        public uint Orthographic;
        public Vector2 Size;
        public Vector2 HalfExtent;
        public float CreaseCos;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LinesPush
    {
        public uint Edges;
        public int Reach;
        public Vector2 Size;
        public float OutlineHalf;
        public float InnerHalf;
        public Vector4 Ink;
    }

    /// <summary>Tests: draw only the ink lines' coverage, white on black, in place of the view.</summary>
    internal bool InkCoverageOnly { get; set; }

    [StructLayout(LayoutKind.Sequential)]
    struct Push
    {
        public ulong Vertices;
        public ulong Draw;
        public ulong Scene;
    }

    readonly Pipeline?[] _pipelines = new Pipeline?[8];

    /// <param name="mirrored">
    /// The model matrix mirrors (a negative determinant), which turns counter-clockwise triangles
    /// clockwise; glTF takes the determinant's sign as the winding order.
    /// </param>
    Pipeline MeshPipeline(Shading shading, bool doubleSided, bool mirrored) =>
        _pipelines[(int)shading * 4 + (doubleSided ? 2 : 0) + (mirrored ? 1 : 0)] ??= device.Pipelines.Get(new GraphicsPipelineDesc
        {
            VertexShader = "mesh.vert",
            FragmentShader = shading == Shading.Cel ? "mesh_cel.frag" : "mesh_flat.frag",
            Blend = BlendMode.Opaque,
            ColorFormats = [RenderTarget3D.ColorFormat, RenderTarget3D.SurfaceFormat],
            DepthFormat = RenderTarget3D.DepthFormat,
            DepthTest = true,
            DepthWrite = true,
            DepthCompare = VkCompareOp.Greater,
            // The projection's Y flip makes up point up in Vulkan's y-down framebuffer, so the picture is
            // upright, not mirrored, and glTF's counter-clockwise front faces stay counter-clockwise.
            Cull = doubleSided ? VkCullModeFlags.None : VkCullModeFlags.BackBit,
            FrontFace = mirrored ? VkFrontFace.Clockwise : VkFrontFace.CounterClockwise,
            Samples = RenderTarget3D.Samples,
        });

    Pipeline EdgesPipeline => _edges ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "fullscreen.vert",
        FragmentShader = "ink_edges.frag",
        Blend = BlendMode.Opaque,
        ColorFormats = [RenderTarget3D.EdgeFormat],
    });

    Pipeline LinesPipeline => _lines ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "fullscreen.vert",
        FragmentShader = "ink_lines.frag",
        Blend = BlendMode.Alpha,
        ColorFormats = [RenderTarget3D.ColorFormat],
    });

    Pipeline? _edges, _lines;

    /// <summary>
    /// Clears the target to <paramref name="background"/> (linear), draws the items seen by
    /// <paramref name="camera"/> under <paramref name="sun"/>, resolves, and draws the ink lines of
    /// the items that have them at <paramref name="scale"/> (the device scale) times the style's widths.
    /// </summary>
    public void Render(Frame frame, RenderTarget3D target, Camera camera, IReadOnlyList<DrawItem> items, Vector4 background, SunLight sun, CelStyle? style = null, float scale = 1)
    {
        style ??= CelStyle.Default;
        var commands = frame.Commands;
        var aspect = (float)target.Width / target.Height;
        var orthographic = camera.Projection == Projection.Orthographic;
        var direction = sun.Direction.LengthSquared() > 0 ? Vector3.Normalize(sun.Direction) : Vector3.Zero;
        var sceneAddress = frame.Upload<SceneData>([new SceneData
        {
            ViewProjection = camera.ViewProjection(aspect),
            View = camera.View,
            Sun = new Vector4(direction, sun.Strength / MathF.PI),
            Band = style.BandEdge,
            Orthographic = orthographic ? 1u : 0u,
        }]);
        commands.BeginRendering(
            [
                new ColorAttachment(target.Color, LoadAction.Clear, background, target.Resolved, Store: false),
                new ColorAttachment(target.Surface, LoadAction.Clear, Vector4.Zero),
            ],
            new DepthAttachment(target.Depth, LoadAction.Clear, 0f));
        commands.SetViewport(0, 0, target.Width, target.Height);
        commands.SetScissor(0, 0, target.Width, target.Height);
        // Model ids for the surface records, by group; past the 10 bits they repeat.
        var models = new Dictionary<object, uint>(ReferenceEqualityComparer.Instance);
        uint next = 0;
        uint NewModel() => next++ % MaxModels + 1;
        var ink = false;
        foreach (var item in items)
        {
            var model = item.Group is null ? NewModel() : models.TryGetValue(item.Group, out var known) ? known : models[item.Group] = NewModel();
            ink |= item.Ink;
            commands.BindPipeline(MeshPipeline(item.Shading, item.DoubleSided, item.Model.GetDeterminant() < 0));
            var normalMatrix = Matrix4x4.Invert(item.Model, out var inverse) ? Matrix4x4.Transpose(inverse) : item.Model;
            var draw = frame.Upload<DrawData>([new DrawData
            {
                Model = item.Model,
                NormalMatrix = normalMatrix,
                Color = item.Color,
                Shadow = item.Shadow,
                Surface = model | ((uint)item.Part & 255) << 10 | (item.Ink ? 1u << 18 : 0),
            }]);
            commands.BindIndexBuffer(item.Mesh.Indices);
            commands.Push(new Push { Vertices = item.Mesh.Vertices.Address, Draw = draw, Scene = sceneAddress });
            commands.DrawIndexed(item.Mesh.IndexCount);
        }
        commands.EndRendering();

        var outline = style.OutlineWidth * scale;
        var inner = style.InnerWidth * scale;
        if (InkCoverageOnly)
        {
            commands.BeginRendering(new ColorAttachment(target.Resolved, LoadAction.Clear, new Vector4(0, 0, 0, 1)));
            commands.EndRendering();
        }
        if (!ink || outline <= 0 && inner <= 0)
            return;
        var tanY = MathF.Tan(camera.VerticalFieldOfView / 2);
        var halfExtent = orthographic ? new Vector2(camera.OrthographicHeight / 2 * aspect, camera.OrthographicHeight / 2) : new Vector2(tanY * aspect, tanY);
        commands.Transition(target.Surface, VkImageLayout.ShaderReadOnlyOptimal);
        commands.BeginRendering(new ColorAttachment(target.Edges, LoadAction.DontCare));
        commands.BindPipeline(EdgesPipeline);
        commands.Push(new EdgesPush
        {
            Surfaces = (uint)target.Surface.BindlessIndex,
            Orthographic = orthographic ? 1u : 0u,
            Size = new Vector2(target.Width, target.Height),
            HalfExtent = halfExtent,
            // Faces meeting at less than the crease angle: their normals are more than 180° minus it apart.
            CreaseCos = MathF.Cos((180 - style.CreaseAngle) * MathF.PI / 180),
        });
        commands.Draw(3);
        commands.EndRendering();
        commands.Transition(target.Edges, VkImageLayout.ShaderReadOnlyOptimal);
        commands.BeginRendering(new ColorAttachment(target.Resolved));
        commands.BindPipeline(LinesPipeline);
        commands.Push(new LinesPush
        {
            Edges = (uint)target.Edges.BindlessIndex,
            Reach = (int)MathF.Ceiling(MathF.Max(outline, inner) / 2 + 0.5f),
            Size = new Vector2(target.Width, target.Height),
            OutlineHalf = outline / 2,
            InnerHalf = inner / 2,
            Ink = InkCoverageOnly ? Vector4.One : Colors.ToLinear(style.Ink),
        });
        commands.Draw(3);
        commands.EndRendering();
    }
}

/// <summary>sRGB colours (as palettes and pickers give them) and the linear values shaders work with.</summary>
public static class Colors
{
    public static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

    public static Vector4 ToLinear(Vector4 c) => new(ToLinear(c.X), ToLinear(c.Y), ToLinear(c.Z), c.W);

    public static float ToSrgb(float c) => c <= 0.0031308f ? c * 12.92f : 1.055f * MathF.Pow(c, 1 / 2.4f) - 0.055f;

    public static Vector4 ToSrgb(Vector4 c) => new(ToSrgb(c.X), ToSrgb(c.Y), ToSrgb(c.Z), c.W);

    /// <summary>"#rrggbb" to a linear colour (alpha 1), as shaders and <see cref="DrawItem"/> take it.</summary>
    public static Vector4 LinearFromHex(string hex) => ToLinear(FromHex(hex));

    /// <summary>"#rrggbb" to an sRGB colour (alpha 1).</summary>
    public static Vector4 FromHex(string hex)
    {
        var value = Convert.ToUInt32(hex.TrimStart('#'), 16);
        return new Vector4(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f, 1);
    }
}
