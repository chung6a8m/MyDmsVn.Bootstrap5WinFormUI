using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Animation;

internal interface IAnimationClock
{
    TimeSpan Elapsed { get; }

    void Restart();
}
