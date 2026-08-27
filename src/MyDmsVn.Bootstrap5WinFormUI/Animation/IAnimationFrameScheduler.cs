using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Animation;

internal interface IAnimationFrameScheduler : IDisposable
{
    bool IsRunning { get; }

    void Start(Action callback);

    void Stop();
}
