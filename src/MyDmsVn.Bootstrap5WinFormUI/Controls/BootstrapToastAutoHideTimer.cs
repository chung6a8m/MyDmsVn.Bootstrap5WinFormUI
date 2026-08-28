using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal interface IBootstrapToastAutoHideTimer : IDisposable
{
    int Interval { get; set; }
    bool Enabled { get; }
    event EventHandler? Tick;
    void Start();
    void Stop();
}

internal sealed class WinFormsBootstrapToastAutoHideTimer : IBootstrapToastAutoHideTimer
{
    private readonly Timer _timer = new Timer();

    public int Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public bool Enabled => _timer.Enabled;

    public event EventHandler? Tick
    {
        add => _timer.Tick += value;
        remove => _timer.Tick -= value;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Dispose();
}
