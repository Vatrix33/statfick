using System.Numerics;
using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Statventure.Graphics;

namespace Statventure.Render3D;

/// <summary>
/// Where a 3D view is drawn: an MSAA 4× colour and depth target, resolved into
/// <see cref="Resolved"/>, a single-sample image the window shows or the canvas composites. Colour
/// is sRGB: shaders work with linear values and the hardware encodes them (and averages the MSAA
/// samples), as Blender converts its linear render for display.
/// </summary>
public sealed class RenderTarget3D : IDisposable
{
    public const VkFormat ColorFormat = VkFormat.R8G8B8A8SRGB;
    public const VkFormat DepthFormat = VkFormat.D32SFloat;
    public const VkSampleCountFlags Samples = VkSampleCountFlags._4Bit;

    public int Width { get; }
    public int Height { get; }
    public GpuImage Color { get; }
    public GpuImage Depth { get; }
    public GpuImage Resolved { get; }

    public RenderTarget3D(GpuDevice device, int width, int height)
    {
        Width = width;
        Height = height;
        Color = GpuImage.CreateRenderTarget(device, width, height, ColorFormat, Samples);
        Depth = GpuImage.CreateDepth(device, width, height, DepthFormat, Samples);
        Resolved = GpuImage.CreateRenderTarget(device, width, height, ColorFormat);
    }

    public void Dispose()
    {
        Color.Dispose();
        Depth.Dispose();
        Resolved.Dispose();
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
/// also has its shadow tone.
/// </summary>
public readonly record struct DrawItem(Mesh Mesh, Matrix4x4 Model, Vector4 Color, bool DoubleSided = false)
{
    public Shading Shading { get; init; }
    public Vector4 Shadow { get; init; }
}

/// <summary>
/// The sun: the direction toward it (world space, normalised when drawn) and its strength as a
/// Blender sun's (irradiance; a white diffuse surface facing it shows strength / π).
/// </summary>
public readonly record struct SunLight(Vector3 Direction, float Strength);

/// <summary>
/// How cel shading turns light into tones (Docs/Renderer-3D.md, "Cel shading"; the lizardman test's
/// values by default): the ink colour (sRGB), how far a shadow tone leans toward it, and the band
/// edge on the diffuse light from which a surface takes its lit tone.
/// </summary>
public sealed record CelStyle(Vector4 Ink, float ShadowMix, float BandEdge)
{
    public static readonly CelStyle Default = new(Colors.FromHex("#130e0b"), 0.42f, 0.3f);

    /// <summary>The shadow tone (linear) of a lit tone (linear): mixed toward the ink in sRGB, as the lizardman script mixed.</summary>
    public Vector4 ShadowOf(Vector4 lit)
    {
        var srgb = Colors.ToSrgb(lit);
        return Colors.ToLinear(srgb + (Ink - srgb) * ShadowMix) with { W = lit.W };
    }
}

/// <summary>
/// Draws meshes into a <see cref="RenderTarget3D"/>: depth tested (reversed depth), back faces
/// culled unless double-sided, MSAA 4×, flat or cel-shaded (Docs/Renderer-3D.md).
/// </summary>
public sealed class Renderer3D(GpuDevice device)
{
    [StructLayout(LayoutKind.Sequential)]
    struct DrawData
    {
        public Matrix4x4 Model;
        public Matrix4x4 NormalMatrix;
        public Vector4 Color;
        public Vector4 Shadow;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SceneData
    {
        public Matrix4x4 ViewProjection;
        public Vector4 Sun;
        public float Band;
    }

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
            ColorFormats = [RenderTarget3D.ColorFormat],
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

    /// <summary>
    /// Clears the target to <paramref name="background"/> (linear), draws the items seen by
    /// <paramref name="camera"/> under <paramref name="sun"/> and resolves.
    /// </summary>
    public void Render(Frame frame, RenderTarget3D target, Camera camera, IReadOnlyList<DrawItem> items, Vector4 background, SunLight sun, CelStyle? style = null)
    {
        style ??= CelStyle.Default;
        var commands = frame.Commands;
        var direction = sun.Direction.LengthSquared() > 0 ? Vector3.Normalize(sun.Direction) : Vector3.Zero;
        var sceneAddress = frame.Upload<SceneData>([new SceneData
        {
            ViewProjection = camera.ViewProjection((float)target.Width / target.Height),
            Sun = new Vector4(direction, sun.Strength / MathF.PI),
            Band = style.BandEdge,
        }]);
        commands.BeginRendering(new ColorAttachment(target.Color, LoadAction.Clear, background, target.Resolved, Store: false),
            new DepthAttachment(target.Depth, LoadAction.Clear, 0f));
        commands.SetViewport(0, 0, target.Width, target.Height);
        commands.SetScissor(0, 0, target.Width, target.Height);
        foreach (var item in items)
        {
            commands.BindPipeline(MeshPipeline(item.Shading, item.DoubleSided, item.Model.GetDeterminant() < 0));
            var normalMatrix = Matrix4x4.Invert(item.Model, out var inverse) ? Matrix4x4.Transpose(inverse) : item.Model;
            var draw = frame.Upload<DrawData>([new DrawData { Model = item.Model, NormalMatrix = normalMatrix, Color = item.Color, Shadow = item.Shadow }]);
            commands.BindIndexBuffer(item.Mesh.Indices);
            commands.Push(new Push { Vertices = item.Mesh.Vertices.Address, Draw = draw, Scene = sceneAddress });
            commands.DrawIndexed(item.Mesh.IndexCount);
        }
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
