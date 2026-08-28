using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal enum BootstrapToastHostState
{
    Queued,
    Visible,
    Dismissing
}

/// <summary>
/// Hosts, owns, stacks, queues, and disposes <see cref="BootstrapToast"/> notifications.
/// </summary>
public class BootstrapToastContainer : Panel
{
    private sealed class ToastEntry
    {
        public ToastEntry(BootstrapToast toast)
        {
            Toast = toast;
        }

        public BootstrapToast Toast { get; }

        public BootstrapToastHostState State { get; set; } = BootstrapToastHostState.Queued;
    }

    private readonly List<ToastEntry> _entries = new List<ToastEntry>();
    private BootstrapToastPlacement _placement = BootstrapToastPlacement.TopRight;
    private int _toastSpacing = 8;
    private int _maximumVisibleToasts = 5;
    private bool _suppressPromotion;
    private bool _disposing;
    private bool _reflowing;

    /// <summary>
    /// Initializes a designer-safe toast container with top-right placement, eight logical pixels of spacing, and five visible slots.
    /// </summary>
    public BootstrapToastContainer()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        TabStop = false;
    }

    /// <summary>Gets or sets the corner used to anchor and grow the toast stack.</summary>
    [Category("Layout")]
    [Description("Selects the corner used to anchor the toast stack.")]
    [DefaultValue(BootstrapToastPlacement.TopRight)]
    public BootstrapToastPlacement Placement
    {
        get => _placement;
        set
        {
            ValidatePlacement(value);
            if (_placement == value)
            {
                return;
            }

            _placement = value;
            ReflowVisibleToasts();
        }
    }

    /// <summary>Gets or sets the logical 96-DPI spacing between visible toasts.</summary>
    [Category("Layout")]
    [Description("Sets the logical 96-DPI spacing between visible toasts.")]
    [DefaultValue(8)]
    public int ToastSpacing
    {
        get => _toastSpacing;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Toast spacing cannot be negative.");
            }

            if (_toastSpacing == value)
            {
                return;
            }

            _toastSpacing = value;
            ReflowVisibleToasts();
        }
    }

    /// <summary>Gets or sets the maximum number of toasts that may be visible at once.</summary>
    [Category("Behavior")]
    [Description("Sets the maximum number of toasts that may be visible at once; overflow remains queued in FIFO order.")]
    [DefaultValue(5)]
    public int MaximumVisibleToasts
    {
        get => _maximumVisibleToasts;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum visible toasts must be greater than zero.");
            }

            if (_maximumVisibleToasts == value)
            {
                return;
            }

            _maximumVisibleToasts = value;
            ReconcileVisibleCount();
            ReflowVisibleToasts();
            PromoteQueuedToasts();
        }
    }

    /// <summary>
    /// Transfers ownership of <paramref name="toast"/> to this container and either shows it or queues it.
    /// After a successful transfer the caller must not dispose, reparent, remove, or manually change the toast's visibility.
    /// </summary>
    /// <param name="toast">The unowned, undisposed toast to transfer.</param>
    public void ShowToast(BootstrapToast toast)
    {
        if (toast is null)
        {
            throw new ArgumentNullException(nameof(toast));
        }

        if (toast.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(toast));
        }

        if (toast.IsOwned)
        {
            throw new InvalidOperationException("The toast is already owned by a container.");
        }

        if (toast.Parent is not null)
        {
            throw new InvalidOperationException("The toast must not already have a parent when ownership is transferred.");
        }

        if (_disposing || IsDisposed)
        {
            throw new ObjectDisposedException(nameof(BootstrapToastContainer));
        }

        toast.Visible = false;
        toast.AttachOwner(RequestDismissal, OnToastPreferredHeightChanged);
        var entry = new ToastEntry(toast);
        _entries.Add(entry);
        Controls.Add(toast);

        if (CountVisibleEntries() < _maximumVisibleToasts)
        {
            Promote(entry);
        }
        else
        {
            toast.NotifyEnterStarted();
        }

        ReflowVisibleToasts();
    }

    /// <summary>
    /// Semantically dismisses every currently owned toast exactly once without promoting queued work during the bulk operation.
    /// </summary>
    public void DismissAll()
    {
        if (_disposing || IsDisposed || _entries.Count == 0)
        {
            return;
        }

        var snapshot = _entries.ToArray();
        _suppressPromotion = true;
        try
        {
            foreach (var entry in snapshot)
            {
                if (_entries.Contains(entry) && entry.State != BootstrapToastHostState.Dismissing)
                {
                    RequestDismissal(entry.Toast);
                }
            }
        }
        finally
        {
            _suppressPromotion = false;
        }

        PromoteQueuedToasts();
        ReflowVisibleToasts();
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ReflowVisibleToasts();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        RecomputeOwnedHeights();
        ReflowVisibleToasts();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            _suppressPromotion = true;

            foreach (var entry in _entries.ToArray())
            {
                var toast = entry.Toast;
                if (!toast.IsDisposed)
                {
                    toast.NotifyRemovedFromOwner();
                    Controls.Remove(toast);
                    toast.Dispose();
                }
            }

            _entries.Clear();
        }

        base.Dispose(disposing);
    }

    private void RequestDismissal(BootstrapToast toast)
    {
        if (_disposing || toast is null || toast.IsDisposed)
        {
            return;
        }

        var entry = _entries.FirstOrDefault(candidate => ReferenceEquals(candidate.Toast, toast));
        if (entry is null || entry.State == BootstrapToastHostState.Dismissing)
        {
            return;
        }

        entry.State = BootstrapToastHostState.Dismissing;
        toast.NotifyExitStarting();
        toast.RaiseDismissedFromOwner();
        RemoveEntryAndDispose(entry);

        if (!_suppressPromotion)
        {
            PromoteQueuedToasts();
            ReflowVisibleToasts();
        }
    }

    private void RemoveEntryAndDispose(ToastEntry entry)
    {
        var toast = entry.Toast;
        _entries.Remove(entry);

        if (!toast.IsDisposed)
        {
            toast.Visible = false;
            toast.NotifyRemovedFromOwner();
            Controls.Remove(toast);
            toast.Dispose();
        }
    }

    private void PromoteQueuedToasts()
    {
        if (_disposing || _suppressPromotion)
        {
            return;
        }

        while (CountVisibleEntries() < _maximumVisibleToasts)
        {
            var next = _entries.FirstOrDefault(entry => entry.State == BootstrapToastHostState.Queued);
            if (next is null)
            {
                break;
            }

            Promote(next);
        }
    }

    private void Promote(ToastEntry entry)
    {
        var toast = entry.Toast;
        if (toast.IsDisposed)
        {
            _entries.Remove(entry);
            return;
        }

        RecomputeHeight(toast);
        entry.State = BootstrapToastHostState.Visible;
        toast.NotifyEnterStarted();
        toast.Visible = true;
        ReflowVisibleToasts();
        toast.NotifyEnterCompleted();
    }

    private void ReconcileVisibleCount()
    {
        var visible = _entries.Where(entry => entry.State == BootstrapToastHostState.Visible).ToArray();
        for (var index = _maximumVisibleToasts; index < visible.Length; index++)
        {
            visible[index].State = BootstrapToastHostState.Queued;
            visible[index].Toast.NotifyEnterStarted();
            visible[index].Toast.Visible = false;
        }
    }

    private int CountVisibleEntries()
    {
        return _entries.Count(entry => entry.State == BootstrapToastHostState.Visible);
    }

    private void RecomputeOwnedHeights()
    {
        foreach (var entry in _entries)
        {
            if (!entry.Toast.IsDisposed)
            {
                RecomputeHeight(entry.Toast);
            }
        }
    }

    private static void RecomputeHeight(BootstrapToast toast)
    {
        var preferredHeight = toast.CalculatePreferredHeightForCurrentWidth();
        if (toast.Height != preferredHeight)
        {
            toast.Height = preferredHeight;
        }
    }

    private void OnToastPreferredHeightChanged(BootstrapToast toast)
    {
        if (_disposing || _reflowing || toast.IsDisposed)
        {
            return;
        }

        if (!_entries.Any(entry => ReferenceEquals(entry.Toast, toast)))
        {
            return;
        }

        RecomputeHeight(toast);
        ReflowVisibleToasts();
    }

    private void ReflowVisibleToasts()
    {
        if (_disposing || IsDisposed || _reflowing)
        {
            return;
        }

        var visible = _entries
            .Where(entry => entry.State == BootstrapToastHostState.Visible && !entry.Toast.IsDisposed)
            .ToArray();
        if (visible.Length == 0)
        {
            return;
        }

        _reflowing = true;
        try
        {
            foreach (var entry in visible)
            {
                RecomputeHeight(entry.Toast);
            }

            var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
            var bounds = BootstrapToastLayoutLogic.CalculateStackBounds(
                ClientRectangle,
                visible.Select(entry => entry.Toast.Size).ToArray(),
                _placement,
                _toastSpacing,
                _maximumVisibleToasts,
                dpi);

            for (var index = 0; index < visible.Length && index < bounds.Count; index++)
            {
                visible[index].Toast.Bounds = bounds[index];
            }
        }
        finally
        {
            _reflowing = false;
        }
    }

    private static void ValidatePlacement(BootstrapToastPlacement placement)
    {
        if (placement < BootstrapToastPlacement.TopLeft || placement > BootstrapToastPlacement.BottomRight)
        {
            throw new InvalidEnumArgumentException(nameof(placement), (int)placement, typeof(BootstrapToastPlacement));
        }
    }
}
