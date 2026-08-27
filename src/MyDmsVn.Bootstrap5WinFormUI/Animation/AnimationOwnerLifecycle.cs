using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Animation;

internal sealed class AnimationOwnerLifecycle : IDisposable
{
    private readonly Control? _owner;
    private readonly Action _pause;
    private readonly Action _resume;
    private readonly Action _ownerDisposed;
    private bool _disposed;
    private bool _isOwnerDisposed;

    public AnimationOwnerLifecycle(Control? owner, Action pause, Action resume, Action ownerDisposed)
    {
        _owner = owner;
        _pause = pause ?? throw new ArgumentNullException(nameof(pause));
        _resume = resume ?? throw new ArgumentNullException(nameof(resume));
        _ownerDisposed = ownerDisposed ?? throw new ArgumentNullException(nameof(ownerDisposed));

        if (_owner is null)
        {
            return;
        }

        if (_owner.IsDisposed)
        {
            _isOwnerDisposed = true;
            return;
        }

        _owner.VisibleChanged += OnVisibleChanged;
        _owner.Disposed += OnOwnerDisposed;
    }

    public bool IsOwnerDisposed => _isOwnerDisposed || (_owner?.IsDisposed ?? false);

    public bool IsOwnerVisible => _owner is null || (!IsOwnerDisposed && _owner.Visible);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Detach();
    }

    private void OnVisibleChanged(object? sender, EventArgs e)
    {
        if (_disposed || _owner is null || IsOwnerDisposed)
        {
            return;
        }

        if (_owner.Visible)
        {
            _resume();
        }
        else
        {
            _pause();
        }
    }

    private void OnOwnerDisposed(object? sender, EventArgs e)
    {
        if (_disposed || _isOwnerDisposed)
        {
            return;
        }

        _isOwnerDisposed = true;
        Detach();
        _ownerDisposed();
    }

    private void Detach()
    {
        if (_owner is null)
        {
            return;
        }

        _owner.VisibleChanged -= OnVisibleChanged;
        _owner.Disposed -= OnOwnerDisposed;
    }
}
