using Brutal.VulkanApi;

namespace Statventure.Graphics;

/// <summary>The engine's samplers, by their index in the bindless sampler table.</summary>
public enum SamplerKind
{
    LinearClamp = 0,
    NearestClamp = 1,
    LinearRepeat = 2,
    NearestRepeat = 3,
    AnisotropicRepeat = 4,
    LinearMirror = 5,
}

/// <summary>
/// The bindless table: one descriptor set, bound for every draw, holding every sampled image (binding 0)
/// and the engine's samplers (binding 1). Shaders pick a texture and sampler by index:
/// <c>texture(sampler2D(Textures[nonuniformEXT(i)], Samplers[s]), uv)</c>. Index 0 is a 1×1 white texture.
/// </summary>
public sealed unsafe class Bindless : IDisposable
{
    public const int MaxTextures = 16384;
    public const int MaxSamplers = 16;

    readonly GpuDevice _device;
    readonly Stack<int> _free = new();
    int _next;
    readonly VkSampler[] _samplers;

    public VkDescriptorSetLayout Layout { get; }
    public VkDescriptorPool Pool { get; }
    public VkDescriptorSet Set { get; }
    public int Count => _next - _free.Count;
    /// <summary>A 1×1 white texture at index 0.</summary>
    public GpuImage? White { get; private set; }

    internal Bindless(GpuDevice device)
    {
        _device = device;
        var d = device.Device;
        var bindings = stackalloc VkDescriptorSetLayoutBinding[2];
        bindings[0] = new VkDescriptorSetLayoutBinding { Binding = 0, DescriptorType = VkDescriptorType.SampledImage, DescriptorCount = MaxTextures, StageFlags = VkShaderStageFlags.All };
        bindings[1] = new VkDescriptorSetLayoutBinding { Binding = 1, DescriptorType = VkDescriptorType.Sampler, DescriptorCount = MaxSamplers, StageFlags = VkShaderStageFlags.All };
        var flags = stackalloc VkDescriptorBindingFlags[2];
        flags[0] = VkDescriptorBindingFlags.PartiallyBoundBit | VkDescriptorBindingFlags.UpdateAfterBindBit | VkDescriptorBindingFlags.UpdateUnusedWhilePendingBit;
        flags[1] = VkDescriptorBindingFlags.PartiallyBoundBit | VkDescriptorBindingFlags.UpdateAfterBindBit;
        var flagInfo = new VkDescriptorSetLayoutBindingFlagsCreateInfo { BindingCount = 2, BindingFlags = flags };
        Layout = d.CreateDescriptorSetLayout(new VkDescriptorSetLayoutCreateInfo
        {
            Next = &flagInfo,
            Flags = VkDescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
            BindingCount = 2,
            Bindings = bindings,
        }, null);
        var sizes = stackalloc VkDescriptorPoolSize[2];
        sizes[0] = new VkDescriptorPoolSize { Type = VkDescriptorType.SampledImage, DescriptorCount = MaxTextures };
        sizes[1] = new VkDescriptorPoolSize { Type = VkDescriptorType.Sampler, DescriptorCount = MaxSamplers };
        Pool = d.CreateDescriptorPool(new VkDescriptorPoolCreateInfo
        {
            Flags = VkDescriptorPoolCreateFlags.UpdateAfterBindBit,
            MaxSets = 1,
            PoolSizeCount = 2,
            PoolSizes = sizes,
        }, null);
        var layout = Layout;
        VkDescriptorSet set;
        d.AllocateDescriptorSets(new VkDescriptorSetAllocateInfo { DescriptorPool = Pool, DescriptorSetCount = 1, SetLayouts = &layout }, new ReadOnlySpan<VkDescriptorSet>(&set, 1));
        Set = set;

        _samplers =
        [
            CreateSampler(VkFilter.Linear, VkSamplerAddressMode.ClampToEdge, 1),
            CreateSampler(VkFilter.Nearest, VkSamplerAddressMode.ClampToEdge, 1),
            CreateSampler(VkFilter.Linear, VkSamplerAddressMode.Repeat, 1),
            CreateSampler(VkFilter.Nearest, VkSamplerAddressMode.Repeat, 1),
            CreateSampler(VkFilter.Linear, VkSamplerAddressMode.Repeat, Math.Min(8f, device.Limits.MaxSamplerAnisotropy)),
            CreateSampler(VkFilter.Linear, VkSamplerAddressMode.MirroredRepeat, 1),
        ];
        for (var i = 0; i < _samplers.Length; i++)
        {
            var info = new VkDescriptorImageInfo { Sampler = _samplers[i] };
            d.UpdateDescriptorSets([new VkWriteDescriptorSet { DstSet = Set, DstBinding = 1, DstArrayElement = i, DescriptorCount = 1, DescriptorType = VkDescriptorType.Sampler, ImageInfo = &info }], []);
        }
    }

    /// <summary>Creates the white texture (index 0); called once the device is fully built.</summary>
    internal void CreateDefaults()
    {
        White = GpuImage.CreateTexture(_device, 1, 1, [255, 255, 255, 255]);
    }

    VkSampler CreateSampler(VkFilter filter, VkSamplerAddressMode address, float anisotropy) =>
        _device.Device.CreateSampler(new VkSamplerCreateInfo
        {
            MagFilter = filter,
            MinFilter = filter,
            MipmapMode = filter == VkFilter.Nearest ? VkSamplerMipmapMode.Nearest : VkSamplerMipmapMode.Linear,
            AddressModeU = address,
            AddressModeV = address,
            AddressModeW = address,
            AnisotropyEnable = anisotropy > 1f,
            MaxAnisotropy = anisotropy,
            MaxLod = 1000f,
        }, null);

    /// <summary>Adds an image view to the table and returns its index.</summary>
    public int Register(VkImageView view)
    {
        int index;
        lock (_free)
        {
            if (_free.Count > 0)
                index = _free.Pop();
            else if (_next < MaxTextures)
                index = _next++;
            else
                throw new InvalidOperationException($"the bindless table is full ({MaxTextures} textures)");
        }
        var info = new VkDescriptorImageInfo { ImageView = view, ImageLayout = VkImageLayout.ShaderReadOnlyOptimal };
        _device.Device.UpdateDescriptorSets([new VkWriteDescriptorSet { DstSet = Set, DstBinding = 0, DstArrayElement = index, DescriptorCount = 1, DescriptorType = VkDescriptorType.SampledImage, ImageInfo = &info }], []);
        return index;
    }

    /// <summary>Frees an index (call only when the GPU no longer uses it; GpuImage defers this).</summary>
    public void Unregister(int index)
    {
        lock (_free)
            _free.Push(index);
    }

    public void Dispose()
    {
        White?.Dispose();
        _device.Collect();
        var d = _device.Device;
        foreach (var sampler in _samplers)
            d.DestroySampler(sampler, null);
        d.DestroyDescriptorPool(Pool, null);
        d.DestroyDescriptorSetLayout(Layout, null);
    }
}
