using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Animation;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

internal sealed class BootstrapToastAnimationHarness
{
    private readonly List<BootstrapToastAnimationRecord> _records = new List<BootstrapToastAnimationRecord>();

    public bool ReducedMotion { get; set; }

    public IReadOnlyList<BootstrapToastAnimationRecord> Records => _records;

    public BootstrapAnimation Create(TimeSpan duration, Func<double, double> easing, Control owner)
    {
        var clock = new ManualAnimationClock();
        var scheduler = new ManualAnimationFrameScheduler();
        var animation = new BootstrapAnimation(duration, easing, owner, clock, scheduler, () => ReducedMotion);
        _records.Add(new BootstrapToastAnimationRecord(animation, clock, scheduler));
        return animation;
    }
}

internal sealed class BootstrapToastAnimationRecord
{
    public BootstrapToastAnimationRecord(
        BootstrapAnimation animation,
        ManualAnimationClock clock,
        ManualAnimationFrameScheduler scheduler)
    {
        Animation = animation;
        Clock = clock;
        Scheduler = scheduler;
    }

    public BootstrapAnimation Animation { get; }

    public ManualAnimationClock Clock { get; }

    public ManualAnimationFrameScheduler Scheduler { get; }

    public void Advance(int milliseconds)
    {
        Clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
        Scheduler.FireFrame();
    }
}

internal sealed class ManualToastAutoHideTimer : IBootstrapToastAutoHideTimer
{
    private EventHandler? _tick;

    public int Interval { get; set; }

    public bool Enabled { get; private set; }

    public bool IsDisposed { get; private set; }

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public int SubscriberCount { get; private set; }

    public event EventHandler? Tick
    {
        add
        {
            _tick += value;
            SubscriberCount++;
        }
        remove
        {
            _tick -= value;
            SubscriberCount--;
        }
    }

    public void Start()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(ManualToastAutoHideTimer));
        }

        Enabled = true;
        StartCount++;
    }

    public void Stop()
    {
        if (IsDisposed)
        {
            return;
        }

        if (Enabled)
        {
            StopCount++;
        }

        Enabled = false;
    }

    public void Fire()
    {
        _tick?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        Enabled = false;
    }
}
