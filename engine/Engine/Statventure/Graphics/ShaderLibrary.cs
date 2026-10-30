using System.Reflection;
using Brutal.VulkanApi;

namespace Statventure.Graphics;

/// <summary>
/// Loads SPIR-V shader modules by name ("canvas.vert"). Engine shaders are embedded in the engine
/// assembly; games add more sources (e.g. their packs) with <see cref="AddSource"/>.
/// </summary>
public sealed unsafe class ShaderLibrary : IDisposable
{
    readonly GpuDevice _device;
    readonly Dictionary<string, VkShaderModule> _modules = [];
    readonly List<Func<string, byte[]?>> _sources = [];

    internal ShaderLibrary(GpuDevice device)
    {
        _device = device;
        var assembly = typeof(ShaderLibrary).Assembly;
        _sources.Add(name => ReadResource(assembly, "Statventure.Shaders." + name + ".spv"));
    }

    static byte[]? ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
            return null;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    /// <summary>Adds a place to look for SPIR-V; later sources win over earlier ones.</summary>
    public void AddSource(Func<string, byte[]?> source)
    {
        lock (_modules)
            _sources.Insert(0, source);
    }

    public bool Exists(string name)
    {
        lock (_modules)
            return _modules.ContainsKey(name) || _sources.Any(source => source(name) is not null);
    }

    public VkShaderModule Get(string name)
    {
        lock (_modules)
        {
            if (_modules.TryGetValue(name, out var module))
                return module;
            byte[]? code = null;
            foreach (var source in _sources)
                if ((code = source(name)) is not null)
                    break;
            if (code is null)
                throw new FileNotFoundException($"shader {name} was not found (is it compiled by Tools/ShaderCompiler?)");
            fixed (byte* pointer = code)
                module = _device.Device.CreateShaderModule(new VkShaderModuleCreateInfo { CodeSize = ByteSize.Of(code.Length), Code = (int*)pointer }, null);
            _modules[name] = module;
            return module;
        }
    }

    public void Dispose()
    {
        foreach (var module in _modules.Values)
            _device.Device.DestroyShaderModule(module, null);
        _modules.Clear();
    }
}
