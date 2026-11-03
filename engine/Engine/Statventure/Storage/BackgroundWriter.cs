using Statventure.Core;

namespace Statventure.Storage;

/// <summary>
/// Writes files off the caller's thread (Docs/Assets.md, "When a save is written"; Q16): the caller
/// prepares the bytes and hands over the work for a file; one writer thread does the work in the
/// order the files were first asked for. A file asked for again before its work started keeps only
/// the latest work (the latest state wins); asked for while its work runs, the new work follows.
/// The thread is a foreground thread that ends when nothing is left, so the process doesn't end
/// before the pending files are written; <see cref="Flush"/> waits for them. A failure is logged
/// and reported through <see cref="Failed"/>; the next request for the file tries again.
/// </summary>
sealed class BackgroundWriter : IDisposable
{
    readonly object _gate = new();
    readonly OrderedDictionary<string, Action> _pending = new(StringComparer.Ordinal);
    string? _running;
    Thread? _thread;
    bool _disposed;

    /// <summary>A file's work failed (on the writer thread): the file and the error.</summary>
    public event Action<string, Exception>? Failed;

    /// <summary>Tests: called on the writer thread before each piece of work, with its file.</summary>
    internal Action<string>? BeforeWork { get; set; }

    /// <summary>Whether the writer thread is alive (it ends when nothing is left).</summary>
    internal bool IsRunning
    {
        get
        {
            lock (_gate)
                return _thread != null;
        }
    }

    /// <summary>Hands over the work for <paramref name="file"/>, replacing work for it that hasn't started.</summary>
    public void Enqueue(string file, Action work)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _pending[file] = work;
            if (_thread == null)
            {
                _thread = new Thread(Run) { IsBackground = false, Name = "Statventure storage writer" };
                _thread.Start();
            }
        }
    }

    /// <summary>Drops the work for <paramref name="file"/> that hasn't started and waits for work on it that runs.</summary>
    public void Discard(string file)
    {
        lock (_gate)
        {
            _pending.Remove(file);
            while (_running == file)
                Monitor.Wait(_gate);
        }
    }

    /// <summary>Waits until no work for <paramref name="file"/> is pending or running.</summary>
    public void WaitFor(string file)
    {
        lock (_gate)
            while (_pending.ContainsKey(file) || _running == file)
                Monitor.Wait(_gate);
    }

    /// <summary>Waits until every piece of work handed over so far is done.</summary>
    public void Flush()
    {
        lock (_gate)
            while (_pending.Count > 0 || _running != null)
                Monitor.Wait(_gate);
    }

    /// <summary>Flushes, then refuses new work.</summary>
    public void Dispose()
    {
        lock (_gate)
            _disposed = true;
        Flush();
    }

    void Run()
    {
        while (true)
        {
            string file;
            Action work;
            lock (_gate)
            {
                if (_pending.Count == 0)
                {
                    _thread = null;
                    Monitor.PulseAll(_gate);
                    return;
                }
                (file, work) = _pending.GetAt(0);
                _pending.RemoveAt(0);
                _running = file;
            }
            try
            {
                BeforeWork?.Invoke(file);
                work();
            }
            catch (Exception e)
            {
                Log.Error("Storage", $"writing {file} failed: {e.Message}");
                try
                {
                    Failed?.Invoke(file, e);
                }
                catch (Exception listener)
                {
                    Log.Error("Storage", listener);
                }
            }
            finally
            {
                lock (_gate)
                {
                    _running = null;
                    Monitor.PulseAll(_gate);
                }
            }
        }
    }
}
