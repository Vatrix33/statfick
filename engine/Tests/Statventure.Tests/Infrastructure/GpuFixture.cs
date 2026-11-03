using Statventure.Core;
using Statventure.Graphics;

namespace Statventure.Tests.Infrastructure;

/// <summary>One headless GPU device shared by every GPU test (creating devices is slow).</summary>
public sealed class GpuFixture : IDisposable
{
    public GpuDevice Device { get; }

    public GpuFixture()
    {
        Log.Level = LogLevel.Warning;
        Device = GpuDevice.Create(new GpuDeviceOptions { Headless = true, ApplicationName = "Statventure Tests" });
    }

    public void Dispose() => Device.Dispose();
}

[CollectionDefinition("GPU")]
public sealed class GpuCollection : ICollectionFixture<GpuFixture>;

/// <summary>
/// Wall-clock speed measurements, run alone: xunit runs this collection after the parallel ones, so
/// no other test competes for the cores while they measure (the user's decision of 2026-11-03; on a
/// CPU Vulkan the other tests' rendering shares them too). It has a GPU device of its own.
/// </summary>
[CollectionDefinition("Speed", DisableParallelization = true)]
public sealed class SpeedCollection : ICollectionFixture<GpuFixture>;
