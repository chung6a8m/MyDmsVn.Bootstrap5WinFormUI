using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapUiDebouncer : IDisposable
{
    private readonly Timer _timer;
    private Action? _pending;
    private bool _disposed;

    internal BootstrapUiDebouncer()
    {
        _timer = new Timer();
        _timer.Tick += OnTick;
    }

    internal void Schedule(TimeSpan delay, Action action)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BootstrapUiDebouncer));
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));
        _pending = action ?? throw new ArgumentNullException(nameof(action));
        _timer.Stop();
        if (delay == TimeSpan.Zero)
        {
            var pending = _pending;
            _pending = null;
            pending();
            return;
        }

        _timer.Interval = Math.Max(1, Math.Min(int.MaxValue, (int)Math.Ceiling(delay.TotalMilliseconds)));
        _timer.Start();
    }

    internal void Cancel()
    {
        if (_disposed) return;
        _timer.Stop();
        _pending = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pending = null;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer.Dispose();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        var pending = _pending;
        _pending = null;
        pending?.Invoke();
    }
}
