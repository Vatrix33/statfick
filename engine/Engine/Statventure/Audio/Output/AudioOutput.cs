using System.Diagnostics;
using System.Runtime.InteropServices;
using Brutal.OpenAlApi;
using Statventure.Core;

namespace Statventure.Audio;

/// <summary>Where rendered audio goes: stereo float32 buffers of whole render quanta, at <see cref="SampleRate"/>.</summary>
internal interface IAudioOutput : IDisposable
{
    /// <summary>The rate the graph renders at: the device's own rate when it opened, else 48 kHz.</summary>
    int SampleRate { get; }
    /// <summary>Frames per buffer: whole quanta, about 5 ms.</summary>
    int BufferFrames { get; }
    /// <summary>Buffers the output can take now (about 20 ms are kept queued).</summary>
    int Writable { get; }
    /// <summary>Queues one buffer: <see cref="BufferFrames"/> interleaved stereo frames.</summary>
    void Write(ReadOnlySpan<float> interleaved);
    /// <summary>Called by the audio thread between buffers: follows device changes, restarts after an underrun.</summary>
    void Maintain();
    void Pause();
    void Resume();
    /// <summary>Waits until a buffer may have become free (at most <paramref name="milliseconds"/>).</summary>
    void Wait(int milliseconds);
    /// <summary>Whether sound reaches a device now (else buffers are taken at the real-time pace and dropped).</summary>
    bool HasDevice { get; }
}

/// <summary>
/// The output through OpenAL Soft (Docs/Audio.md, step 7): a streaming source fed queued buffers
/// (4 × about 5 ms), played untouched: direct channels (left to left, right to right), HRTF off,
/// OpenAL's output limiter off, the device opened at its own rate. It follows the system's default
/// device (OpenAL Soft's default-device event, or a lost connection: reopened on the default, the
/// graph keeping its rate, OpenAL resampling if the new rate differs); without a device it takes
/// buffers at the real-time pace and drops them, trying again for a device every second.
/// </summary>
internal sealed class OpenAlOutput : IAudioOutput
{
    public const int BufferCount = 4;
    static readonly (AlcContextAttribute, int)[] Untouched = [(AlcContextAttribute.Hrtf, 0), (AlcContextAttribute.OutputLimiter, 0)];

    readonly Stopwatch _clock = new();
    readonly AutoResetEvent _bufferDone = new(false);
    readonly byte[] _bytes;
    readonly LoopbackDevice? _loopback;
    Device? _device;
    Context? _context;
    AlSource _source;
    AlBuffer[] _buffers = [];
    readonly Queue<AlBuffer> _free = new();
    long _silentFrames;
    double _nextRetry;
    volatile bool _defaultChanged;
    readonly bool _neverOpen;
    bool _paused;

    /// <summary>Opens the system's default device (or runs silently without one; <paramref name="silent"/>: never looks for one, for the tests).</summary>
    public OpenAlOutput(bool silent = false)
    {
        SampleRate = 48000;
        _neverOpen = silent;
        try
        {
            _device = silent ? null : Alc.OpenDevice(null!);
            if (_device != null)
                SampleRate = DeviceRate(_device);
        }
        catch (Exception exception)
        {
            Log.Warning("Audio", "no output device: " + exception.Message);
            _device = null;
        }
        BufferFrames = FramesPerBuffer(SampleRate);
        _bytes = new byte[BufferFrames * 2 * sizeof(float)];
        if (_device != null)
            StartStream(_device.CreateContext(new Context.CreateInfo { Attributes = Untouched }));
        if (!silent)
        try
        {
            Alc.SetEventCallback((type, _, _, _) =>
            {
                if (type == AlcEventType.DefaultDeviceChanged)
                    _defaultChanged = true;
            });
            Alc.EventControl([AlcEventType.DefaultDeviceChanged], true);
        }
        catch (Exception exception)
        {
            Log.Warning("Audio", "no device change events: " + exception.Message);
        }
        _clock.Start();
        Log.Info("Audio", _device != null
            ? $"OpenAL Soft on {_device.AllDevicesSpecifier} at {SampleRate} Hz, HRTF {_device.HrtfStatus}, {BufferCount} × {BufferFrames} frames"
            : $"no device: rendering silently at {SampleRate} Hz");
    }

    /// <summary>A loopback device for the tests: OpenAL mixes into memory when <see cref="LoopbackDevice.Render"/> is called.</summary>
    internal OpenAlOutput(LoopbackDevice loopback, int sampleRate)
    {
        _loopback = loopback;
        SampleRate = sampleRate;
        BufferFrames = FramesPerBuffer(sampleRate);
        _bytes = new byte[BufferFrames * 2 * sizeof(float)];
        StartStream(loopback.CreateContext(new Context.CreateInfo
        {
            // The device's attributes, plus the format a loopback device needs.
            Attributes = [(AlcContextAttribute.Frequency, sampleRate), (AlcContextAttribute.FormatChannels, (int)AlcChannelLayout.Stereo),
                (AlcContextAttribute.FormatType, (int)AlcSampleType.Float), .. Untouched],
        }));
    }

    public int SampleRate { get; }
    public int BufferFrames { get; }
    public bool HasDevice => _device != null || _loopback != null;

    /// <summary>Whole render quanta of about 5 ms: 2 quanta at 44.1 and 48 kHz, 4 at 96 kHz.</summary>
    internal static int FramesPerBuffer(int sampleRate) =>
        AudioGraph.RenderQuantumFrames * Math.Max(1, (int)Math.Round(0.005 * sampleRate / AudioGraph.RenderQuantumFrames));

    static int DeviceRate(Device device)
    {
        foreach (var (key, value) in device.Attributes)
            if ((int)key == 0x1007) // ALC_FREQUENCY
                return value;
        return 48000;
    }

    void StartStream(Context context)
    {
        _context = context;
        context.MakeCurrent();
        _source = Al.GenSource();
        _source.DirectChannels = AlDirectChannelsMode.DropUnmatched;
        _buffers = Al.GenBuffers(BufferCount);
        _free.Clear();
        foreach (var buffer in _buffers)
            _free.Enqueue(buffer);
        try
        {
            Al.SetEventCallback((type, _, _, _) =>
            {
                if (type == AlEventType.BufferCompleted)
                    _bufferDone.Set();
            });
            Al.EventControl([AlEventType.BufferCompleted], true);
        }
        catch (Exception exception)
        {
            Log.Warning("Audio", "no buffer events: " + exception.Message);
        }
    }

    public int Writable
    {
        get
        {
            if (!HasDevice)
            {
                // The real-time pace: as many buffers as keep the rendered frames 20 ms ahead of the clock.
                var due = (long)(_clock.Elapsed.TotalSeconds * SampleRate) + BufferCount * BufferFrames - _silentFrames;
                return (int)Math.Clamp(due / BufferFrames, 0, BufferCount);
            }
            Reclaim();
            return _free.Count;
        }
    }

    void Reclaim()
    {
        var processed = _source.BuffersProcessed;
        if (processed <= 0)
            return;
        Span<AlBuffer> done = stackalloc AlBuffer[processed];
        _source.UnqueueBuffers(done);
        foreach (var buffer in done)
            _free.Enqueue(buffer);
    }

    public void Write(ReadOnlySpan<float> interleaved)
    {
        if (!HasDevice)
        {
            _silentFrames += BufferFrames;
            return;
        }
        var buffer = _free.Dequeue();
        MemoryMarshal.AsBytes(interleaved).CopyTo(_bytes);
        buffer.SetData(AlFormat.StereoFloat32, _bytes, SampleRate);
        _source.QueueBuffers([buffer]);
    }

    public void Maintain()
    {
        if (_loopback != null)
        {
            if (!_paused && _source.State != AlSourceState.Playing && _source.BuffersQueued > 0)
                _source.Play();
            return;
        }
        var now = _clock.Elapsed.TotalSeconds;
        if (_device == null)
        {
            if (now >= _nextRetry && !_neverOpen)
            {
                _nextRetry = now + 1;
                TryOpen();
            }
            return;
        }
        if (_defaultChanged || !_device.Connected)
        {
            _defaultChanged = false;
            Reopen();
            return;
        }
        if (!_paused && _source.State != AlSourceState.Playing && _source.BuffersQueued > 0)
        {
            // Stopped with buffers queued: it ran dry (or has just started).
            if (_source.State == AlSourceState.Stopped)
            {
                Underruns++;
                Log.Warning("Audio", $"underrun at {_clock.Elapsed.TotalSeconds:F3} s");
            }
            _source.Play();
        }
    }

    /// <summary>How often the source ran out of queued buffers and had to be restarted.</summary>
    internal int Underruns { get; private set; }

    /// <summary>How often the device was reopened on the default.</summary>
    internal int Reopens { get; private set; }

    /// <summary>For the tests: acts as if the system's default device had changed (reopening it, here on the same device).</summary>
    internal void SimulateDefaultDeviceChange() => _defaultChanged = true;

    void TryOpen()
    {
        try
        {
            var device = Alc.OpenDevice(null!);
            _device = device;
            StartStream(device.CreateContext(new Context.CreateInfo { Attributes = Untouched }));
            Log.Info("Audio", $"device found: {device.AllDevicesSpecifier} (rendering at {SampleRate} Hz, the device at {DeviceRate(device)})");
        }
        catch (Exception)
        {
            _device = null;
        }
    }

    /// <summary>Moves to the default device; while that fails, renders silently at the real-time pace.</summary>
    void Reopen()
    {
        try
        {
            _device!.Reopen(null!, Untouched);
            // The source carries on, its queue with it (a source a lost device stopped is restarted by Maintain).
            Reopens++;
            Log.Info("Audio", $"moved to {_device.AllDevicesSpecifier} (rendering at {SampleRate} Hz, the device at {DeviceRate(_device)})");
        }
        catch (Exception exception)
        {
            Log.Warning("Audio", "device lost: " + exception.Message);
            CloseDevice();
            _silentFrames = (long)(_clock.Elapsed.TotalSeconds * SampleRate);
            _nextRetry = _clock.Elapsed.TotalSeconds + 1;
        }
    }

    public void Pause()
    {
        _paused = true;
        if (HasDevice)
            _source.Pause();
        else
            _clock.Stop();
    }

    public void Resume()
    {
        _paused = false;
        if (HasDevice)
        {
            if (_source.BuffersQueued > 0)
                _source.Play();
        }
        else
            _clock.Start();
    }

    public void Wait(int milliseconds) => _bufferDone.WaitOne(HasDevice ? milliseconds : 1);

    void CloseDevice()
    {
        if (_context != null)
        {
            Context.MakeCurrent(null!);
            _context.Dispose();
            _context = null;
        }
        _device?.Dispose();
        _device = null;
    }

    public void Dispose()
    {
        if (HasDevice)
        {
            _source.Stop();
            _source.Delete();
            Al.DeleteBuffers(_buffers);
        }
        CloseDevice();
        if (_loopback == null && !_neverOpen)
            try
            {
                Alc.EventControl([AlcEventType.DefaultDeviceChanged], false);
                Alc.SetEventCallback(null!);
            }
            catch (Exception exception)
            {
                Log.Warning("Audio", "device change events stayed on: " + exception.Message);
            }
        _bufferDone.Dispose();
    }
}
