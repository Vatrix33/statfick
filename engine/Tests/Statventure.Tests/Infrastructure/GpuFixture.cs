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
