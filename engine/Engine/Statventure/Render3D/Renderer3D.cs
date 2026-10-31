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
    public const VkFormat EdgeFormat = VkFormat.R32UInt;
    public const VkFormat FeatureFormat = VkFormat.R32G32B32A32UInt;
    /// <summary>The particle layer: premultiplied linear colour, blended and resolved at 16-bit float.</summary>
    public const VkFormat ParticleFormat = VkFormat.R16G16B16A16SFloat;
    public const VkSampleCountFlags Samples = VkSampleCountFlags._4Bit;
    /// <summary>
    /// A shadow map's size: 16 maps, one per direction over the sun's disc, fitted to the visible
    /// area, in a 4×4 atlas (Docs/Renderer-3D.md, "Shadows").
    /// </summary>
    public const int ShadowSize = 1024;
    public const int ShadowTiles = 4;

    public int Width { get; }
    public int Height { get; }
    public GpuImage Color { get; }
    public GpuImage Depth { get; }
    public GpuImage Resolved { get; }
    public GpuImage Surface { get; }
    public GpuImage Edges { get; }
    public GpuImage ShadowMap { get; }
    /// <summary>The nearest feature edge point at each pixel (feature.frag) and its depth test.</summary>
    public GpuImage Features { get; }
    public GpuImage FeatureDepth { get; }

    readonly GpuDevice _device;
    GlowImages? _glow;
    (GpuImage Layer, GpuImage Resolved)? _particles;

    /// <summary>The particles' layer (MSAA 4×) and its resolve, made when a view first shows particles.</summary>
    internal (GpuImage Layer, GpuImage Resolved) Particles => _particles ??= (
        GpuImage.CreateRenderTarget(_device, Width, Height, ParticleFormat, Samples),
        GpuImage.CreateRenderTarget(_device, Width, Height, ParticleFormat));

    /// <summary>
    /// The glow's images (Docs/Renderer-3D.md, "Glow"), made when a view first glows: the glowing
    /// parts (sRGB-encoded colour; model and depth) and their depth test, the blur's two working
    /// images and its two results, the halo's owners (two passes), and the composited view.
    /// </summary>
    internal sealed record GlowImages(GpuImage Source, GpuImage Owner, GpuImage Depth, GpuImage BlurA, GpuImage BlurB, GpuImage Wide, GpuImage Core,
        GpuImage OwnerA, GpuImage OwnerB, GpuImage Composite)
    {
        public IEnumerable<GpuImage> All => [Source, Owner, Depth, BlurA, BlurB, Wide, Core, OwnerA, OwnerB, Composite];
    }

    internal GlowImages Glow => _glow ??= new GlowImages(
        GpuImage.CreateRenderTarget(_device, Width, Height, VkFormat.R8G8B8A8UNorm),
        GpuImage.CreateRenderTarget(_device, Width, Height, SurfaceFormat),
        GpuImage.CreateDepth(_device, Width, Height, DepthFormat),
        GpuImage.CreateRenderTarget(_device, Width, Height, VkFormat.R16G16B16A16SFloat),
        GpuImage.CreateRenderTarget(_device, Width, Height, VkFormat.R16G16B16A16SFloat),
        GpuImage.CreateRenderTarget(_device, Width, Height, VkFormat.R16G16B16A16SFloat),
        GpuImage.CreateRenderTarget(_device, Width, Height, VkFormat.R16G16B16A16SFloat),
        GpuImage.CreateRenderTarget(_device, Width, Height, SurfaceFormat),
        GpuImage.CreateRenderTarget(_device, Width, Height, SurfaceFormat),
        GpuImage.CreateRenderTarget(_device, Width, Height, ColorFormat));

    public RenderTarget3D(GpuDevice device, int width, int height)
    {
        _device = device;
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
        ShadowMap = GpuImage.CreateDepth(device, ShadowSize * ShadowTiles, ShadowSize * ShadowTiles, DepthFormat, sampled: true);
        Features = GpuImage.CreateRenderTarget(device, width, height, FeatureFormat);
        FeatureDepth = GpuImage.CreateDepth(device, width, height, DepthFormat);
    }

    public void Dispose()
    {
        Color.Dispose();
        Depth.Dispose();
        Resolved.Dispose();
        Surface.Dispose();
        Edges.Dispose();
        ShadowMap.Dispose();
        Features.Dispose();
        FeatureDepth.Dispose();
        foreach (var image in _glow?.All ?? [])
            image.Dispose();
        _particles?.Layer.Dispose();
        _particles?.Resolved.Dispose();
    }
}

/// <summary>How a mesh is lit.</summary>
public enum Shading
{
    /// <summary>One colour, no light.</summary>
    Flat,
    /// <summary>Two tones from the sun (Docs/Renderer-3D.md, "Cel shading").</summary>
    Cel,
    /// <summary>Cel-shaded in the covers' tones, blended (<see cref="DrawItem.Terrain"/>; Docs/Renderer-3D.md, "Terrain").</summary>
    Terrain,
    /// <summary>Cel-shaded with a rippling normal (Docs/Renderer-3D.md, "Terrain").</summary>
    Water,
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
    /// <summary>Casts a shadow (Docs/Renderer-3D.md, "Shadows").</summary>
    public bool CastsShadow { get; init; }
    /// <summary>Receives shadows: scenery (the stage, terrain and its features).</summary>
    public bool ReceivesShadows { get; init; }
    /// <summary>Hatched in shadow (cel-shaded items; Docs/Renderer-3D.md, "Hatching").</summary>
    public bool Hatch { get; init; }
    /// <summary>Where its model is (world): the hatching moves with it.</summary>
    public Vector3 Anchor { get; init; }
    /// <summary>Its glow colour (linear; zero: none; Docs/Renderer-3D.md, "Glow").</summary>
    public Vector3 Glow { get; init; }
    /// <summary>Posed on the GPU before drawing (a skinned mesh; Docs/Renderer-3D.md, "GPU skinning").</summary>
    public SkinJob? Skin { get; init; }
    /// <summary>Its mesh's creases and open edges for the ink lines.</summary>
    public FeatureEdges? Features { get; init; }
    /// <summary>A terrain chunk's cover blending (<see cref="Shading.Terrain"/>).</summary>
    public TerrainShade? Terrain { get; init; }
}

/// <summary>
/// The sun: the direction toward it (world space, normalised when drawn), its strength as a Blender
/// sun's (irradiance; a white diffuse surface facing it shows strength / π) and its angular
/// diameter in degrees (Blender's sun angle), which softens shadows.
/// </summary>
public readonly record struct SunLight(Vector3 Direction, float Strength)
{
    public float Angle { get; init; } = 3;
}

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
    /// <summary>Hatching in shadow: lines this far apart and this wide (pixels at device scale 1), at this angle (degrees, clockwise from the right on screen), this far toward the ink.</summary>
    public float HatchSpacing { get; init; } = 6;
    public float HatchWidth { get; init; } = 1;
    public float HatchAngle { get; init; } = 72;
    public float HatchInk { get; init; } = 0.35f;
    /// <summary>The glow's halo (the lizardman's compose.py): the wide blur's and the core's Gaussian radius (standard deviation, pixels at device scale 1) and the wide halo's alpha boost.</summary>
    public float GlowWide { get; init; } = 8;
    public float GlowCore { get; init; } = 2.5f;
    public float GlowBoost { get; init; } = 2.2f;

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
    /// <summary>Model ids in a surface record have 10 bits (0 is the background, 1023 the particles in the glow's owner records).</summary>
    const int MaxModels = 1022;

    [StructLayout(LayoutKind.Sequential)]
    struct DrawData
    {
        public Matrix4x4 Model;
        public Matrix4x4 NormalMatrix;
        public Vector4 Color;
        public Vector4 Shadow;
        public uint Surface;
        public uint ReceivesShadows;
        public Vector3 Anchor;
        public uint Hatch;
        public ulong Extra;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SceneData
    {
        public Matrix4x4 ViewProjection;
        public Matrix4x4 View;
        public Vector4 Sun;
        public float Band;
        public uint Orthographic;
        public Matrix4x4 LightViewProjection;
        public Vector4 ShadowScale;
        public uint ShadowMap;
        public float ShadowTexel;
        public float ShadowFar;
        public Vector4 Ink;
        public Vector2 Viewport;
        public Vector2 HatchNormal;
        public float HatchSpacing;
        public float HatchWidth;
        public float HatchInk;
        public float Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ParticleData
    {
        public Vector3 Centre;
        public float Pixel;
        public Vector3 AxisU;
        public float HalfU;
        public Vector3 AxisV;
        public float HalfV;
        public Vector4 Color;
        public uint Shape;
        public float P0;
        public float P1;
        public float Outline;
        public float Opacity;
        public float Depth;
        public uint Glow;
        public uint Pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ParticlePush
    {
        public ulong Quads;
        public ulong Scene;
    }

    /// <summary>The 16 directions over the sun's disc (a Vogel spiral: even over its area), in units of its radius.</summary>
    static readonly Vector2[] SunDisc = Enumerable.Range(0, 16).Select(i =>
    {
        var r = MathF.Sqrt((i + 0.5f) / 16);
        var a = i * 2.39996323f;
        return new Vector2(r * MathF.Cos(a), r * MathF.Sin(a));
    }).ToArray();

    [StructLayout(LayoutKind.Sequential)]
    struct EdgesPush
    {
        public uint Surfaces;
        public uint Orthographic;
        public Vector2 Size;
        public Vector2 HalfExtent;
        public float CreaseCos;
        public uint Features;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FeaturePush
    {
        public ulong Vertices;
        public ulong Edges;
        public ulong Draw;
        public ulong Scene;
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

    [StructLayout(LayoutKind.Sequential)]
    struct SkinPush
    {
        public ulong Rest;
        public ulong Influences;
        public ulong Joints;
        public ulong Posed;
        public uint Count;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NormalsPush
    {
        public ulong Posed;
        public ulong Fans;
        public ulong Indices;
        public uint Count;
    }

    /// <summary>A terrain's cover blending as terrain.frag reads it (TerrainShade): a 32-byte head, then the tones.</summary>
    static ulong UploadTerrain(Frame frame, TerrainShade shade)
    {
        static float Bits(int value) => BitConverter.Int32BitsToSingle(value);
        Vector4[] block =
        [
            new Vector4(shade.Origin.X, shade.Origin.Y, shade.CellSize, Bits(shade.Covers.BindlessIndex)),
            new Vector4(Bits(shade.Width), Bits(shade.Depth), Bits(shade.Tones.Length / 2), 0),
            .. shade.Tones,
        ];
        return frame.Upload<Vector4>(block);
    }

    /// <summary>Per frame in flight: device memory for posed vertices, reused from the frame's start.</summary>
    readonly GpuBuffer?[] _arenas = new GpuBuffer?[8];
    readonly long[] _arenaFrames = new long[8];
    readonly ulong[] _arenaUsed = new ulong[8];

    /// <summary>
    /// Skins every item with a skinning job (Docs/Renderer-3D.md, "GPU skinning"): skin.comp moves the
    /// vertices into this frame's device memory, normals.comp recomputes their normals; returns each
    /// item's vertex address for drawing.
    /// </summary>
    ulong[] Skin(Frame frame, IReadOnlyList<DrawItem> items)
    {
        var addresses = items.Select(i => i.Mesh.Vertices.Address).ToArray();
        var jobs = Enumerable.Range(0, items.Count).Where(i => items[i].Skin != null).ToList();
        if (jobs.Count == 0)
            return addresses;
        var slot = frame.Slot.Index;
        if (_arenaFrames[slot] != frame.Number)
            (_arenaFrames[slot], _arenaUsed[slot]) = (frame.Number, 0);
        var need = _arenaUsed[slot] + (ulong)jobs.Sum(i => (items[i].Mesh.VertexCount * 32 + 255) / 256 * 256);
        if (_arenas[slot] is not { } arena || arena.Size < need)
        {
            // A bigger buffer: this frame's earlier posed vertices stay in the old one (released later).
            _arenas[slot]?.Dispose();
            _arenas[slot] = arena = GpuBuffer.Create(device, Math.Max(need * 2, 1 << 20), VkBufferUsageFlags.StorageBufferBit, MemoryKind.Device);
            _arenaUsed[slot] = 0;
        }
        var commands = frame.Commands;
        var posed = new ulong[items.Count];
        commands.BindPipeline(device.Pipelines.GetCompute("skin.comp"));
        foreach (var i in jobs)
        {
            var job = items[i].Skin!;
            posed[i] = arena.Address + _arenaUsed[slot];
            _arenaUsed[slot] += (ulong)(job.Rest.VertexCount * 32 + 255) / 256 * 256;
            commands.Push(new SkinPush
            {
                Rest = job.Rest.Vertices.Address,
                Influences = job.Influences.Address,
                Joints = frame.Upload<Matrix4x4>(job.Joints),
                Posed = posed[i],
                Count = (uint)job.Rest.VertexCount,
            });
            commands.Dispatch((job.Rest.VertexCount + 63) / 64);
        }
        commands.MemoryBarrier();
        commands.BindPipeline(device.Pipelines.GetCompute("normals.comp"));
        foreach (var i in jobs)
        {
            var job = items[i].Skin!;
            commands.Push(new NormalsPush { Posed = posed[i], Fans = job.Fans.Address, Indices = job.Rest.Indices.Address, Count = (uint)job.Rest.VertexCount });
            commands.Dispatch((job.Rest.VertexCount + 63) / 64);
            addresses[i] = posed[i];
        }
        commands.MemoryBarrier();
        return addresses;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BlurPush
    {
        public uint Source;
        public int Radius;
        public float Fraction;
        public int Horizontal;
        public Vector2 Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct OwnerPush
    {
        public uint Source;
        public int Reach;
        public int Horizontal;
        public uint Pad;
        public Vector2 Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CompositePush
    {
        public uint Scene;
        public uint Wide;
        public uint Core;
        public uint Owner;
        public uint Surfaces;
        public float Boost;
    }

    readonly Pipeline?[] _pipelines = new Pipeline?[16];

    /// <param name="mirrored">
    /// The model matrix mirrors (a negative determinant), which turns counter-clockwise triangles
    /// clockwise; glTF takes the determinant's sign as the winding order.
    /// </param>
    Pipeline MeshPipeline(Shading shading, bool doubleSided, bool mirrored) =>
        _pipelines[(int)shading * 4 + (doubleSided ? 2 : 0) + (mirrored ? 1 : 0)] ??= device.Pipelines.Get(new GraphicsPipelineDesc
        {
            VertexShader = "mesh.vert",
            FragmentShader = shading switch
            {
                Shading.Cel => "mesh_cel.frag",
                Shading.Terrain => "terrain.frag",
                Shading.Water => "water.frag",
                _ => "mesh_flat.frag",
            },
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

    Pipeline ShadowPipeline => _shadow ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "mesh.vert",
        FragmentShader = "shadow.frag",
        Blend = BlendMode.Opaque,
        ColorFormats = [],
        DepthFormat = RenderTarget3D.DepthFormat,
        DepthTest = true,
        DepthWrite = true,
        DepthCompare = VkCompareOp.Greater,
        // Both sides cast: open and single-sided meshes block light too.
        Cull = VkCullModeFlags.None,
    });

    Pipeline FeaturePipeline => _feature ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "feature.vert",
        FragmentShader = "feature.frag",
        Blend = BlendMode.Opaque,
        ColorFormats = [RenderTarget3D.FeatureFormat],
        DepthFormat = RenderTarget3D.DepthFormat,
        DepthTest = true,
        DepthWrite = true,
        DepthCompare = VkCompareOp.Greater,
    });

    Pipeline? _feature, _edges, _lines, _shadow, _glowSource, _particle, _particleGlow, _particleOwner, _particleComposite;

    Pipeline ParticlePipeline => _particle ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "particle.vert",
        FragmentShader = "particle.frag",
        Blend = BlendMode.Premultiplied,
        ColorFormats = [RenderTarget3D.ParticleFormat],
        DepthFormat = RenderTarget3D.DepthFormat,
        DepthTest = true,
        DepthCompare = VkCompareOp.Greater,
        Samples = RenderTarget3D.Samples,
    });

    Pipeline ParticleGlowPipeline => _particleGlow ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "particle.vert",
        FragmentShader = "particle_glow.frag",
        Blend = BlendMode.Premultiplied,
        ColorFormats = [VkFormat.R8G8B8A8UNorm],
        DepthFormat = RenderTarget3D.DepthFormat,
        DepthTest = true,
        DepthCompare = VkCompareOp.Greater,
    });

    Pipeline ParticleOwnerPipeline => _particleOwner ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "particle.vert",
        FragmentShader = "particle_owner.frag",
        Blend = BlendMode.Opaque,
        ColorFormats = [RenderTarget3D.SurfaceFormat],
        DepthFormat = RenderTarget3D.DepthFormat,
        DepthTest = true,
        DepthWrite = true,
        DepthCompare = VkCompareOp.Greater,
    });

    Pipeline ParticleCompositePipeline => _particleComposite ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "fullscreen.vert",
        FragmentShader = "particle_composite.frag",
        Blend = BlendMode.Premultiplied,
        ColorFormats = [RenderTarget3D.ColorFormat],
    });

    Pipeline GlowSourcePipeline => _glowSource ??= device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "mesh.vert",
        FragmentShader = "glow.frag",
        Blend = BlendMode.Opaque,
        ColorFormats = [VkFormat.R8G8B8A8UNorm, RenderTarget3D.SurfaceFormat],
        DepthFormat = RenderTarget3D.DepthFormat,
        DepthTest = true,
        DepthWrite = true,
        DepthCompare = VkCompareOp.Greater,
    });

    Pipeline FullscreenPipeline(string fragment, VkFormat format) => device.Pipelines.Get(new GraphicsPipelineDesc
    {
        VertexShader = "fullscreen.vert",
        FragmentShader = fragment,
        Blend = BlendMode.Opaque,
        ColorFormats = [format],
    });

    /// <summary>
    /// Pillow's GaussianBlur radius as its extended box (BoxBlur.c, after Gwosdek et al.): three
    /// passes of a box of 2·whole + 1 pixels and one more at each end weighted by the fraction.
    /// </summary>
    internal static (int Whole, float Fraction) BoxOf(float sigma)
    {
        var variance = sigma * sigma / 3;
        var length = MathF.Sqrt(12 * variance + 1);
        var whole = (int)MathF.Floor((length - 1) / 2);
        var fraction = (2 * whole + 1) * (whole * (whole + 1) - 3 * variance) / (6 * (variance - (whole + 1) * (whole + 1)));
        return (whole, fraction);
    }

    /// <summary>
    /// The lizardman's glow (Docs/Renderer-3D.md, "Glow"): the glowing parts drawn alone, blurred
    /// wide and tight as compose.py did, and laid over the view behind their models.
    /// </summary>
    void RenderGlow(Frame frame, RenderTarget3D target, IReadOnlyList<DrawItem> items, ulong[] vertices, uint[] models, ulong sceneAddress, CelStyle style, float scale,
        (ulong Address, int Count, int Glowing) particles)
    {
        var commands = frame.Commands;
        var glow = target.Glow;
        var size = new Vector2(target.Width, target.Height);
        commands.BeginRendering(
            [new ColorAttachment(glow.Source, LoadAction.Clear, Vector4.Zero), new ColorAttachment(glow.Owner, LoadAction.Clear, Vector4.Zero)],
            new DepthAttachment(glow.Depth, LoadAction.Clear, 0f, Store: particles.Glowing > 0));
        commands.BindPipeline(GlowSourcePipeline);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.Glow == Vector3.Zero)
                continue;
            // The glow colour rides in the shadow tone's place.
            var draw = frame.Upload<DrawData>([new DrawData { Model = item.Model, NormalMatrix = item.Model, Shadow = new Vector4(item.Glow, 1), Surface = models[i] }]);
            commands.BindIndexBuffer(item.Mesh.Indices);
            commands.Push(new Push { Vertices = vertices[i], Draw = draw, Scene = sceneAddress });
            commands.DrawIndexed(item.Mesh.IndexCount);
        }
        commands.EndRendering();
        if (particles.Glowing > 0)
        {
            // Glowing particles (only they are in the list then): their shapes blended into the
            // source, far to near, then their owner records where they cover half a pixel.
            var push = new ParticlePush { Quads = particles.Address, Scene = sceneAddress };
            commands.BeginRendering(new ColorAttachment(glow.Source), new DepthAttachment(glow.Depth, LoadAction.Load, 0f, Store: true));
            commands.BindPipeline(ParticleGlowPipeline);
            commands.Push(push);
            commands.Draw(particles.Glowing * 6);
            commands.EndRendering();
            commands.BeginRendering(new ColorAttachment(glow.Owner), new DepthAttachment(glow.Depth, LoadAction.Load, 0f));
            commands.BindPipeline(ParticleOwnerPipeline);
            commands.Push(push);
            commands.Draw(particles.Glowing * 6);
            commands.EndRendering();
        }

        void Pass(Pipeline pipeline, GpuImage source, GpuImage destination, Action push)
        {
            commands.Transition(source, VkImageLayout.ShaderReadOnlyOptimal);
            commands.BeginRendering(new ColorAttachment(destination, LoadAction.DontCare));
            commands.BindPipeline(pipeline);
            push();
            commands.Draw(3);
            commands.EndRendering();
        }
        var blur = FullscreenPipeline("glow_blur.frag", VkFormat.R16G16B16A16SFloat);
        foreach (var (sigma, result) in new[] { (style.GlowWide * scale, glow.Wide), (style.GlowCore * scale, glow.Core) })
        {
            var (whole, fraction) = BoxOf(sigma);
            GpuImage[] chain = [glow.Source, glow.BlurA, glow.BlurB, glow.BlurA, glow.BlurB, glow.BlurA, result];
            for (var k = 0; k < 6; k++)
            {
                var push = new BlurPush { Source = (uint)chain[k].BindlessIndex, Radius = whole, Fraction = fraction, Horizontal = k < 3 ? 1 : 0, Size = size };
                Pass(blur, chain[k], chain[k + 1], () => commands.Push(push));
            }
        }
        var reach = 3 * (BoxOf(style.GlowWide * scale).Whole + 1) + 1;
        var owner = FullscreenPipeline("glow_owner.frag", RenderTarget3D.SurfaceFormat);
        Pass(owner, glow.Owner, glow.OwnerA, () => commands.Push(new OwnerPush { Source = (uint)glow.Owner.BindlessIndex, Reach = reach, Horizontal = 1, Size = size }));
        Pass(owner, glow.OwnerA, glow.OwnerB, () => commands.Push(new OwnerPush { Source = (uint)glow.OwnerA.BindlessIndex, Reach = reach, Horizontal = 0, Size = size }));
        foreach (var image in new[] { glow.Wide, glow.Core, glow.OwnerB, target.Surface })
            commands.Transition(image, VkImageLayout.ShaderReadOnlyOptimal);
        Pass(FullscreenPipeline("glow_composite.frag", RenderTarget3D.ColorFormat), target.Resolved, glow.Composite, () => commands.Push(new CompositePush
        {
            Scene = (uint)target.Resolved.BindlessIndex,
            Wide = (uint)glow.Wide.BindlessIndex,
            Core = (uint)glow.Core.BindlessIndex,
            Owner = (uint)glow.OwnerB.BindlessIndex,
            Surfaces = (uint)target.Surface.BindlessIndex,
            Boost = style.GlowBoost,
        }));
        commands.Transition(glow.Composite, VkImageLayout.TransferSrcOptimal);
        commands.Transition(target.Resolved, VkImageLayout.TransferDstOptimal);
        commands.CopyImage(glow.Composite, target.Resolved, 0, 0, target.Width, target.Height);
    }

    /// <summary>
    /// The sun's view for the shadow map: orthographic along the light, fitted around the receivers
    /// the camera sees (widened by how far a shadow's soft edge can reach) and deep enough for every
    /// caster. Null when nothing receives shadows in view.
    /// </summary>
    static (Matrix4x4 View, Matrix4x4 Projection, Vector2 Size, float Depth, float Far)? FitShadow(IReadOnlyList<DrawItem> items, Camera camera, float aspect, Vector3 toward, float tanHalfAngle)
    {
        var up = MathF.Abs(toward.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitZ;
        var view = Matrix4x4.CreateLookAt(Vector3.Zero, -toward, up);
        Vector3 casterMin = new(float.MaxValue), casterMax = new(float.MinValue);
        Vector3 receiverMin = new(float.MaxValue), receiverMax = new(float.MinValue);
        Vector3 worldMin = new(float.MaxValue), worldMax = new(float.MinValue);
        foreach (var item in items)
        {
            if (!item.CastsShadow && !item.ReceivesShadows)
                continue;
            var (low, high) = item.Skin is { } skin ? (skin.BoundsMin, skin.BoundsMax) : (item.Mesh.BoundsMin, item.Mesh.BoundsMax);
            for (var corner = 0; corner < 8; corner++)
            {
                var local = new Vector3((corner & 1) == 0 ? low.X : high.X, (corner & 2) == 0 ? low.Y : high.Y, (corner & 4) == 0 ? low.Z : high.Z);
                var world = Vector3.Transform(local, item.Model);
                var light = Vector3.Transform(world, view);
                worldMin = Vector3.Min(worldMin, world);
                worldMax = Vector3.Max(worldMax, world);
                if (item.CastsShadow)
                    (casterMin, casterMax) = (Vector3.Min(casterMin, light), Vector3.Max(casterMax, light));
                if (item.ReceivesShadows)
                    (receiverMin, receiverMax) = (Vector3.Min(receiverMin, light), Vector3.Max(receiverMax, light));
            }
        }
        if (receiverMin.X > receiverMax.X)
            return null;
        // The camera's view of the scene's depth: its frustum between the nearest and farthest the
        // scene's bounds reach from it, in the light's view.
        var cameraView = camera.View;
        float nearest = float.MaxValue, farthest = 0;
        for (var corner = 0; corner < 8; corner++)
        {
            var world = new Vector3((corner & 1) == 0 ? worldMin.X : worldMax.X, (corner & 2) == 0 ? worldMin.Y : worldMax.Y, (corner & 4) == 0 ? worldMin.Z : worldMax.Z);
            var depth = -Vector3.Transform(world, cameraView).Z;
            nearest = MathF.Min(nearest, depth);
            farthest = MathF.Max(farthest, depth);
        }
        nearest = MathF.Max(nearest, camera.Near);
        farthest = MathF.Min(farthest, camera.Far);
        Matrix4x4.Invert(cameraView, out var cameraToWorld);
        var tanY = MathF.Tan(camera.VerticalFieldOfView / 2);
        Vector2 seenMin = new(float.MaxValue), seenMax = new(float.MinValue);
        foreach (var depth in new[] { nearest, farthest })
        {
            var half = camera.Projection == Projection.Orthographic
                ? new Vector2(camera.OrthographicHeight / 2 * aspect, camera.OrthographicHeight / 2)
                : new Vector2(tanY * aspect, tanY) * depth;
            for (var corner = 0; corner < 4; corner++)
            {
                var point = new Vector3((corner & 1) == 0 ? -half.X : half.X, (corner & 2) == 0 ? -half.Y : half.Y, -depth);
                var light = Vector3.Transform(Vector3.Transform(point, cameraToWorld), view);
                seenMin = Vector2.Min(seenMin, new Vector2(light.X, light.Y));
                seenMax = Vector2.Max(seenMax, new Vector2(light.X, light.Y));
            }
        }
        // The maps for directions off the sun's centre are sheared by distance times the half angle
        // (distances from the light's view plane through the origin).
        var reach = MathF.Max(MathF.Abs(casterMin.Z), MathF.Max(MathF.Abs(casterMax.Z), MathF.Max(MathF.Abs(receiverMin.Z), MathF.Abs(receiverMax.Z)))) * tanHalfAngle;
        var min = Vector2.Max(seenMin, new Vector2(receiverMin.X, receiverMin.Y)) - new Vector2(reach);
        var max = Vector2.Min(seenMax, new Vector2(receiverMax.X, receiverMax.Y)) + new Vector2(reach);
        if (min.X >= max.X || min.Y >= max.Y)
            return null;
        // Light-view depth: the light looks down −Z; distances from the casters nearest it to the farthest receiver.
        float near = -MathF.Max(casterMax.Z, receiverMax.Z), far = -MathF.Min(casterMin.Z, receiverMin.Z);
        near -= 0.01f * (far - near) + 1e-3f;
        far += 0.01f * (far - near) + 1e-3f;
        var size = max - min;
        // Reversed depth, as the camera's: 1 nearest the light; Y flipped for Vulkan.
        var projection = new Matrix4x4(
            2 / size.X, 0, 0, 0,
            0, -2 / size.Y, 0, 0,
            0, 0, 1 / (far - near), 0,
            -(max.X + min.X) / size.X, (max.Y + min.Y) / size.Y, far / (far - near), 1);
        return (view, projection, size, far - near, far);
    }

    /// <summary>
    /// Clears the target to <paramref name="background"/> (linear), draws the items seen by
    /// <paramref name="camera"/> under <paramref name="sun"/>, resolves, and draws the ink lines of
    /// the items that have them at <paramref name="scale"/> (the device scale) times the style's widths.
    /// </summary>
    public void Render(Frame frame, RenderTarget3D target, Camera camera, IReadOnlyList<DrawItem> items, Vector4 background, SunLight sun, CelStyle? style = null, float scale = 1,
        float time = 0, IReadOnlyList<Particle>? particles = null)
    {
        style ??= CelStyle.Default;
        var commands = frame.Commands;
        // Particles in front of the camera, far to near; the glowing ones also on their own.
        var quads = InkCoverageOnly || particles is null ? [] : particles.Select(p => ParticleQuad.Of(p, camera, target.Height, scale))
            .Where(q => q.Opacity > 0 && q.Depth > camera.Near).OrderByDescending(q => q.Depth).ToArray();
        ParticleData Data(ParticleQuad q) => new()
        {
            Centre = q.Centre, Pixel = q.Pixel, AxisU = q.AxisU, HalfU = q.HalfU, AxisV = q.AxisV, HalfV = q.HalfV, Color = q.Color,
            Shape = (uint)q.Shape, P0 = q.P0, P1 = q.P1, Outline = q.Outline, Opacity = q.Opacity, Depth = q.Depth, Glow = q.Glow ? 1u : 0,
        };
        var glowingQuads = quads.Where(q => q.Glow).ToArray();
        var particleAddress = quads.Length > 0 ? frame.Upload<ParticleData>(quads.Select(Data).ToArray()) : 0;
        var glowingAddress = glowingQuads.Length > 0 ? frame.Upload<ParticleData>(glowingQuads.Select(Data).ToArray()) : 0;
        var vertices = Skin(frame, items);
        var aspect = (float)target.Width / target.Height;
        var orthographic = camera.Projection == Projection.Orthographic;
        var direction = sun.Direction.LengthSquared() > 0 ? Vector3.Normalize(sun.Direction) : Vector3.Zero;
        var tanHalfAngle = MathF.Tan(sun.Angle / 2 * MathF.PI / 180);
        var scene = new SceneData
        {
            ViewProjection = camera.ViewProjection(aspect),
            View = camera.View,
            Sun = new Vector4(direction, sun.Strength / MathF.PI),
            Band = style.BandEdge,
            Orthographic = orthographic ? 1u : 0u,
            ShadowMap = uint.MaxValue,
            Ink = Colors.ToLinear(style.Ink),
            Viewport = new Vector2(target.Width, target.Height),
            // Lines run at the hatch angle (clockwise from the right, Y down); their normal is a quarter turn on.
            HatchNormal = new Vector2(-MathF.Sin(style.HatchAngle * MathF.PI / 180), MathF.Cos(style.HatchAngle * MathF.PI / 180)),
            HatchSpacing = style.HatchSpacing * scale,
            HatchWidth = style.HatchWidth * scale,
            HatchInk = style.HatchInk,
            Time = time,
        };
        if (direction != Vector3.Zero && sun.Strength > 0 && FitShadow(items, camera, aspect, direction, tanHalfAngle) is { } fit)
        {
            var lightViewProjection = fit.View * fit.Projection;
            commands.BeginRendering([], new DepthAttachment(target.ShadowMap, LoadAction.Clear, 0f));
            commands.BindPipeline(ShadowPipeline);
            var draws = Enumerable.Range(0, items.Count).Where(i => items[i].CastsShadow)
                .Select(i => (items[i].Mesh, Vertices: vertices[i], Draw: frame.Upload<DrawData>([new DrawData { Model = items[i].Model, NormalMatrix = items[i].Model }]))).ToList();
            for (var tile = 0; tile < SunDisc.Length; tile++)
            {
                // Seen from a point of the sun's disc, the scene shifts by its distance times the
                // offset: a shear of the centre's view (x' = x − u·tan·z, z running from the light).
                var u = SunDisc[tile] * tanHalfAngle;
                var shear = Matrix4x4.Identity;
                shear.M31 = -u.X;
                shear.M32 = -u.Y;
                var shadowAddress = frame.Upload<SceneData>([scene with { ViewProjection = fit.View * shear * fit.Projection, View = fit.View }]);
                int x = tile % RenderTarget3D.ShadowTiles * RenderTarget3D.ShadowSize, y = tile / RenderTarget3D.ShadowTiles * RenderTarget3D.ShadowSize;
                commands.SetViewport(x, y, RenderTarget3D.ShadowSize, RenderTarget3D.ShadowSize);
                commands.SetScissor(x, y, RenderTarget3D.ShadowSize, RenderTarget3D.ShadowSize);
                foreach (var (mesh, shadowVertices, draw) in draws)
                {
                    commands.BindIndexBuffer(mesh.Indices);
                    commands.Push(new Push { Vertices = shadowVertices, Draw = draw, Scene = shadowAddress });
                    commands.DrawIndexed(mesh.IndexCount);
                }
            }
            commands.EndRendering();
            commands.Transition(target.ShadowMap, VkImageLayout.ShaderReadOnlyOptimal);
            scene.LightViewProjection = lightViewProjection;
            // Shadow-map units per world unit: across, up (V runs down) and toward the light; the
            // sun's half angle as a tangent.
            scene.ShadowScale = new Vector4(1 / fit.Size.X, -1 / fit.Size.Y, 1 / fit.Depth, tanHalfAngle);
            scene.ShadowMap = (uint)target.ShadowMap.BindlessIndex;
            scene.ShadowTexel = MathF.Max(fit.Size.X, fit.Size.Y) / RenderTarget3D.ShadowSize;
            scene.ShadowFar = fit.Far;
        }
        var sceneAddress = frame.Upload<SceneData>([scene]);
        commands.BeginRendering(
            [
                new ColorAttachment(target.Color, LoadAction.Clear, background, target.Resolved, Store: false),
                new ColorAttachment(target.Surface, LoadAction.Clear, Vector4.Zero),
            ],
            new DepthAttachment(target.Depth, LoadAction.Clear, 0f, Store: quads.Length > 0));
        commands.SetViewport(0, 0, target.Width, target.Height);
        commands.SetScissor(0, 0, target.Width, target.Height);
        // Model ids for the surface records, by group; past the 10 bits they repeat.
        var groups = new Dictionary<object, uint>(ReferenceEqualityComparer.Instance);
        uint next = 0;
        uint NewModel() => next++ % MaxModels + 1;
        var models = items.Select(item => item.Group is null ? NewModel() : groups.TryGetValue(item.Group, out var known) ? known : groups[item.Group] = NewModel()).ToArray();
        var ink = false;
        var drawAddresses = new ulong[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var model = models[index];
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
                ReceivesShadows = item.ReceivesShadows ? 1u : 0,
                Anchor = item.Anchor,
                Hatch = item.Hatch ? 1u : 0,
                Extra = item.Terrain is { } terrain ? UploadTerrain(frame, terrain) : 0,
            }]);
            drawAddresses[index] = draw;
            commands.BindIndexBuffer(item.Mesh.Indices);
            commands.Push(new Push { Vertices = vertices[index], Draw = draw, Scene = sceneAddress });
            commands.DrawIndexed(item.Mesh.IndexCount);
        }
        commands.EndRendering();

        if (!InkCoverageOnly && (items.Any(i => i.Glow != Vector3.Zero) || glowingQuads.Length > 0))
            RenderGlow(frame, target, items, vertices, models, sceneAddress, style, scale, (glowingAddress, quads.Length, glowingQuads.Length));

        var outline = style.OutlineWidth * scale;
        var inner = style.InnerWidth * scale;
        if (InkCoverageOnly)
        {
            commands.BeginRendering(new ColorAttachment(target.Resolved, LoadAction.Clear, new Vector4(0, 0, 0, 1)));
            commands.EndRendering();
        }
        if (ink && (outline > 0 || inner > 0))
            RenderInk(frame, target, camera, items, vertices, drawAddresses, sceneAddress, style, outline, inner);
        if (quads.Length == 0)
            return;
        // The particles over everything, ink lines included: blended into their own layer against the
        // scene's depth, resolved, and laid over the view.
        var (layer, resolved) = target.Particles;
        commands.BeginRendering(new ColorAttachment(layer, LoadAction.Clear, Vector4.Zero, resolved, Store: false),
            new DepthAttachment(target.Depth, LoadAction.Load, 0f));
        commands.SetViewport(0, 0, target.Width, target.Height);
        commands.SetScissor(0, 0, target.Width, target.Height);
        commands.BindPipeline(ParticlePipeline);
        commands.Push(new ParticlePush { Quads = particleAddress, Scene = sceneAddress });
        commands.Draw(quads.Length * 6);
        commands.EndRendering();
        commands.Transition(resolved, VkImageLayout.ShaderReadOnlyOptimal);
        commands.BeginRendering(new ColorAttachment(target.Resolved));
        commands.BindPipeline(ParticleCompositePipeline);
        commands.Push((uint)resolved.BindlessIndex);
        commands.Draw(3);
        commands.EndRendering();
    }

    /// <summary>The ink lines (Docs/Renderer-3D.md, "Ink lines"): feature edges, the edges pass, the lines over the view.</summary>
    void RenderInk(Frame frame, RenderTarget3D target, Camera camera, IReadOnlyList<DrawItem> items, ulong[] vertices, ulong[] drawAddresses, ulong sceneAddress,
        CelStyle style, float outline, float inner)
    {
        var commands = frame.Commands;
        var aspect = (float)target.Width / target.Height;
        var orthographic = camera.Projection == Projection.Orthographic;
        // Feature edges: the meshes' creases and open edges, each pixel near one recording its nearest point.
        var creaseCos = MathF.Cos((180 - style.CreaseAngle) * MathF.PI / 180);
        commands.BeginRendering(new ColorAttachment(target.Features, LoadAction.Clear, Vector4.Zero), new DepthAttachment(target.FeatureDepth, LoadAction.Clear, 0f));
        commands.BindPipeline(FeaturePipeline);
        for (var index = 0; index < items.Count; index++)
            if (items[index] is { Ink: true, Features: { } features })
            {
                commands.Push(new FeaturePush { Vertices = vertices[index], Edges = features.Buffer.Address, Draw = drawAddresses[index], Scene = sceneAddress, CreaseCos = creaseCos });
                commands.Draw(features.Count * 6);
            }
        commands.EndRendering();
        commands.Transition(target.Features, VkImageLayout.ShaderReadOnlyOptimal);
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
            CreaseCos = creaseCos,
            Features = (uint)target.Features.BindlessIndex,
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
