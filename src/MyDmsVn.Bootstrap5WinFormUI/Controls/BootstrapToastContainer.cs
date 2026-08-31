using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal enum BootstrapToastHostState
{
    Queued,
    Entering,
    Visible,
    Exiting
}

/// <summary>
/// Hosts, owns, stacks, queues, animates, and disposes <see cref="BootstrapToast"/> notifications.
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

        public BootstrapAnimation? Transition { get; set; }
    }

    private sealed class ReflowGeometry
    {
        public ReflowGeometry(ToastEntry entry, Rectangle start, Rectangle target)
        {
            Entry = entry;
            Start = start;
            Target = target;
        }

        public ToastEntry Entry { get; }

        public Rectangle Start { get; }

        public Rectangle Target { get; }
    }

    private readonly List<ToastEntry> _entries = new List<ToastEntry>();
    private readonly Func<TimeSpan, Func<double, double>, Control, BootstrapAnimation> _animationFactory;
    private BootstrapToastPlacement _placement = BootstrapToastPlacement.TopRight;
    private int _toastSpacing = 8;
    private int _maximumVisibleToasts = 5;
    private int? _maximumStackHeightPixels;
    private BootstrapAnimation? _reflowAnimation;
    private bool _suppressPromotion;
    private bool _disposing;
    private bool _reflowingLayout;

    /// <summary>
    /// Initializes a designer-safe toast container with top-right placement, eight logical pixels of spacing, and five visible slots.
    /// </summary>
    public BootstrapToastContainer()
        : this((duration, easing, owner) => new BootstrapAnimation(duration, easing, owner))
    {
    }

    internal BootstrapToastContainer(
        Func<TimeSpan, Func<double, double>, Control, BootstrapAnimation> animationFactory)
    {
        _animationFactory = animationFactory ?? throw new ArgumentNullException(nameof(animationFactory));

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        TabStop = false;
        VisibleChanged += OnContainerVisibleChanged;
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
            StartReflow();
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
            ReconcileVisibleConstraints();
            PromoteQueuedToasts();
            StartReflow();
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
            ReconcileVisibleConstraints();
            PromoteQueuedToasts();
            StartReflow();
        }
    }

    internal int? MaximumStackHeightPixels
    {
        get => _maximumStackHeightPixels;
        set
        {
            if (value.HasValue && value.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum stack height must be greater than zero.");
            }

            if (_maximumStackHeightPixels == value)
            {
                return;
            }

            _maximumStackHeightPixels = value;
            RecomputeOwnedHeights();
            ReconcileVisibleConstraints();
            PromoteQueuedToasts();
            StartReflow();
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

        var wasVisible = toast.Visible;
        var entry = new ToastEntry(toast);
        try
        {
            toast.Visible = false;
            Controls.Add(toast);
            toast.AttachOwner(RequestDismissal, OnToastPreferredHeightChanged);
            toast.NotifyHostVisibilityChanged(Visible);
            _entries.Add(entry);

            toast.NotifyEnterStarted();
            PromoteQueuedToasts();
        }
        catch
        {
            CancelEntryTransition(entry);
            _entries.Remove(entry);

            if (toast.IsOwned)
            {
                toast.NotifyRemovedFromOwner();
            }

            if (!toast.IsDisposed && ReferenceEquals(toast.Parent, this))
            {
                Controls.Remove(toast);
            }

            if (!toast.IsDisposed)
            {
                toast.Visible = wasVisible;
            }

            throw;
        }
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
                if (_entries.Contains(entry) && entry.State != BootstrapToastHostState.Exiting)
                {
                    RequestDismissal(entry.Toast);
                }
            }
        }
        finally
        {
            _suppressPromotion = false;
        }
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        CancelReflow();
        SnapForHostGeometryChange();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        RecomputeOwnedHeights();
        ReconcileVisibleConstraints();
        PromoteQueuedToasts();
        CancelReflow();
        SnapForHostGeometryChange();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            _suppressPromotion = true;
            VisibleChanged -= OnContainerVisibleChanged;
            CancelReflow();

            foreach (var entry in _entries.ToArray())
            {
                CancelEntryTransition(entry);
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

    private void BeginEnter(ToastEntry entry)
    {
        if (_disposing || entry.Toast.IsDisposed || !_entries.Contains(entry))
        {
            return;
        }

        CancelReflow();
        CancelEntryTransition(entry);
        RecomputeHeight(entry.Toast);
        entry.State = BootstrapToastHostState.Entering;
        entry.Toast.NotifyEnterStarted();
        entry.Toast.NotifyHostVisibilityChanged(Visible);
        entry.Toast.Visible = true;

        var target = GetTargetBounds(entry);
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(
            BootstrapThemeManager.CurrentTheme.Metrics,
            GetCurrentDpi());
        var startX = IsLeftPlacement(_placement)
            ? target.X - metrics.SlideDistance
            : target.X + metrics.SlideDistance;
        var start = new Rectangle(startX, target.Y, target.Width, target.Height);
        entry.Toast.Bounds = start;

        var animation = _animationFactory(
            TimeSpan.FromMilliseconds(entry.Toast.AnimationDuration),
            BootstrapEasing.EaseOut,
            this);
        entry.Transition = animation;

        animation.ProgressChanged += (_, _) =>
        {
            if (_disposing || entry.Toast.IsDisposed || !ReferenceEquals(entry.Transition, animation))
            {
                return;
            }

            var currentTarget = GetTargetBounds(entry);
            entry.Toast.Bounds = new Rectangle(
                Lerp(start.X, currentTarget.X, animation.Progress),
                currentTarget.Y,
                currentTarget.Width,
                currentTarget.Height);
        };
        animation.Completed += (_, _) => CompleteEnter(entry, animation);
        animation.Start();
    }

    private void CompleteEnter(ToastEntry entry, BootstrapAnimation animation)
    {
        if (_disposing || entry.Toast.IsDisposed || !ReferenceEquals(entry.Transition, animation))
        {
            return;
        }

        entry.Transition = null;
        animation.Dispose();
        entry.State = BootstrapToastHostState.Visible;
        entry.Toast.Bounds = GetTargetBounds(entry);
        entry.Toast.NotifyHostVisibilityChanged(Visible);
        entry.Toast.NotifyEnterCompleted();
    }

    private void RequestDismissal(BootstrapToast toast)
    {
        if (_disposing || toast is null || toast.IsDisposed)
        {
            return;
        }

        var entry = _entries.FirstOrDefault(candidate => ReferenceEquals(candidate.Toast, toast));
        if (entry is null || entry.State == BootstrapToastHostState.Exiting)
        {
            return;
        }

        if (entry.State == BootstrapToastHostState.Queued)
        {
            entry.State = BootstrapToastHostState.Exiting;
            toast.NotifyExitStarting();
            toast.RaiseDismissedFromOwner();
            if (_disposing || IsDisposed || toast.IsDisposed || !_entries.Contains(entry))
            {
                return;
            }

            RemoveEntryAndDispose(entry);
            return;
        }

        CancelReflow();
        CancelEntryTransition(entry);
        var start = toast.Bounds;
        entry.State = BootstrapToastHostState.Exiting;
        toast.NotifyExitStarting();
        toast.RaiseDismissedFromOwner();
        if (_disposing || IsDisposed || toast.IsDisposed || !_entries.Contains(entry) ||
            entry.State != BootstrapToastHostState.Exiting)
        {
            return;
        }

        BeginExit(entry, start);
    }

    private void BeginExit(ToastEntry entry, Rectangle start)
    {
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(
            BootstrapThemeManager.CurrentTheme.Metrics,
            GetCurrentDpi());
        var endX = IsLeftPlacement(_placement)
            ? start.X - metrics.SlideDistance
            : start.X + metrics.SlideDistance;

        var animation = _animationFactory(
            TimeSpan.FromMilliseconds(entry.Toast.AnimationDuration),
            BootstrapEasing.EaseIn,
            this);
        entry.Transition = animation;

        animation.ProgressChanged += (_, _) =>
        {
            if (_disposing || entry.Toast.IsDisposed || !ReferenceEquals(entry.Transition, animation))
            {
                return;
            }

            entry.Toast.Bounds = new Rectangle(
                Lerp(start.X, endX, animation.Progress),
                start.Y,
                start.Width,
                start.Height);
        };
        animation.Completed += (_, _) => CompleteExit(entry, animation);
        animation.Start();
    }

    private void CompleteExit(ToastEntry entry, BootstrapAnimation animation)
    {
        if (_disposing || !ReferenceEquals(entry.Transition, animation))
        {
            return;
        }

        entry.Transition = null;
        animation.Dispose();
        RemoveEntryAndDispose(entry);

        if (_suppressPromotion)
        {
            return;
        }

        PromoteQueuedToasts();
        StartReflow();
    }

    private void RemoveEntryAndDispose(ToastEntry entry)
    {
        CancelEntryTransition(entry);
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

        while (CountOccupiedSlots() < _maximumVisibleToasts)
        {
            var next = _entries.FirstOrDefault(entry => entry.State == BootstrapToastHostState.Queued);
            if (next is null || !CanOccupyNextVisibleSlot(next))
            {
                break;
            }

            BeginEnter(next);
        }
    }

    private void ReconcileVisibleConstraints()
    {
        CancelReflow();
        while (!OccupiedStackFitsConstraints())
        {
            var candidate = _entries.LastOrDefault(entry =>
                entry.State == BootstrapToastHostState.Visible ||
                entry.State == BootstrapToastHostState.Entering);
            if (candidate is null)
            {
                break;
            }

            CancelEntryTransition(candidate);
            candidate.State = BootstrapToastHostState.Queued;
            candidate.Toast.NotifyEnterStarted();
            candidate.Toast.Visible = false;
        }
    }

    private bool OccupiedStackFitsConstraints()
    {
        var occupied = _entries
            .Where(entry => entry.State != BootstrapToastHostState.Queued && !entry.Toast.IsDisposed)
            .ToArray();
        if (occupied.Length > _maximumVisibleToasts)
        {
            return false;
        }

        return !_maximumStackHeightPixels.HasValue ||
               CalculateStackHeight(occupied) <= _maximumStackHeightPixels.Value;
    }

    private bool CanOccupyNextVisibleSlot(ToastEntry candidate)
    {
        if (!_maximumStackHeightPixels.HasValue)
        {
            return true;
        }

        RecomputeHeight(candidate.Toast);
        var occupied = _entries
            .Where(entry => entry.State != BootstrapToastHostState.Queued && !entry.Toast.IsDisposed)
            .Concat(new[] { candidate })
            .ToArray();
        return CalculateStackHeight(occupied) <= _maximumStackHeightPixels.Value;
    }

    private int CalculateStackHeight(IReadOnlyList<ToastEntry> entries)
    {
        return BootstrapToastLayoutLogic.CalculateRequiredStackHeight(
            entries.Select(entry => entry.Toast.Size).ToArray(),
            _toastSpacing,
            GetCurrentDpi());
    }

    private int CountOccupiedSlots()
    {
        return _entries.Count(entry => entry.State != BootstrapToastHostState.Queued);
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

    private void RecomputeHeight(BootstrapToast toast)
    {
        var preferredHeight = toast.CalculatePreferredHeightForCurrentWidth();
        var resolvedHeight = _maximumStackHeightPixels.HasValue
            ? Math.Min(preferredHeight, _maximumStackHeightPixels.Value)
            : preferredHeight;
        if (toast.Height != resolvedHeight)
        {
            toast.Height = resolvedHeight;
        }
    }

    private void OnToastPreferredHeightChanged(BootstrapToast toast)
    {
        if (_disposing || _reflowingLayout || toast.IsDisposed)
        {
            return;
        }

        var entry = _entries.FirstOrDefault(candidate => ReferenceEquals(candidate.Toast, toast));
        if (entry is null)
        {
            return;
        }

        RecomputeHeight(toast);
        ReconcileVisibleConstraints();
        PromoteQueuedToasts();
        if (entry.State == BootstrapToastHostState.Visible)
        {
            StartReflow();
        }
        else if (entry.State == BootstrapToastHostState.Entering)
        {
            SnapForHostGeometryChange();
        }
        else
        {
            StartReflow();
        }
    }

    private void StartReflow()
    {
        if (_disposing || IsDisposed || _reflowingLayout)
        {
            return;
        }

        CancelReflow();
        var targets = CalculateTargetMap();
        var stable = _entries
            .Where(entry => entry.State == BootstrapToastHostState.Visible && !entry.Toast.IsDisposed)
            .Select(entry => new ReflowGeometry(entry, entry.Toast.Bounds, targets[entry]))
            .Where(geometry => geometry.Start != geometry.Target)
            .ToArray();
        if (stable.Length == 0)
        {
            SnapForHostGeometryChange();
            return;
        }

        var duration = Math.Max(1, stable.Max(geometry => geometry.Entry.Toast.AnimationDuration));
        var animation = _animationFactory(
            TimeSpan.FromMilliseconds(duration),
            BootstrapEasing.EaseInOut,
            this);
        _reflowAnimation = animation;

        animation.ProgressChanged += (_, _) =>
        {
            if (_disposing || !ReferenceEquals(_reflowAnimation, animation))
            {
                return;
            }

            _reflowingLayout = true;
            try
            {
                foreach (var geometry in stable)
                {
                    if (!geometry.Entry.Toast.IsDisposed && geometry.Entry.State == BootstrapToastHostState.Visible)
                    {
                        geometry.Entry.Toast.Bounds = Lerp(geometry.Start, geometry.Target, animation.Progress);
                    }
                }
            }
            finally
            {
                _reflowingLayout = false;
            }
        };
        animation.Completed += (_, _) => CompleteReflow(animation, stable);
        animation.Start();
    }

    private void CompleteReflow(BootstrapAnimation animation, IReadOnlyList<ReflowGeometry> geometries)
    {
        if (_disposing || !ReferenceEquals(_reflowAnimation, animation))
        {
            return;
        }

        _reflowAnimation = null;
        animation.Dispose();
        _reflowingLayout = true;
        try
        {
            foreach (var geometry in geometries)
            {
                if (!geometry.Entry.Toast.IsDisposed && geometry.Entry.State == BootstrapToastHostState.Visible)
                {
                    geometry.Entry.Toast.Bounds = geometry.Target;
                }
            }
        }
        finally
        {
            _reflowingLayout = false;
        }
    }

    private void SnapForHostGeometryChange()
    {
        if (_disposing || IsDisposed || _reflowingLayout)
        {
            return;
        }

        var targets = CalculateTargetMap();
        _reflowingLayout = true;
        try
        {
            foreach (var pair in targets)
            {
                var entry = pair.Key;
                var target = pair.Value;
                if (entry.Toast.IsDisposed || entry.State == BootstrapToastHostState.Exiting)
                {
                    continue;
                }

                if (entry.State == BootstrapToastHostState.Visible)
                {
                    entry.Toast.Bounds = target;
                }
                else if (entry.State == BootstrapToastHostState.Entering)
                {
                    entry.Toast.Bounds = new Rectangle(
                        entry.Toast.Left,
                        target.Y,
                        target.Width,
                        target.Height);
                }
            }
        }
        finally
        {
            _reflowingLayout = false;
        }
    }

    private Dictionary<ToastEntry, Rectangle> CalculateTargetMap()
    {
        var active = _entries
            .Where(entry => entry.State != BootstrapToastHostState.Queued && !entry.Toast.IsDisposed)
            .ToArray();
        var result = new Dictionary<ToastEntry, Rectangle>();
        if (active.Length == 0)
        {
            return result;
        }

        foreach (var entry in active)
        {
            if (entry.State != BootstrapToastHostState.Exiting)
            {
                RecomputeHeight(entry.Toast);
            }
        }

        var bounds = BootstrapToastLayoutLogic.CalculateStackBounds(
            ClientRectangle,
            active.Select(entry => entry.Toast.Size).ToArray(),
            _placement,
            _toastSpacing,
            Math.Max(_maximumVisibleToasts, active.Length),
            GetCurrentDpi());
        for (var index = 0; index < active.Length && index < bounds.Count; index++)
        {
            result[active[index]] = bounds[index];
        }

        return result;
    }

    private Rectangle GetTargetBounds(ToastEntry entry)
    {
        var targets = CalculateTargetMap();
        return targets.TryGetValue(entry, out var target) ? target : entry.Toast.Bounds;
    }

    private void CancelEntryTransition(ToastEntry entry)
    {
        var animation = entry.Transition;
        entry.Transition = null;
        if (animation is null)
        {
            return;
        }

        animation.Stop();
        animation.Dispose();
    }

    private void CancelReflow()
    {
        var animation = _reflowAnimation;
        _reflowAnimation = null;
        if (animation is null)
        {
            return;
        }

        animation.Stop();
        animation.Dispose();
    }

    private void OnContainerVisibleChanged(object? sender, EventArgs e)
    {
        if (_disposing)
        {
            return;
        }

        foreach (var entry in _entries)
        {
            if (!entry.Toast.IsDisposed)
            {
                entry.Toast.NotifyHostVisibilityChanged(Visible);
            }
        }
    }

    private int GetCurrentDpi()
    {
        return DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
    }

    private static bool IsLeftPlacement(BootstrapToastPlacement placement)
    {
        return placement == BootstrapToastPlacement.TopLeft ||
               placement == BootstrapToastPlacement.BottomLeft;
    }

    private static int Lerp(int start, int end, double progress)
    {
        return (int)Math.Round(start + ((end - start) * progress), MidpointRounding.AwayFromZero);
    }

    private static Rectangle Lerp(Rectangle start, Rectangle end, double progress)
    {
        return new Rectangle(
            Lerp(start.X, end.X, progress),
            Lerp(start.Y, end.Y, progress),
            Lerp(start.Width, end.Width, progress),
            Lerp(start.Height, end.Height, progress));
    }

    private static void ValidatePlacement(BootstrapToastPlacement placement)
    {
        if (placement < BootstrapToastPlacement.TopLeft || placement > BootstrapToastPlacement.BottomRight)
        {
            throw new InvalidEnumArgumentException(nameof(placement), (int)placement, typeof(BootstrapToastPlacement));
        }
    }
}
