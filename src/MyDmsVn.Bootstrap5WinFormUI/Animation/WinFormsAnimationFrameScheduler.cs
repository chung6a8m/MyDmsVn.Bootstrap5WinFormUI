using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Animation;

internal sealed class WinFormsAnimationFrameScheduler : IAnimationFrameScheduler
{
    private const int FrameIntervalMilliseconds = 16;

    private readonly Timer _timer;
    private Action? _callback;
    private bool _disposed;

    public WinFormsAnimationFrameScheduler()
    {
        _timer = new Timer
        {
            Interval = FrameIntervalMilliseconds
        };
        _timer.Tick += OnTick;
    }

    public bool IsRunning => !_disposed && _timer.Enabled;

    public void Start(Action callback)
    {
        ThrowIfDisposed();
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        if (!_timer.Enabled)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        ThrowIfDisposed();
        _timer.Stop();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer.Dispose();
        _callback = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _callback?.Invoke();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WinFormsAnimationFrameScheduler));
        }
    }
}
