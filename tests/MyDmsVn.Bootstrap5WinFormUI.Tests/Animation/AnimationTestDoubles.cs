using System;
using MyDmsVn.Bootstrap5WinFormUI.Animation;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Animation;

internal sealed class ManualAnimationClock : IAnimationClock
{
    public TimeSpan Elapsed { get; private set; }

    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        Elapsed += elapsed;
    }

    public void Restart()
    {
        Elapsed = TimeSpan.Zero;
    }
}

internal sealed class ManualAnimationFrameScheduler : IAnimationFrameScheduler
{
    private Action? _callback;
    private bool _disposed;

    public bool IsRunning { get; private set; }

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public void Start(Action callback)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ManualAnimationFrameScheduler));
        }

        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        IsRunning = true;
        StartCount++;
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        if (IsRunning)
        {
            StopCount++;
        }

        IsRunning = false;
    }

    public void FireFrame()
    {
        if (IsRunning)
        {
            _callback?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        IsRunning = false;
        _callback = null;
    }
}
