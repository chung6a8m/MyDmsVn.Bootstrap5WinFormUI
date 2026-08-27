using System;
using System.Diagnostics;

namespace MyDmsVn.Bootstrap5WinFormUI.Animation;

internal sealed class StopwatchAnimationClock : IAnimationClock
{
    private readonly Stopwatch _stopwatch = new Stopwatch();

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Restart()
    {
        _stopwatch.Restart();
    }
}
