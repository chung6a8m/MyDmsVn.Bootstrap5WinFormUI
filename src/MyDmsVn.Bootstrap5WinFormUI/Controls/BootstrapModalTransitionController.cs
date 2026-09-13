using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapModalTransitionController : IDisposable
{
    private readonly Form _modal;
    private readonly BootstrapAnimation _opening;
    private bool _disposed;
    private double _targetOpacity = 1d;

    public BootstrapModalTransitionController(Form modal)
    {
        _modal = modal ?? throw new ArgumentNullException(nameof(modal));
        _opening = new BootstrapAnimation(TimeSpan.FromMilliseconds(140), BootstrapEasing.EaseOut, modal);
        _opening.ProgressChanged += OnOpeningProgressChanged;
        _opening.Completed += OnOpeningCompleted;
    }

    public bool IsRunning => !_disposed && _opening.IsRunning;

    public void BeginOpen()
    {
        if (_disposed) return;
        _targetOpacity = _modal.Opacity;
        _modal.Opacity = _targetOpacity * 0.92d;
        _opening.Restart();
        if (!_opening.IsRunning) _modal.Opacity = _targetOpacity;
    }

    public void Stop()
    {
        if (_disposed) return;
        _opening.Stop();
        if (!_modal.IsDisposed) _modal.Opacity = _targetOpacity;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _opening.ProgressChanged -= OnOpeningProgressChanged;
        _opening.Completed -= OnOpeningCompleted;
        _opening.Dispose();
    }

    private void OnOpeningProgressChanged(object? sender, EventArgs e)
    {
        if (!_disposed && !_modal.IsDisposed) _modal.Opacity = _targetOpacity * (0.92d + 0.08d * _opening.Progress);
    }

    private void OnOpeningCompleted(object? sender, EventArgs e)
    {
        if (!_disposed && !_modal.IsDisposed) _modal.Opacity = _targetOpacity;
    }
}
