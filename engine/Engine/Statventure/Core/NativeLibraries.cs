using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Statventure.Core;

/// <summary>
/// Resolves native library names that BRUTAL's packages ask for but don't ship under that name.
/// Install once at startup, before any BRUTAL call.
/// <list type="bullet">
/// <item>Vulkan is loaded as "vulkan", which only exists where the Vulkan developer package is
/// installed (libvulkan.so); every Linux driver ships libvulkan.so.1.</item>
/// <item>ShaderC is imported as "shaderc"; the package ships libshaderc_shared.so / shaderc_shared.dll.</item>
/// </list>
/// </summary>
public static class NativeLibraries
{
    static int _installed;

    public static void Install()
    {
        if (Interlocked.Exchange(ref _installed, 1) == 1)
            return;
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += Resolve;
    }

    static nint Resolve(Assembly assembly, string name)
    {
        foreach (var candidate in Candidates(name))
            if (NativeLibrary.TryLoad(candidate, assembly, DllImportSearchPath.SafeDirectories | DllImportSearchPath.AssemblyDirectory, out var handle)
                || NativeLibrary.TryLoad(candidate, out handle))
                return handle;
        return nint.Zero;
    }

    static IEnumerable<string> Candidates(string name)
    {
        // The NuGet packages' folder for this platform: the portable runtime id (a distro's own .NET,
        // such as Ubuntu's, reports its own, e.g. ubuntu.24.04-x64).
        var platform = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        var rid = platform + "-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        var native = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native");
        if (OperatingSystem.IsLinux())
        {
            if (name == "vulkan")
                yield return "libvulkan.so.1";
            if (name == "shaderc")
            {
                yield return Path.Combine(native, "libshaderc_shared.so");
                yield return Path.Combine(AppContext.BaseDirectory, "libshaderc_shared.so");
            }
        }
        else if (OperatingSystem.IsWindows())
        {
            if (name == "shaderc")
            {
                yield return Path.Combine(native, "shaderc_shared.dll");
                yield return Path.Combine(AppContext.BaseDirectory, "shaderc_shared.dll");
            }
        }
    }
}
