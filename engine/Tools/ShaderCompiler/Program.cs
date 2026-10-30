// Compiles every shader stage in a folder (*.vert, *.frag, *.comp) to SPIR-V for Vulkan 1.3.
// *.glsl files are includes. A stage is rebuilt when it, or any include, is newer than its output.
//   ShaderCompiler <source folder> <output folder> [--force]
using Brutal.ShaderCApi;
using Statventure.Core;

NativeLibraries.Install();
if (args.Length < 2)
{
    Console.Error.WriteLine("usage: ShaderCompiler <source folder> <output folder> [--force]");
    return 2;
}
var sourceDir = Path.GetFullPath(args[0]);
var outputDir = Path.GetFullPath(args[1]);
var force = args.Contains("--force");
Directory.CreateDirectory(outputDir);

var stages = new Dictionary<string, ShaderKind>
{
    [".vert"] = ShaderKind.VertexShader,
    [".frag"] = ShaderKind.FragmentShader,
    [".comp"] = ShaderKind.ComputeShader,
};
var newestInclude = Directory.EnumerateFiles(sourceDir, "*.glsl", SearchOption.AllDirectories)
    .Select(File.GetLastWriteTimeUtc)
    .DefaultIfEmpty(DateTime.MinValue)
    .Max();

var compiler = ShaderC.PInvoke.shaderc_compiler_initialize();
var options = ShaderC.PInvoke.shaderc_compile_options_initialize();
options.SetTargetEnv(TargetEnv.Vulkan, (uint)EnvVersion.Vulkan_1_3);
options.SetOptimizationLevel(OptimizationLevel.Performance);
options.SetDefaultIncludeCallbacks();

int built = 0, failed = 0, current = 0;
foreach (var path in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories).Order())
{
    if (!stages.TryGetValue(Path.GetExtension(path), out var kind))
        continue;
    var output = Path.Combine(outputDir, Path.GetFileName(path) + ".spv");
    var newestInput = File.GetLastWriteTimeUtc(path) > newestInclude ? File.GetLastWriteTimeUtc(path) : newestInclude;
    if (!force && File.Exists(output) && File.GetLastWriteTimeUtc(output) >= newestInput)
    {
        current++;
        continue;
    }
    if (Compile(path, kind, output))
        built++;
    else
        failed++;
}
options.Dispose();
compiler.Dispose();
Console.WriteLine($"shaders: {built} built, {current} up to date, {failed} failed");
return failed == 0 ? 0 : 1;

unsafe bool Compile(string path, ShaderKind kind, string output)
{
    var source = File.ReadAllBytes(path);
    var name = System.Text.Encoding.UTF8.GetBytes(path + "\0");
    fixed (byte* sourcePointer = source)
    fixed (byte* namePointer = name)
    fixed (byte* entryPointer = "main\0"u8)
    {
        using var result = ShaderC.PInvoke.shaderc_compile_into_spv(compiler, sourcePointer, new ByteSize64((ulong)source.Length), kind, namePointer, entryPointer, options);
        if (result.GetCompilationStatus() != CompilationStatus.Success)
        {
            Console.Error.WriteLine(result.GetErrorMessage().TrimEnd());
            return false;
        }
        var bytes = new ReadOnlySpan<byte>(result.GetBytes(), (int)result.GetLength());
        File.WriteAllBytes(output, bytes.ToArray());
        return true;
    }
}
