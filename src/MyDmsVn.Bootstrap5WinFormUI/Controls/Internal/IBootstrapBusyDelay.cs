using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal interface IBootstrapBusyDelay : IDisposable
{
    void Schedule(TimeSpan delay, Action action);
    void Cancel();
}

internal sealed class BootstrapBusyDelay : IBootstrapBusyDelay
{
    private readonly BootstrapUiDebouncer _debouncer = new();

    public void Schedule(TimeSpan delay, Action action) => _debouncer.Schedule(delay, action);
    public void Cancel() => _debouncer.Cancel();
    public void Dispose() => _debouncer.Dispose();
}
