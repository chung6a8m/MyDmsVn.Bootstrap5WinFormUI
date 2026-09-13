using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides a Bootstrap-inspired modal shell while preserving native WinForms dialog behavior.</summary>
[DefaultProperty(nameof(Text))]
public class BootstrapModal : Form
{
    private readonly BootstrapModalSurface _surface;
    private readonly BootstrapModalHeader _header;
    private BootstrapModalSize _modalSize = BootstrapModalSize.Default;
    private BootstrapModalBackdropMode _backdropMode = BootstrapModalBackdropMode.Dismissible;
    private bool _closeOnEscape = true;
    private bool _showCloseButton = true;
    private int _borderRadius = -1;
    private bool _frameworkDismissPending;
    private bool _themeSubscribed;
    private bool _disposing;
    private string _lastFallbackAccessibleName = string.Empty;
    private BootstrapModalBackdropWindow? _backdropWindow;
    private BootstrapModalOwnerTracker? _ownerTracker;
    private BootstrapModalOwnerContext _ownerContext;
    private readonly BootstrapModalTransitionController _transitionController;

    /// <summary>Initializes a designer-safe native modal form.</summary>
    public BootstrapModal()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        MinimizeBox = false;
        MaximizeBox = false;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 300);

        _header = new BootstrapModalHeader();
        _surface = new BootstrapModalSurface(_header);
        _transitionController = new BootstrapModalTransitionController(this);
        _header.DismissRequested += OnHeaderDismissRequested;
        Controls.Add(_surface);
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyTheme();
        UpdateShellMetrics();
    }

    /// <summary>Gets or sets the framework preferred-size mode.</summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapModalSize.Default)]
    public BootstrapModalSize ModalSize
    {
        get => _modalSize;
        set
        {
            if (!Enum.IsDefined(typeof(BootstrapModalSize), value)) throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(BootstrapModalSize));
            if (_modalSize == value) return;
            _modalSize = value;
            if (Visible) ResolveBoundsForShow();
            PerformLayout();
        }
    }

    /// <summary>Gets or sets the backdrop interaction mode.</summary>
    [Category("Behavior")]
    [DefaultValue(BootstrapModalBackdropMode.Dismissible)]
    public BootstrapModalBackdropMode BackdropMode
    {
        get => _backdropMode;
        set
        {
            if (!Enum.IsDefined(typeof(BootstrapModalBackdropMode), value)) throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(BootstrapModalBackdropMode));
            if (_backdropMode == value) return;
            _backdropMode = value;
            if (Visible) EnsureBackdrop();
        }
    }

    /// <summary>Gets or sets whether Escape dismisses when no native cancel button owns it.</summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool CloseOnEscape { get => _closeOnEscape; set => _closeOnEscape = value; }

    /// <summary>Gets or sets whether the framework header close affordance is visible.</summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowCloseButton
    {
        get => _showCloseButton;
        set
        {
            if (_showCloseButton == value) return;
            _showCloseButton = value;
            _header.ShowCloseButton = value;
        }
    }

    /// <summary>Gets or sets the logical corner radius, or -1 to use the theme radius.</summary>
    [Category("Appearance")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1) throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or a non-negative value.");
            if (_borderRadius == value) return;
            _borderRadius = value;
            UpdateShellMetrics();
        }
    }

    /// <summary>Gets or sets the preferred descendant to focus when the modal first opens.</summary>
    [Browsable(false)]
    [DefaultValue(null)]
    public Control? InitialFocusControl { get; set; }

    /// <summary>Gets the scrollable modal body container.</summary>
    [Category("Layout")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Panel BodyPanel => _surface.BodyPanel;

    /// <summary>Gets the trailing modal action container.</summary>
    [Category("Layout")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public FlowLayoutPanel FooterPanel => _surface.FooterPanel;

    internal bool FrameworkDismissPending => _frameworkDismissPending;
    internal BootstrapModalBackdropWindow? BackdropWindow => _backdropWindow;
    internal BootstrapModalTransitionController TransitionController => _transitionController;

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (_header is not null) _header.Title = Text;
        if (string.IsNullOrEmpty(AccessibleName) || AccessibleName == _lastFallbackAccessibleName)
        {
            AccessibleName = Text;
            _lastFallbackAccessibleName = Text;
        }
    }

    /// <inheritdoc />
    protected override void OnShown(EventArgs e)
    {
        ResolveBoundsForShow();
        _surface.RefreshFooterVisibility();
        EnsureBackdrop();
        base.OnShown(e);
        BringToFront();
        BootstrapModalFocusLogic.ApplyInitialFocus(this, InitialFocusControl);
        _transitionController.BeginOpen();
    }

    /// <inheritdoc />
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Escape && CancelButton is null)
        {
            if (!CloseOnEscape) return false;
            RequestDismiss();
            return true;
        }

        return base.ProcessDialogKey(keyData);
    }

    /// <inheritdoc />
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_frameworkDismissPending && e.Cancel)
        {
            _frameworkDismissPending = false;
            DialogResult = DialogResult.None;
        }
    }

    /// <inheritdoc />
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _transitionController.Stop();
        CleanupBackdrop();
        _frameworkDismissPending = false;
        base.OnFormClosed(e);
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateShellMetrics();
        if (Visible) ResolveBoundsForShow();
    }

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        UpdateShellMetrics(e.DeviceDpiNew);
        if (Visible) ResolveBoundsForShow(e.DeviceDpiNew);
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateWindowRegion();
    }

    /// <inheritdoc />
    protected override void OnRightToLeftChanged(EventArgs e)
    {
        base.OnRightToLeftChanged(e);
        ApplyRightToLeftLayout();
    }

    /// <inheritdoc />
    protected override void OnRightToLeftLayoutChanged(EventArgs e)
    {
        base.OnRightToLeftLayoutChanged(e);
        ApplyRightToLeftLayout();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }
            _header.DismissRequested -= OnHeaderDismissRequested;
            CleanupBackdrop();
            _transitionController.Dispose();
        }
        base.Dispose(disposing);
    }

    internal void RequestDismiss()
    {
        if (IsDisposed) return;
        if (_frameworkDismissPending)
        {
            if (DialogResult != DialogResult.None) return;
            _frameworkDismissPending = false;
        }

        _frameworkDismissPending = true;
        if (DialogResult == DialogResult.None) DialogResult = DialogResult.Cancel;
    }

    private void OnHeaderDismissRequested(object? sender, EventArgs e) => RequestDismiss();

    private void ApplyRightToLeftLayout()
    {
        if (_header is null || _surface is null) return;
        var rtl = RightToLeft == RightToLeft.Yes && RightToLeftLayout;
        _header.ApplyRightToLeft(rtl);
        FooterPanel.FlowDirection = rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
        PerformLayout();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (_disposing || IsDisposed) return;
        ApplyTheme();
        UpdateShellMetrics();
        if (e.NewTheme.ReducedMotion) _transitionController.Stop();
        Invalidate(true);
    }

    private void ApplyTheme()
    {
        var state = BootstrapModalRenderLogic.Resolve(BootstrapThemeManager.CurrentTheme);
        BackColor = state.Surface;
        ForeColor = state.Text;
        _surface.ApplyVisualState(state);
        _header.ApplyVisualState(state);
        _header.ApplyTypography(BootstrapThemeManager.CurrentTheme.Typography);
        _backdropWindow?.ApplyVisualState(state);
    }

    private void UpdateShellMetrics(int? targetDpi = null)
    {
        var dpi = targetDpi.GetValueOrDefault(DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi);
        var metrics = BootstrapModalLayoutLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, BorderRadius, dpi);
        _surface.ApplyMetrics(metrics);
        _header.ApplyMetrics(metrics);
        UpdateWindowRegion();
        PerformLayout();
    }

    private void ResolveBoundsForShow(int? targetDpi = null)
    {
        var ownerContext = BootstrapModalOwnerContext.Resolve(this);
        var hasNativeOwner = MyDmsVn.Bootstrap5WinFormUI.Compatibility.BootstrapModalNativeWindow.IsUsable(ownerContext.OwnerHandle);
        var screen = ownerContext.ManagedOwner is not null
            ? Screen.FromControl(ownerContext.ManagedOwner)
            : hasNativeOwner
                ? Screen.FromHandle(ownerContext.OwnerHandle)
                : Screen.FromControl(this);
        var working = screen.WorkingArea;
        var ownerBounds = ownerContext.OwnerBounds.Width > 0 && ownerContext.OwnerBounds.Height > 0
            ? ownerContext.OwnerBounds
            : working;
        var dpi = targetDpi.GetValueOrDefault(DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi);
        var targetWidth = BootstrapModalLayoutLogic.ResolveDialogSize(ModalSize, Size, 1, MinimumSize, MaximumSize, working.Size, dpi).Width;
        var measurementSize = new Size(targetWidth, Math.Min(Math.Max(1, Height), working.Height));
        var measurementBounds = BootstrapModalLayoutLogic.CenterAndClamp(measurementSize, ownerBounds, working);
        MyDmsVn.Bootstrap5WinFormUI.Compatibility.BootstrapModalNativeWindow.ApplyResolvedBounds(this, measurementBounds);
        PerformLayout();
        var chromeHeight = _surface.ResolveChromeAndContentHeight();
        var resolved = BootstrapModalLayoutLogic.ResolveDialogSize(ModalSize, Size, chromeHeight, MinimumSize, MaximumSize, working.Size, dpi);
        var resolvedBounds = BootstrapModalLayoutLogic.CenterAndClamp(resolved, ownerBounds, working);
        MyDmsVn.Bootstrap5WinFormUI.Compatibility.BootstrapModalNativeWindow.ApplyResolvedBounds(this, resolvedBounds);
    }

    private void EnsureBackdrop()
    {
        CleanupBackdrop();
        if (!Modal || BackdropMode == BootstrapModalBackdropMode.None || _disposing || IsDisposed) return;

        _ownerContext = BootstrapModalOwnerContext.Resolve(this);
        var visual = BootstrapModalRenderLogic.Resolve(BootstrapThemeManager.CurrentTheme);
        var backdrop = new BootstrapModalBackdropWindow(visual);
        backdrop.BackdropClicked += OnBackdropClicked;
        _backdropWindow = backdrop;
        UpdateBackdropBounds();

        if (_ownerContext.ManagedOwner is not null)
            backdrop.Show(_ownerContext.ManagedOwner);
        else if (MyDmsVn.Bootstrap5WinFormUI.Compatibility.BootstrapModalNativeWindow.IsUsable(_ownerContext.OwnerHandle))
            backdrop.Show(new BootstrapModalNativeOwnerWindow(_ownerContext.OwnerHandle));
        else
            backdrop.Show();

        _ownerTracker = new BootstrapModalOwnerTracker(_ownerContext.ManagedOwner, UpdateBackdropBounds, CleanupBackdrop);
        BringToFront();
    }

    private void UpdateBackdropBounds()
    {
        if (_backdropWindow is null || _backdropWindow.IsDisposed) return;
        if (_ownerContext.ManagedOwner is not null && !_ownerContext.ManagedOwner.IsDisposed)
        {
            _backdropWindow.Bounds = _ownerContext.ManagedOwner.Bounds;
            return;
        }

        if (MyDmsVn.Bootstrap5WinFormUI.Compatibility.BootstrapModalNativeWindow.TryGetBounds(_ownerContext.OwnerHandle, out var nativeBounds))
        {
            _backdropWindow.Bounds = nativeBounds;
            return;
        }

        _backdropWindow.Bounds = Screen.FromControl(this).WorkingArea;
    }

    private void CleanupBackdrop()
    {
        _ownerTracker?.Dispose();
        _ownerTracker = null;
        var backdrop = _backdropWindow;
        _backdropWindow = null;
        if (backdrop is null) return;
        backdrop.BackdropClicked -= OnBackdropClicked;
        if (!backdrop.IsDisposed) backdrop.Dispose();
        _ownerContext = default;
    }

    private void OnBackdropClicked(object? sender, EventArgs e)
    {
        if (BackdropMode == BootstrapModalBackdropMode.Dismissible) RequestDismiss();
    }

    private void UpdateWindowRegion()
    {
        if (_surface is null || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var logicalRadius = BorderRadius >= 0 ? BorderRadius : BootstrapThemeManager.CurrentTheme.Metrics.Radius;
        var radius = DpiScaler.Scale((float)logicalRadius, dpi);
        using var path = RoundedPath.Create(new RectangleF(0, 0, Width, Height), new CornerRadius(radius));
        var replacement = new Region(path);
        var previous = Region;
        Region = replacement;
        previous?.Dispose();
    }
}
