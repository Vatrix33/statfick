using System.Numerics;
using System.Text.Json;
using Statventure.Render3D;

namespace Statventure.Tests.Render3D;

/// <summary>
/// The glTF reader: Blender's test models (Data/Models, from Tools/BlenderReference/make_models.py)
/// read as they were built, and a hand-written file covers what Blender's export doesn't use
/// (interleaved and quantized attributes, 8-bit indices, strips, fans, sparse accessors, missing
/// normals, matrix nodes).
/// </summary>
public class GltfImportTests
{
    static string ModelPath(string file) => Path.Combine(Render3DReferenceTests.ModelDirectory, file);

    static void Near(Vector3 expected, Vector3 actual, float tolerance = 1e-5f) =>
        Assert.True(Vector3.Distance(expected, actual) <= tolerance, $"expected {expected}, got {actual}");

    [Fact]
    public void BlenderModelHasItsHierarchy()
    {
        var model = ModelData.Load(ModelPath("blocks.glb"));
        int Find(string name) => model.Nodes.Select((n, i) => (n, i)).Single(x => x.n.Name == name).i;
        var stand = Find("Stand");
        Assert.Equal([stand], model.Roots);
        Assert.Equal(["Banner", "Base", "BlockA", "BlockB"], model.Nodes[stand].Children.Select(c => model.Nodes[c].Name).Order());
        Assert.Equal(Find("Base"), model.Nodes[Find("Pillar")].Parent);
        Assert.Equal(Find("Pillar"), model.Nodes[Find("Cap")].Parent);
        // BlockA has the custom property ink = 0 (no ink lines), exported as extras; the rest have lines.
        Assert.Equal(["BlockA"], model.Nodes.Where(n => !n.Ink).Select(n => n.Name));
        // One mesh, two nodes: the second mirrored, so its triangles wind the other way.
        Assert.Equal(model.Nodes[Find("BlockA")].Mesh, model.Nodes[Find("BlockB")].Mesh);
        var world = model.WorldMatrices();
        Assert.True(world[Find("BlockA")].GetDeterminant() > 0);
        Assert.True(world[Find("BlockB")].GetDeterminant() < 0);
        // The stand is scaled 1.2 and turned 15° about Blender's Z, glTF's Y.
        Near(new Vector3(1.2f * MathF.Cos(MathF.PI / 12), 0, -1.2f * MathF.Sin(MathF.PI / 12)), Vector3.TransformNormal(Vector3.UnitX, world[stand]));
        // Blender's +Z is glTF's +Y: the cap sits on top of the pillar, both above the slab.
        Assert.True(world[Find("Cap")].Translation.Y > world[Find("Pillar")].Translation.Y);
    }

    /// <summary>
    /// A stage exported as make_models.py's export_stage exports one (Docs/Scene.md, step 3): its
    /// cameras (the exporter's vertical angle for a perspective camera with sensor fit Vertical; an
    /// orthographic one's height as its scale, the export having put its magnifications right), its sun
    /// (Blender's strength, the angle the export copied into the light's extras; the file lists
    /// KHR_lights_punctual as required), the nodes carrying them, and the timeline as one clip.
    /// </summary>
    [Fact]
    public void BlenderStageHasItsCamerasAndSun()
    {
        var path = ModelPath("dolly.glb");
        using (var document = JsonDocument.Parse(GltfJson(path)))
            Assert.Contains("KHR_lights_punctual", document.RootElement.GetProperty("extensionsRequired"u8).EnumerateArray().Select(e => e.GetString()));
        var model = ModelData.Load(path);
        int Find(string name) => model.Nodes.Select((n, i) => (n, i)).Single(x => x.n.Name == name).i;
        Assert.Equal(2, model.Cameras.Count);
        var lens = model.Cameras[model.Nodes[Find("Camera")].Camera];
        Assert.Equal(("Lens", Projection.Perspective), (lens.Name, lens.Projection));
        // Lens 35 mm on a 24 mm sensor, fitted vertically: 2·atan(12 / 35).
        Assert.Equal(2 * Math.Atan(12.0 / 35), lens.VerticalFieldOfView, 1e-6);
        Assert.Equal((0.1f, 40f), (lens.Near, lens.Far));
        var plan = model.Cameras[model.Nodes[Find("Plan")].Camera];
        Assert.Equal(("Plan", Projection.Orthographic, 6f, 0.5f, 30f), (plan.Name, plan.Projection, plan.OrthographicHeight, plan.Near, plan.Far));
        var sun = Assert.Single(model.Lights);
        Assert.Equal("Daylight", sun.Name);
        Assert.Equal(2.5f, sun.Strength);
        Assert.Equal(4f, sun.Angle, 1e-5f);
        Assert.Equal(0, model.Nodes[Find("Sun")].Light);
        // Only those nodes carry a camera or the light.
        Assert.Equal(["Camera", "Plan", "Sun"], model.Nodes.Where(n => n.Camera >= 0 || n.Light >= 0).Select(n => n.Name).Order());
        Assert.All(model.Nodes.Where(n => n.Name != "Sun"), n => Assert.Equal(-1, n.Light));
        // The timeline is one clip, named after Blender's scene, on the dolly and the camera (the rest is still).
        var clip = Assert.Single(model.Animations);
        Assert.Equal("Scene", clip.Name);
        Assert.Equal(["Camera", "Dolly"], clip.Channels.Select(c => model.Nodes[c.Node].Name).Distinct().Order());
        model.Validate();
    }

    /// <summary>A .glb's JSON chunk.</summary>
    internal static byte[] GltfJson(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return bytes[20..(20 + BitConverter.ToInt32(bytes, 12))];
    }

    [Fact]
    public void BlenderModelHasItsMaterials()
    {
        var model = ModelData.Load(ModelPath("blocks.glb"));
        var colors = new Dictionary<string, string>
        {
            ["stone"] = "#6b6258", ["cloth"] = "#7b5a3a", ["metal.base"] = "#8a8f96",
            ["metal.trim"] = "#c9a46a", ["cloth.001"] = "#5d2f4e", ["glow"] = "#f2d488",
        };
        Assert.Equal(colors.Keys.Order(), model.Materials.Select(m => m.Name).Order());
        foreach (var material in model.Materials)
        {
            var expected = Colors.LinearFromHex(colors[material.Name]);
            Assert.True(Vector4.Distance(expected, material.BaseColor) < 1e-5f, $"{material.Name}: {material.BaseColor}");
            // Only the banner's material shows its back faces (Blender's backface culling off).
            Assert.Equal(material.Name == "cloth.001", material.DoubleSided);
        }
        Assert.Equal("cloth", model.Materials.Single(m => m.Name == "cloth.001").Slot);
        Assert.Equal("metal.base", model.Materials.Single(m => m.Name == "metal.base").Slot);

        // The slab: one mesh, two primitives; the top (+Y) in cloth, the rest in stone.
        var slab = model.Meshes.Single(m => m.Name == "slab");
        Assert.Equal(2, slab.Primitives.Count);
        var top = slab.Primitives.Single(p => model.MaterialOf(p).Name == "cloth");
        Assert.All(top.Normals, n => Near(Vector3.UnitY, n));
        Assert.Equal(2, top.Indices.Length / 3);
        foreach (var primitive in model.Meshes.SelectMany(m => m.Primitives))
        {
            Assert.NotNull(primitive.TexCoords);
            Assert.All(primitive.Normals, n => Assert.InRange(n.Length(), 0.999f, 1.001f));
            Assert.All(primitive.Indices, i => Assert.True(i < primitive.Positions.Length));
        }
    }

    [Fact]
    public void BlenderSkinHasItsJointsAndWeights()
    {
        var model = ModelData.Load(ModelPath("rigged.glb"));
        var skin = Assert.Single(model.Skins);
        Assert.Equal(["Lower", "Upper"], skin.Joints.Select(j => model.Nodes[j].Name));
        Assert.Equal(2, skin.InverseBindMatrices.Count);
        var tube = model.Nodes.Single(n => n.Name == "Tube");
        Assert.Equal(0, tube.Skin);
        var primitive = Assert.Single(model.Meshes[tube.Mesh].Primitives);
        Assert.NotNull(primitive.Joints);
        Assert.NotNull(primitive.Weights);
        for (var i = 0; i < primitive.Positions.Length; i++)
        {
            var w = primitive.Weights![i];
            Assert.InRange(w.X + w.Y + w.Z + w.W, 0.999f, 1.001f);
            var j = primitive.Joints![i];
            Assert.All(new[] { j.J0, j.J1, j.J2, j.J3 }, index => Assert.True(index < skin.Joints.Count));
        }
        // At rest every joint takes the mesh to the same place (Blender exports its rest pose),
        // which isn't the tube node's place: glTF ignores a skinned mesh's own transform.
        var world = model.WorldMatrices();
        var placements = skin.Joints.Select((joint, j) => skin.InverseBindMatrices[j] * world[joint]).ToArray();
        foreach (var placement in placements)
            Near(Vector3.Transform(new Vector3(0.2f, 1.5f, 0), placements[0]), Vector3.Transform(new Vector3(0.2f, 1.5f, 0), placement));
        Near(new Vector3(0.2f, 0, 0), world[model.Nodes.ToList().IndexOf(tube)].Translation);
    }

    [Fact]
    public void BlenderShadersMarkGlowAndUnlit()
    {
        var model = ModelData.Load(ModelPath("glowing.glb"));
        ModelMaterial Named(string name) => model.Materials.Single(m => m.Name == name);
        // Principled BSDF with emission: cel-shaded, glowing.
        var blade = Named("metal.base");
        Assert.True(Vector3.Distance(new Vector3(Colors.LinearFromHex("#bffff0").X, Colors.LinearFromHex("#bffff0").Y, Colors.LinearFromHex("#bffff0").Z), blade.Emission) < 1e-5f);
        Assert.False(blade.Unlit || blade.EmissionOnly);
        // An Emission shader alone: no base colour, only emission.
        Assert.True(Named("badge").EmissionOnly);
        // A Background shader: unlit, no glow.
        var pupil = Named("pupil");
        Assert.True(pupil.Unlit);
        Assert.Equal(Vector3.Zero, pupil.Emission);
        // hatch = 0 on a material (its extras).
        Assert.Equal(["metal.base"], ModelData.Load(ModelPath("shapes.glb")).Materials.Where(m => !m.Hatch).Select(m => m.Name));
    }

    [Theory]
    [InlineData(8f, 7, 0.46875f)]
    [InlineData(2.5f, 2, 0.0301205f)]
    public void GlowBlursAsPillowsGaussianBlur(float sigma, int whole, float fraction)
    {
        // Pillow's BoxBlur.c _gaussian_blur_radius for three passes, which the lizardman's compose.py used.
        var box = Renderer3D.BoxOf(sigma);
        Assert.Equal(whole, box.Whole);
        Assert.Equal(fraction, box.Fraction, 1e-5f);
    }

    [Theory]
    [InlineData("tube-1", "tube-1")]
    [InlineData("metal.base", "metal.base")]
    [InlineData("metal.base.001", "metal.base")]
    [InlineData("cloth.012", "cloth")]
    [InlineData("cloth.12", "cloth.12")]
    [InlineData(".001", ".001")]
    public void MaterialNamesNameSlots(string name, string slot) => Assert.Equal(slot, Palette.SlotOf(name));

    [Fact]
    public void PaletteSlotsColourMaterials()
    {
        var palette = new Palette([new("cloth", "#3f4f8f")]);
        Assert.Equal(Colors.LinearFromHex("#3f4f8f"), palette.Resolve(new ModelMaterial("cloth.001", Vector4.One, false)));
        var glow = new Vector4(0.9f, 0.6f, 0.2f, 1);
        Assert.Equal(glow, palette.Resolve(new ModelMaterial("glow", glow, false)));
    }

    [Fact]
    public void DecodesWhatTheSpecificationAllows()
    {
        var path = HandWritten();
        try
        {
            var model = ModelData.Load(path);
            var primitives = Assert.Single(model.Meshes).Primitives;
            // The line primitive is skipped.
            Assert.Equal(2, primitives.Count);

            // Interleaved (stride 20) float positions, normalized unsigned short UVs and byte normals; a strip through 8-bit indices.
            var strip = primitives[0];
            Assert.Equal([new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0)], strip.Positions);
            Assert.Equal([new(0, 0), new(1, 0), new(1, 32768 / 65535f), new(0, 1)], strip.TexCoords!);
            Assert.Equal([new(0, 0, 1), new(0, 0, 1), new(0, -1, 0), new(0, 0, -1)], strip.Normals);
            // Indices 1 0 3 2 as a strip: (1, 0, 3), then the second triangle's order swapped: (0, 2, 3).
            Assert.Equal([1u, 0, 3, 0, 2, 3], strip.Indices);
            var material = model.MaterialOf(strip);
            Assert.Equal(("cloth.001", "cloth", true), (material.Name, material.Slot, material.DoubleSided));
            Assert.Equal(new Vector4(0.5f, 0.25f, 0.125f, 1), material.BaseColor);

            // A fan without indices or normals, its positions all sparse substitutions over zeros:
            // triangles (1, 2, 0) and (2, 3, 0), each with its own vertices and flat normal.
            var fan = primitives[1];
            Assert.Equal(-1, fan.Material);
            Assert.Same(ModelMaterial.Default, model.MaterialOf(fan));
            Assert.Equal([new(1, 0, 0), new(1, 1, 0), new(0, 0, 0), new(1, 1, 0), new(0, 1, 0), new(0, 0, 0)], fan.Positions);
            Assert.All(fan.Normals, n => Near(Vector3.UnitZ, n));
            Assert.Equal([0u, 1, 2, 3, 4, 5], fan.Indices);

            // A matrix node (translation 2, 3, 4) over a node with translation, rotation (90° about Z) and scale 2.
            // The child's extras say ink: false; the root's say nothing about ink.
            Assert.Equal((true, false), (model.Nodes[0].Ink, model.Nodes[1].Ink));
            var world = model.WorldMatrices();
            Near(new Vector3(3, 5, 4), Vector3.Transform(Vector3.UnitX, world[1]));
            Assert.Equal(new Vector3(2, 2, 2), model.Nodes[1].Scale);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void RefusesExtensionsItCantRead()
    {
        var path = HandWritten(required: "KHR_draco_mesh_compression");
        try
        {
            var error = Assert.Throws<NotSupportedException>(() => ModelData.Load(path));
            Assert.Contains("KHR_draco_mesh_compression", error.Message);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    /// <summary>A .gltf with its buffer as a data URI, in a new temporary folder.</summary>
    static string HandWritten(string? required = null)
    {
        var data = new MemoryStream();
        var bytes = new BinaryWriter(data);
        // View 0: 4 vertices of 20 bytes: position (3 floats), UV (2 normalized ushorts), normal (3 normalized bytes + 1 padding).
        Vector3[] positions = [new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0)];
        (ushort, ushort)[] uvs = [(0, 0), (65535, 0), (65535, 32768), (0, 65535)];
        (sbyte, sbyte, sbyte)[] normals = [(0, 0, 127), (0, 0, 127), (0, -127, 0), (0, 0, -128)];
        for (var i = 0; i < 4; i++)
        {
            bytes.Write(positions[i].X);
            bytes.Write(positions[i].Y);
            bytes.Write(positions[i].Z);
            bytes.Write(uvs[i].Item1);
            bytes.Write(uvs[i].Item2);
            bytes.Write(normals[i].Item1);
            bytes.Write(normals[i].Item2);
            bytes.Write(normals[i].Item3);
            bytes.Write((byte)0);
        }
        // View 1 (offset 80): 8-bit indices.
        bytes.Write([1, 0, 3, 2]);
        // View 2 (offset 84): sparse indices (unsigned short) 1 2 3, padded to 4 bytes.
        foreach (ushort index in new ushort[] { 1, 2, 3, 0 })
            bytes.Write(index);
        // View 3 (offset 92): their positions.
        foreach (var v in new Vector3[] { new(1, 0, 0), new(1, 1, 0), new(0, 1, 0) })
        {
            bytes.Write(v.X);
            bytes.Write(v.Y);
            bytes.Write(v.Z);
        }
        bytes.Flush();
        var buffer = data.ToArray();
        var gltf = new Dictionary<string, object?>
        {
            ["asset"] = new { version = "2.0" },
            ["extensionsUsed"] = required == null ? new[] { "KHR_mesh_quantization" } : new[] { "KHR_mesh_quantization", required },
            ["extensionsRequired"] = required == null ? new[] { "KHR_mesh_quantization" } : new[] { "KHR_mesh_quantization", required },
            ["scene"] = 0,
            ["scenes"] = new[] { new { nodes = new[] { 0 } } },
            ["nodes"] = new object[]
            {
                new { name = "root", matrix = new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 2, 3, 4, 1 }, children = new[] { 1 }, extras = new { note = "x" } },
                new { name = "child", translation = new float[] { 1, 0, 0 }, rotation = new[] { 0, 0, MathF.Sqrt(0.5f), MathF.Sqrt(0.5f) }, scale = new float[] { 2, 2, 2 }, mesh = 0, extras = new { ink = false } },
            },
            ["meshes"] = new[]
            {
                new
                {
                    primitives = new object[]
                    {
                        new { attributes = new Dictionary<string, int> { ["POSITION"] = 0, ["TEXCOORD_0"] = 1, ["NORMAL"] = 2 }, indices = 3, mode = 5, material = 0 },
                        new { attributes = new Dictionary<string, int> { ["POSITION"] = 4 }, mode = 6 },
                        new { attributes = new Dictionary<string, int> { ["POSITION"] = 0 }, mode = 1 },
                    },
                },
            },
            ["materials"] = new[]
            {
                new { name = "cloth.001", doubleSided = true, pbrMetallicRoughness = new { baseColorFactor = new[] { 0.5f, 0.25f, 0.125f, 1 } } },
            },
            ["accessors"] = new object[]
            {
                new { bufferView = 0, componentType = 5126, count = 4, type = "VEC3", min = new float[] { 0, 0, 0 }, max = new float[] { 1, 1, 0 } },
                new { bufferView = 0, byteOffset = 12, componentType = 5123, normalized = true, count = 4, type = "VEC2" },
                new { bufferView = 0, byteOffset = 16, componentType = 5120, normalized = true, count = 4, type = "VEC3" },
                new { bufferView = 1, componentType = 5121, count = 4, type = "SCALAR" },
                new
                {
                    componentType = 5126, count = 4, type = "VEC3", min = new float[] { 0, 0, 0 }, max = new float[] { 1, 1, 0 },
                    sparse = new { count = 3, indices = new { bufferView = 2, componentType = 5123 }, values = new { bufferView = 3 } },
                },
            },
            ["bufferViews"] = new object[]
            {
                new { buffer = 0, byteOffset = 0, byteLength = 80, byteStride = 20, target = 34962 },
                new { buffer = 0, byteOffset = 80, byteLength = 4, target = 34963 },
                new { buffer = 0, byteOffset = 84, byteLength = 6 },
                new { buffer = 0, byteOffset = 92, byteLength = 36 },
            },
            ["buffers"] = new[] { new { byteLength = buffer.Length, uri = "data:application/octet-stream;base64," + Convert.ToBase64String(buffer) } },
        };
        var folder = Directory.CreateTempSubdirectory("statventure-gltf-");
        var path = Path.Combine(folder.FullName, "hand-written.gltf");
        File.WriteAllText(path, JsonSerializer.Serialize(gltf));
        return path;
    }
}
