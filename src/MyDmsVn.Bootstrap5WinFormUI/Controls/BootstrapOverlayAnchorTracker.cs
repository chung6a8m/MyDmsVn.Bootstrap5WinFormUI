using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapOverlayAnchorTracker : IDisposable
{
    private readonly Control _target;
    private readonly Action _reposition;
    private readonly Action _close;
    private readonly List<Control> _ancestors = new List<Control>();
    private readonly List<ScrollableControl> _scrollableAncestors = new List<ScrollableControl>();
    private Form? _form;
    private bool _disposed;

    public BootstrapOverlayAnchorTracker(Control target, Action reposition, Action close)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _reposition = reposition ?? throw new ArgumentNullException(nameof(reposition));
        _close = close ?? throw new ArgumentNullException(nameof(close));
        if (target.IsDisposed)
        {
            throw new ArgumentException("The tracked target cannot be disposed.", nameof(target));
        }

        target.LocationChanged += OnTargetGeometryChanged;
        target.SizeChanged += OnTargetGeometryChanged;
        target.VisibleChanged += OnTargetVisibleChanged;
        target.ParentChanged += OnTargetParentChanged;
        target.Disposed += OnTargetDisposed;
        RebuildAncestorSubscriptions();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _target.LocationChanged -= OnTargetGeometryChanged;
        _target.SizeChanged -= OnTargetGeometryChanged;
        _target.VisibleChanged -= OnTargetVisibleChanged;
        _target.ParentChanged -= OnTargetParentChanged;
        _target.Disposed -= OnTargetDisposed;
        UnsubscribeAncestors();
    }

    private void OnTargetGeometryChanged(object? sender, EventArgs e)
    {
        RequestReposition();
    }

    private void OnTargetVisibleChanged(object? sender, EventArgs e)
    {
        if (!_target.Visible)
        {
            RequestClose();
        }
        else
        {
            RequestReposition();
        }
    }

    private void OnTargetParentChanged(object? sender, EventArgs e)
    {
        RebuildAncestorSubscriptions();
        RequestReposition();
    }

    private void OnTargetDisposed(object? sender, EventArgs e)
    {
        RequestClose();
    }

    private void OnAncestorGeometryChanged(object? sender, EventArgs e)
    {
        RequestReposition();
    }

    private void OnAncestorScroll(object? sender, ScrollEventArgs e)
    {
        RequestReposition();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        RequestClose();
    }

    private void OnFormDeactivate(object? sender, EventArgs e)
    {
        RequestClose();
    }

    private void RebuildAncestorSubscriptions()
    {
        UnsubscribeAncestors();
        if (_disposed)
        {
            return;
        }

        var ancestor = _target.Parent;
        while (ancestor is not null)
        {
            _ancestors.Add(ancestor);
            ancestor.LocationChanged += OnAncestorGeometryChanged;
            ancestor.SizeChanged += OnAncestorGeometryChanged;
            if (ancestor is ScrollableControl scrollable)
            {
                _scrollableAncestors.Add(scrollable);
                scrollable.Scroll += OnAncestorScroll;
            }

            ancestor = ancestor.Parent;
        }

        _form = _target.FindForm();
        if (_form is not null)
        {
            _form.Move += OnAncestorGeometryChanged;
            _form.Resize += OnAncestorGeometryChanged;
            _form.FormClosed += OnFormClosed;
            _form.Deactivate += OnFormDeactivate;
        }
    }

    private void UnsubscribeAncestors()
    {
        foreach (var scrollable in _scrollableAncestors)
        {
            scrollable.Scroll -= OnAncestorScroll;
        }

        _scrollableAncestors.Clear();
        foreach (var ancestor in _ancestors)
        {
            ancestor.LocationChanged -= OnAncestorGeometryChanged;
            ancestor.SizeChanged -= OnAncestorGeometryChanged;
        }

        _ancestors.Clear();
        if (_form is not null)
        {
            _form.Move -= OnAncestorGeometryChanged;
            _form.Resize -= OnAncestorGeometryChanged;
            _form.FormClosed -= OnFormClosed;
            _form.Deactivate -= OnFormDeactivate;
            _form = null;
        }
    }

    private void RequestReposition()
    {
        if (!_disposed)
        {
            _reposition();
        }
    }

    private void RequestClose()
    {
        if (!_disposed)
        {
            _close();
        }
    }
}
