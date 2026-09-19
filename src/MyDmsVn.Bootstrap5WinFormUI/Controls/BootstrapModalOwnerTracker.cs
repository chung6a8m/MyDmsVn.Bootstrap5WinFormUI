using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapModalOwnerTracker : IDisposable
{
    private readonly Form? _owner;
    private readonly Action _boundsChanged;
    private readonly Action _ownerUnavailable;
    private bool _disposed;

    public BootstrapModalOwnerTracker(Form? owner, Action boundsChanged, Action ownerUnavailable)
    {
        _owner = owner;
        _boundsChanged = boundsChanged ?? throw new ArgumentNullException(nameof(boundsChanged));
        _ownerUnavailable = ownerUnavailable ?? throw new ArgumentNullException(nameof(ownerUnavailable));
        if (_owner is null) return;
        _owner.LocationChanged += OnBoundsChanged;
        _owner.SizeChanged += OnBoundsChanged;
        _owner.VisibleChanged += OnVisibleChanged;
        _owner.FormClosed += OnOwnerClosed;
        _owner.Disposed += OnOwnerDisposed;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_owner is null) return;
        _owner.LocationChanged -= OnBoundsChanged;
        _owner.SizeChanged -= OnBoundsChanged;
        _owner.VisibleChanged -= OnVisibleChanged;
        _owner.FormClosed -= OnOwnerClosed;
        _owner.Disposed -= OnOwnerDisposed;
    }

    private void OnBoundsChanged(object? sender, EventArgs e) => _boundsChanged();
    private void OnVisibleChanged(object? sender, EventArgs e) { if (_owner is not null && !_owner.Visible) _ownerUnavailable(); }
    private void OnOwnerClosed(object? sender, FormClosedEventArgs e) => _ownerUnavailable();
    private void OnOwnerDisposed(object? sender, EventArgs e) => _ownerUnavailable();
}
