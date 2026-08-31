using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Hosts caller-owned interactive content in a Bootstrap-inspired native popup anchored to a target control.
/// </summary>
[DefaultEvent(nameof(Opened))]
public class BootstrapPopover : Component
{
    private readonly BootstrapOverlaySurface _surface;
    private readonly BootstrapOverlayDropDown _dropDown;
    private bool _disposed;
    private Control? _target;
    private Control? _content;
    private BootstrapPopoverTrigger _trigger = BootstrapPopoverTrigger.Click;
    private BootstrapOverlayPlacement _placement = BootstrapOverlayPlacement.Auto;
    private BootstrapOverlayCollisionBehavior _collisionBehavior = BootstrapOverlayCollisionBehavior.FlipAndShift;
    private int _offset = 8;
    private int _boundaryPadding = 8;
    private Padding _contentPadding = CreateDefaultContentPadding();
    private int _borderRadius = -1;
    private bool _closeOnEscape = true;
    private bool _closeOnClickOutside = true;
    private bool _restoreFocusAfterClose;
    private BootstrapOverlayAnchorTracker? _anchorTracker;
    private bool _themeSubscribed;

    /// <summary>
    /// Initializes a designer-safe interactive popover with an owned native overlay host.
    /// </summary>
    public BootstrapPopover()
    {
        _surface = new BootstrapOverlaySurface
        {
            LogicalContentPadding = _contentPadding,
            LogicalBorderRadius = _borderRadius
        };
        _dropDown = new BootstrapOverlayDropDown(_surface)
        {
            AutoClose = _closeOnClickOutside,
            CloseOnEscape = _closeOnEscape
        };
        _dropDown.EscapeRequested = OnEscapeRequested;
        _dropDown.TabNavigationRequested = OnTabNavigationRequested;
        _dropDown.Opened += OnDropDownOpened;
        _dropDown.Closed += OnDropDownClosed;
    }

    /// <summary>
    /// Initializes a popover and adds it to the supplied component container.
    /// </summary>
    /// <param name="container">The component container that owns this wrapper.</param>
    public BootstrapPopover(IContainer container)
        : this()
    {
        if (container is null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        container.Add(this);
    }

    /// <summary>
    /// Gets or sets the caller-owned control used as the activation target and placement anchor.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(null)]
    public Control? Target
    {
        get => _target;
        set
        {
            ThrowIfDisposed();
            if (ReferenceEquals(_target, value))
            {
                return;
            }

            if (IsOpen)
            {
                Hide();
            }

            DetachTargetHandlers(_target);
            _target = value is not null && value.IsDisposed ? null : value;
            AttachTargetHandlers(_target);
        }
    }

    /// <summary>
    /// Gets or sets the caller-owned, initially unparented interactive content control.
    /// The popover reparents but never disposes this control.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? Content
    {
        get => _content;
        set
        {
            ThrowIfDisposed();
            if (ReferenceEquals(_content, value))
            {
                return;
            }

            if (IsOpen)
            {
                throw new InvalidOperationException("Hide the popover before changing Content.");
            }

            ValidateNewContent(value);
            DetachContent();
            if (value is not null)
            {
                _surface.AttachContent(value);
                _content = value;
                value.Disposed += OnContentDisposed;
            }
        }
    }

    /// <summary>Gets or sets whether target clicks toggle this popover or application code controls it manually.</summary>
    [Category("Behavior")]
    [DefaultValue(BootstrapPopoverTrigger.Click)]
    public BootstrapPopoverTrigger Trigger
    {
        get => _trigger;
        set
        {
            ValidateTrigger(value);
            if (_trigger == value)
            {
                return;
            }

            DetachTargetClick(_target);
            _trigger = value;
            AttachTargetClick(_target);
        }
    }

    /// <summary>Gets or sets the preferred popup placement.</summary>
    [Category("Layout")]
    [DefaultValue(BootstrapOverlayPlacement.Auto)]
    public BootstrapOverlayPlacement Placement
    {
        get => _placement;
        set
        {
            ValidatePlacement(value);
            _placement = value;
            RepositionIfOpen();
        }
    }

    /// <summary>Gets or sets the popup boundary collision behavior.</summary>
    [Category("Layout")]
    [DefaultValue(BootstrapOverlayCollisionBehavior.FlipAndShift)]
    public BootstrapOverlayCollisionBehavior CollisionBehavior
    {
        get => _collisionBehavior;
        set
        {
            ValidateCollisionBehavior(value);
            _collisionBehavior = value;
            RepositionIfOpen();
        }
    }

    /// <summary>Gets or sets the logical 96-DPI gap between target and popup.</summary>
    [Category("Layout")]
    [DefaultValue(8)]
    public int Offset
    {
        get => _offset;
        set
        {
            ValidateNonNegative(value, "Popover offset cannot be negative.");
            _offset = value;
            RepositionIfOpen();
        }
    }

    /// <summary>Gets or sets the logical 96-DPI inset from the selected screen working area.</summary>
    [Category("Layout")]
    [DefaultValue(8)]
    public int BoundaryPadding
    {
        get => _boundaryPadding;
        set
        {
            ValidateNonNegative(value, "Popover boundary padding cannot be negative.");
            _boundaryPadding = value;
            RepositionIfOpen();
        }
    }

    /// <summary>Gets or sets logical content padding inside the popup chrome.</summary>
    [Category("Layout")]
    [DefaultValue(typeof(Padding), "12, 8, 12, 8")]
    public Padding ContentPadding
    {
        get => _contentPadding;
        set
        {
            ValidatePadding(value);
            _contentPadding = value;
            _surface.LogicalContentPadding = value;
            RepositionIfOpen();
        }
    }

    /// <summary>Gets or sets a logical uniform corner radius, or -1 to use the current theme radius.</summary>
    [Category("Appearance")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or non-negative.");
            }

            _borderRadius = value;
            _surface.LogicalBorderRadius = value;
            RepositionIfOpen();
        }
    }

    /// <summary>Gets or sets whether Escape closes the popover and restores focus to its target.</summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool CloseOnEscape
    {
        get => _closeOnEscape;
        set
        {
            _closeOnEscape = value;
            _dropDown.CloseOnEscape = value;
        }
    }

    /// <summary>Gets or sets whether a native outside click closes the popover.</summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool CloseOnClickOutside
    {
        get => _closeOnClickOutside;
        set
        {
            _closeOnClickOutside = value;
            _dropDown.AutoClose = value;
        }
    }

    /// <summary>Gets whether the native popup is currently open.</summary>
    [Browsable(false)]
    public bool IsOpen => _dropDown.Visible;

    /// <summary>Occurs after the native popup has opened.</summary>
    public event EventHandler? Opened;

    /// <summary>Occurs after the native popup has closed.</summary>
    public event EventHandler? Closed;

    /// <summary>Opens the configured popover at its current target.</summary>
    public void Show()
    {
        ThrowIfDisposed();
        if (IsOpen)
        {
            return;
        }

        var target = RequireTarget();
        var content = RequireContent();
        ApplyCurrentThemeAndDpi(target);
        content.PerformLayout();
        var popupSize = _surface.GetPreferredSize(Size.Empty);
        if (popupSize.Width <= 0 || popupSize.Height <= 0)
        {
            throw new InvalidOperationException("Popover content must have a positive preferred size.");
        }

        _dropDown.AutoClose = _closeOnClickOutside;
        _dropDown.CloseOnEscape = _closeOnEscape;
        _dropDown.ShowAt(CalculatePopupBounds(target, popupSize));
    }

    /// <summary>Closes the popover if it is open without disposing caller-owned content.</summary>
    public void Hide()
    {
        if (_disposed || !IsOpen)
        {
            return;
        }

        _dropDown.Close(ToolStripDropDownCloseReason.CloseCalled);
    }

    /// <summary>Toggles between the open and closed state.</summary>
    public void Toggle()
    {
        ThrowIfDisposed();
        if (IsOpen)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            Hide();
            _disposed = true;
            DetachTargetHandlers(_target);
            _target = null;
            DetachContent();
            StopOpenLifecycle();
            _dropDown.Opened -= OnDropDownOpened;
            _dropDown.Closed -= OnDropDownClosed;
            _dropDown.EscapeRequested = null;
            _dropDown.TabNavigationRequested = null;
            _dropDown.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnTargetClick(object? sender, EventArgs e)
    {
        if (_trigger == BootstrapPopoverTrigger.Click && ReferenceEquals(sender, _target))
        {
            Toggle();
        }
    }

    private void OnTargetDisposed(object? sender, EventArgs e)
    {
        var disposedTarget = sender as Control;
        if (disposedTarget is null || !ReferenceEquals(disposedTarget, _target))
        {
            return;
        }

        DetachTargetHandlers(disposedTarget);
        _target = null;
        Hide();
    }

    private void OnContentDisposed(object? sender, EventArgs e)
    {
        var disposedContent = sender as Control;
        if (disposedContent is null || !ReferenceEquals(disposedContent, _content))
        {
            return;
        }

        disposedContent.Disposed -= OnContentDisposed;
        _surface.DetachContent();
        _content = null;
        Hide();
    }

    private void OnDropDownOpened(object? sender, EventArgs e)
    {
        StartOpenLifecycle();
        FocusFirstContentControl();
        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void OnDropDownClosed(object? sender, ToolStripDropDownClosedEventArgs e)
    {
        StopOpenLifecycle();
        Closed?.Invoke(this, EventArgs.Empty);
        if (_restoreFocusAfterClose)
        {
            _restoreFocusAfterClose = false;
            var target = _target;
            if (target is not null && !target.IsDisposed && target.Visible && target.Enabled)
            {
                target.Focus();
            }
        }
    }

    private void OnEscapeRequested()
    {
        if (!_closeOnEscape)
        {
            return;
        }

        _restoreFocusAfterClose = true;
        Hide();
    }

    private void FocusFirstContentControl()
    {
        var first = FindFirstFocusable(_content);
        first?.Focus();
    }

    private bool OnTabNavigationRequested(bool forward)
    {
        var content = _content;
        if (content is null || content.IsDisposed)
        {
            return false;
        }

        var current = FindFocusedDescendant(content);
        if (current is null)
        {
            var initial = forward ? FindFirstFocusable(content) : FindLastFocusable(content);
            return initial is not null && initial.Focus();
        }

        var next = FindAdjacentFocusable(content, current, forward);
        if (next is not null && next.Focus())
        {
            return true;
        }

        return MoveFocusPastPopover(forward);
    }

    private static Control? FindFirstFocusable(Control? parent)
    {
        if (parent is null)
        {
            return null;
        }

        foreach (var control in EnumerateFocusableForward(parent))
        {
            return control;
        }

        return null;
    }

    private static Control? FindLastFocusable(Control? parent)
    {
        if (parent is null)
        {
            return null;
        }

        Control? last = null;
        foreach (var control in EnumerateFocusableForward(parent))
        {
            last = control;
        }

        return last;
    }

    private static Control? FindAdjacentFocusable(Control root, Control current, bool forward)
    {
        var currentFound = false;
        foreach (var candidate in EnumerateFocusable(root, forward))
        {
            if (currentFound)
            {
                return candidate;
            }

            currentFound = ReferenceEquals(candidate, current);
        }

        return null;
    }

    private static IEnumerable<Control> EnumerateFocusable(Control root, bool forward)
    {
        if (forward)
        {
            foreach (var control in EnumerateFocusableForward(root))
            {
                yield return control;
            }

            yield break;
        }

        var controls = new List<Control>(EnumerateFocusableForward(root));
        for (var index = controls.Count - 1; index >= 0; index--)
        {
            yield return controls[index];
        }
    }

    private static IEnumerable<Control> EnumerateFocusableForward(Control root)
    {
        var descendantFound = false;
        foreach (var descendant in EnumerateFocusableDescendants(root))
        {
            descendantFound = true;
            yield return descendant;
        }

        if (!descendantFound && IsFocusable(root))
        {
            yield return root;
        }
    }

    private static IEnumerable<Control> EnumerateFocusableDescendants(Control root)
    {
        Control? child = null;
        while ((child = root.GetNextControl(child, true)) is not null)
        {
            if (child is ContainerControl container)
            {
                // The parent scope's GetNextControl traversal stops at focus-managing
                // containers, so enumerate that container's native tab scope explicitly.
                var descendantFound = false;
                foreach (var descendant in EnumerateFocusableDescendants(container))
                {
                    descendantFound = true;
                    yield return descendant;
                }

                if (!descendantFound && IsFocusable(container))
                {
                    yield return container;
                }

                continue;
            }

            if (IsFocusable(child))
            {
                yield return child;
            }
        }
    }

    private static Control? FindFocusedDescendant(Control root)
    {
        foreach (Control child in root.Controls)
        {
            var focused = FindFocusedDescendant(child);
            if (focused is not null)
            {
                return focused;
            }
        }

        return root.Focused && IsFocusable(root) ? root : null;
    }

    private static bool IsFocusable(Control control)
    {
        return control.Visible && control.Enabled && control.TabStop && control.CanSelect;
    }

    private bool MoveFocusPastPopover(bool forward)
    {
        var target = _target;
        var parent = target?.Parent;
        var form = target?.FindForm();
        Hide();

        if (target is null || target.IsDisposed || !target.Visible || !target.Enabled)
        {
            return true;
        }

        if (parent is not null
            && !parent.IsDisposed
            && parent.SelectNextControl(target, forward, true, true, false))
        {
            return true;
        }

        if (form is not null
            && !form.IsDisposed
            && !ReferenceEquals(form, parent)
            && form.SelectNextControl(target, forward, true, true, false))
        {
            return true;
        }

        target.Focus();
        return true;
    }

    private void RepositionIfOpen()
    {
        if (!IsOpen || _target is null || _target.IsDisposed)
        {
            return;
        }

        ApplyCurrentThemeAndDpi(_target);
        _dropDown.MoveTo(CalculatePopupBounds(_target, _surface.GetPreferredSize(Size.Empty)));
    }

    private void RepositionOpenPopover()
    {
        var target = _target;
        if (target is null || target.IsDisposed || !target.Visible || !_dropDown.Visible)
        {
            Hide();
            return;
        }

        ApplyCurrentThemeAndDpi(target);
        _dropDown.MoveTo(CalculatePopupBounds(target, _surface.GetPreferredSize(Size.Empty)));
    }

    private void StartOpenLifecycle()
    {
        StopOpenLifecycle();
        var target = _target;
        if (target is null || target.IsDisposed)
        {
            return;
        }

        _anchorTracker = new BootstrapOverlayAnchorTracker(target, RepositionOpenPopover, Hide);
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
    }

    private void StopOpenLifecycle()
    {
        _anchorTracker?.Dispose();
        _anchorTracker = null;
        if (_themeSubscribed)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _themeSubscribed = false;
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        RepositionOpenPopover();
        _surface.Invalidate();
    }

    private void ApplyCurrentThemeAndDpi(Control target)
    {
        _surface.ApplyTheme(BootstrapThemeManager.CurrentTheme, GetControlDpi(target));
    }

    private Rectangle CalculatePopupBounds(Control target, Size popupSize)
    {
        var anchorBounds = target.RectangleToScreen(target.ClientRectangle);
        var dpi = GetControlDpi(target);
        var result = BootstrapOverlayPlacementEngine.Compute(new BootstrapOverlayPlacementRequest(
            anchorBounds,
            popupSize,
            Screen.FromRectangle(anchorBounds).WorkingArea,
            _placement,
            _collisionBehavior,
            DpiScaler.Scale(_offset, dpi),
            DpiScaler.Scale(_boundaryPadding, dpi),
            target.RightToLeft == RightToLeft.Yes));
        return result.Bounds;
    }

    private Control RequireTarget()
    {
        if (_target is null || _target.IsDisposed)
        {
            throw new InvalidOperationException("A live Target is required before showing a popover.");
        }

        if (!_target.Visible)
        {
            throw new InvalidOperationException("The Target must be visible before showing a popover.");
        }

        return _target;
    }

    private Control RequireContent()
    {
        if (_content is null || _content.IsDisposed)
        {
            throw new InvalidOperationException("Live Content is required before showing a popover.");
        }

        return _content;
    }

    private void AttachTargetHandlers(Control? target)
    {
        if (target is null)
        {
            return;
        }

        target.Disposed += OnTargetDisposed;
        AttachTargetClick(target);
    }

    private void DetachTargetHandlers(Control? target)
    {
        if (target is null)
        {
            return;
        }

        target.Disposed -= OnTargetDisposed;
        DetachTargetClick(target);
    }

    private void AttachTargetClick(Control? target)
    {
        if (target is not null && _trigger == BootstrapPopoverTrigger.Click)
        {
            target.Click -= OnTargetClick;
            target.Click += OnTargetClick;
        }
    }

    private void DetachTargetClick(Control? target)
    {
        if (target is not null)
        {
            target.Click -= OnTargetClick;
        }
    }

    private void DetachContent()
    {
        if (_content is null)
        {
            return;
        }

        _content.Disposed -= OnContentDisposed;
        _surface.DetachContent();
        _content = null;
    }

    private static void ValidateNewContent(Control? content)
    {
        if (content is null)
        {
            return;
        }

        if (content.IsDisposed)
        {
            throw new ArgumentException("Popover content cannot be disposed.", nameof(content));
        }

        if (content.Parent is not null)
        {
            throw new InvalidOperationException("Popover content must be unparented when assigned.");
        }
    }

    private static Padding CreateDefaultContentPadding()
    {
        var metrics = BootstrapThemeMetrics.Default;
        return new Padding(metrics.SpacingMD, metrics.SpacingSM, metrics.SpacingMD, metrics.SpacingSM);
    }

    private static int GetControlDpi(Control control)
    {
        return control.DeviceDpi > 0 ? control.DeviceDpi : DpiScaler.DefaultDpi;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BootstrapPopover));
        }
    }

    private static void ValidateTrigger(BootstrapPopoverTrigger trigger)
    {
        if (trigger < BootstrapPopoverTrigger.Click || trigger > BootstrapPopoverTrigger.Manual)
        {
            throw new ArgumentOutOfRangeException(nameof(trigger), trigger, "Unsupported popover trigger.");
        }
    }

    private static void ValidatePlacement(BootstrapOverlayPlacement placement)
    {
        if (placement < BootstrapOverlayPlacement.Auto || placement > BootstrapOverlayPlacement.RightEnd)
        {
            throw new ArgumentOutOfRangeException(nameof(placement), placement, "Unsupported overlay placement.");
        }
    }

    private static void ValidateCollisionBehavior(BootstrapOverlayCollisionBehavior behavior)
    {
        if (behavior < BootstrapOverlayCollisionBehavior.None || behavior > BootstrapOverlayCollisionBehavior.FlipAndShift)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "Unsupported overlay collision behavior.");
        }
    }

    private static void ValidateNonNegative(int value, string message)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, message);
        }
    }

    private static void ValidatePadding(Padding padding)
    {
        if (padding.Left < 0 || padding.Top < 0 || padding.Right < 0 || padding.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(padding), padding, "Popover content padding cannot contain negative edges.");
        }
    }
}
