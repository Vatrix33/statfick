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
/// buffers at the real-time pace and drops them, trying again for a device every second. Its AL calls
/// are made in its own context, set for the calling thread only while it makes them (<see cref="Use"/>),
/// so other outputs in the process don't disturb it.
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
    readonly IDisposable? _deviceEvents;
    readonly bool _neverOpen;
    bool _paused;
    /// <summary>The source was started and hasn't been seen stopped since: everything queued so far plays before it stops.</summary>
    bool _running;

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
            _deviceEvents = DefaultDeviceEvents.OpenAl.Listen(() => _defaultChanged = true);
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

    /// <summary>
    /// Makes this output's context the calling thread's while one method makes its AL calls, and puts the
    /// thread's own back after (OpenAL Soft's thread-local contexts, ALC_EXT_thread_local_context). Not
    /// the process-wide current context: a source is a name within its context and a buffer within its
    /// device, the first ones the same numbers in each, so with that one an output's calls went to
    /// whichever output had made its context current last (an application's, a loopback device's), and
    /// to none once that one closed: the audio thread then found nothing to fill and the graph stopped
    /// for good (Docs/Notes.md, 2026-11-05). Not left set: a thread holding a context keeps it alive past
    /// its output, and OpenAL Soft reports a thread that ends holding one as an error.
    /// </summary>
    ThreadContext Use() => new(_context!.Handle);

    readonly ref struct ThreadContext
    {
        readonly AlcContext _previous;

        public ThreadContext(AlcContext context)
        {
            _previous = Alc.PInvoke.GetThreadContext();
            if (!Alc.PInvoke.SetThreadContext(context))
                throw new InvalidOperationException("OpenAL refused the output's context for this thread");
        }

        public void Dispose() => Alc.PInvoke.SetThreadContext(_previous);
    }

    void StartStream(Context context)
    {
        _context = context;
        using var _ = Use();
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
            using var _ = Use();
            Reclaim();
            return _free.Count;
        }
    }

    /// <summary>Takes back the buffers the source has played. One not started since it was last seen stopped has played none, whatever it reports.</summary>
    void Reclaim()
    {
        if (!_running)
            return;
        var processed = _source.BuffersProcessed;
        if (processed <= 0)
            return;
        Span<AlBuffer> done = stackalloc AlBuffer[processed];
        _source.UnqueueBuffers(done);
        foreach (var buffer in done)
            _free.Enqueue(buffer);
    }

    /// <summary>
    /// After an underrun (the source ran dry: the audio thread wasn't in time), takes back everything the source
    /// played, the first time it is seen stopped and before anything new is written, so it goes on with new
    /// audio (the user's decision, 2026-11-05): restarted with those buffers still queued it would play them
    /// again from its queue's head (OpenAL Soft's StartSources), the last 20 ms over. A stopped source reports
    /// every queued buffer as processed, also ones queued after it stopped, so this comes before each write,
    /// the restart and resuming, and the source counts as not running until it is started again.
    /// </summary>
    void TakeBackAfterUnderrun()
    {
        if (!_running || _source.State != AlSourceState.Stopped)
            return;
        Reclaim();
        _running = false;
        Underruns++;
        if (_loopback == null)
            Log.Warning("Audio", $"underrun at {_clock.Elapsed.TotalSeconds:F3} s");
    }

    public void Write(ReadOnlySpan<float> interleaved)
    {
        if (!HasDevice)
        {
            _silentFrames += BufferFrames;
            return;
        }
        using var _ = Use();
        TakeBackAfterUnderrun();
        var buffer = _free.Dequeue();
        MemoryMarshal.AsBytes(interleaved).CopyTo(_bytes);
        buffer.SetData(AlFormat.StereoFloat32, _bytes, SampleRate);
        _source.QueueBuffers([buffer]);
    }

    public void Maintain()
    {
        if (_loopback != null)
        {
            Start();
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
        Start();
    }

    /// <summary>Starts the source when it has buffers to play and isn't playing: at first, and after an underrun or a lost device (what it played taken back first).</summary>
    void Start()
    {
        using var _ = Use();
        TakeBackAfterUnderrun();
        if (!_paused && _source.State != AlSourceState.Playing && _source.BuffersQueued > 0)
            Play();
    }

    void Play()
    {
        _source.Play();
        _running = true;
    }

    /// <summary>Whether the output hears OpenAL's default-device-changed events now (every live output that looks for a device; not a silent one, not a loopback one, not once disposed).</summary>
    internal bool FollowsDefaultDevice => _deviceEvents is { } listening && DefaultDeviceEvents.OpenAl.Has(listening);

    /// <summary>How often the source ran out of queued buffers (an underrun, or a lost device stopping it).</summary>
    internal int Underruns { get; private set; }

    /// <summary>How often the device was reopened on the default.</summary>
    internal int Reopens { get; private set; }

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
            // The source carries on, its queue with it (one a lost device stopped starts again with new audio: Start).
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
        {
            using var _ = Use();
            _source.Pause();
        }
        else
            _clock.Stop();
    }

    public void Resume()
    {
        _paused = false;
        if (HasDevice)
        {
            using var _ = Use();
            TakeBackAfterUnderrun();
            if (_source.BuffersQueued > 0)
                Play();
        }
        else
            _clock.Start();
    }

    public void Wait(int milliseconds) => _bufferDone.WaitOne(HasDevice ? milliseconds : 1);

    void CloseDevice()
    {
        // No thread holds the context (Use puts each thread's own back), so it goes now.
        _context?.Dispose();
        _context = null;
        _device?.Dispose();
        _device = null;
    }

    public void Dispose()
    {
        _deviceEvents?.Dispose();
        if (HasDevice)
        {
            using var _ = Use();
            _source.Stop();
            _source.Delete();
            Al.DeleteBuffers(_buffers);
        }
        CloseDevice();
        _bufferDone.Dispose();
    }
}

/// <summary>
/// OpenAL's default-device-changed events, shared by every live output that follows the default device (the
/// user's decision, 2026-11-05). OpenAL Soft has one event callback and one set of enabled events for the
/// process (alc/events.cpp): with each output registering its own, only the last one made heard of a change,
/// and the first one disposed turned the events off for all. Here one callback tells every listener; it is
/// registered and the events turned on with the first listener, and both taken away when the last one goes.
/// </summary>
internal sealed class DefaultDeviceEvents(Action<AlcEventCallback?> register, Action<bool> enable)
{
    /// <summary>OpenAL's own (the tests make others, with hooks of their own).</summary>
    public static DefaultDeviceEvents OpenAl { get; } = new(
        callback => Alc.SetEventCallback(callback!),
        on => Alc.EventControl([AlcEventType.DefaultDeviceChanged], on));

    readonly Lock _gate = new();
    Listener[] _listeners = [];

    /// <summary>Calls <paramref name="changed"/> (on OpenAL's event thread) whenever the default device changes, until the result is disposed.</summary>
    public IDisposable Listen(Action changed)
    {
        var listener = new Listener(this, changed);
        lock (_gate)
        {
            if (_listeners.Length == 0)
                try
                {
                    register(Deliver);
                    enable(true);
                }
                catch (Exception exception)
                {
                    Log.Warning("Audio", "no device change events: " + exception.Message);
                }
            Volatile.Write(ref _listeners, [.. _listeners, listener]);
        }
        return listener;
    }

    /// <summary>Whether <paramref name="listening"/> (what <see cref="Listen"/> gave) still listens.</summary>
    public bool Has(IDisposable listening) => Array.IndexOf(Volatile.Read(ref _listeners), listening) >= 0;

    void Leave(Listener listener)
    {
        lock (_gate)
        {
            var left = Array.FindAll(_listeners, other => other != listener);
            if (left.Length == _listeners.Length)
                return;
            Volatile.Write(ref _listeners, left);
            if (left.Length == 0)
                try
                {
                    enable(false);
                    register(null);
                }
                catch (Exception exception)
                {
                    Log.Warning("Audio", "device change events stayed on: " + exception.Message);
                }
        }
    }

    /// <summary>
    /// The callback, on OpenAL's event thread. It takes no lock: OpenAL calls it holding its event mutex, which
    /// registering and enabling take too, so a lock held around those (<see cref="Listen"/>, <see cref="Leave"/>)
    /// would deadlock against it; the listeners are read as last written instead.
    /// </summary>
    void Deliver(AlcEventType type, AlcDeviceType deviceType, AlcDevice device, ReadOnlySpan<byte> message)
    {
        if (type == AlcEventType.DefaultDeviceChanged)
            foreach (var listener in Volatile.Read(ref _listeners))
                listener.Changed();
    }

    sealed class Listener(DefaultDeviceEvents events, Action changed) : IDisposable
    {
        public Action Changed { get; } = changed;

        /// <summary>Stops listening (again: nothing).</summary>
        public void Dispose() => events.Leave(this);
    }
}
