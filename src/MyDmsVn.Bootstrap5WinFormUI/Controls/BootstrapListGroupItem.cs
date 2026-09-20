using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Hosts simple text or arbitrary WinForms content in a list-group row.</summary>
[DefaultEvent(nameof(Click))]
[DefaultProperty(nameof(Text))]
public class BootstrapListGroupItem : Panel
{
    private bool _active;
    private bool _actionable;
    private BootstrapVariant? _variant;
    private bool _useThemeFont = true;
    private bool _settingThemeFont;
    private bool _themeSubscribed;
    private Font? _themeFont;
    private CornerRadius? _connectedCorners;
    private bool _flushVertical;
    private bool _hovered;
    private bool _pressed;
    private bool _spacePressed;
    private Control? _hoveredDecorativeControl;
    private Control? _forwardingPressedControl;
    private readonly HashSet<Control> _trackedDescendants = new HashSet<Control>();
    private string? _automaticAccessibleName;
    private Size _explicitMinimumSize;
    private Size _lastObservedSize;
    private bool _trackExplicitSize;
    private bool _applyingGroupLayoutBounds;

    /// <summary>Initializes a neutral, presentational list-group item.</summary>
    public BootstrapListGroupItem()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        SetStyle(ControlStyles.StandardClick, false);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        AccessibleRole = AccessibleRole.ListItem;
        AccessibleDescription = "Bootstrap-inspired list-group item.";
        BackColor = Color.Transparent;
        Size = new Size(320, 44);
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        UpdateResolvedForeground();
        _lastObservedSize = Size;
        _trackExplicitSize = true;
        Layout += OnItemLayout;
        AutoSizeChanged += OnItemAutoSizeChanged;
        SizeChanged += OnItemSizeChanged;
    }

    /// <summary>Gets or sets the application-owned active presentation state.</summary>
    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value)
            {
                return;
            }

            _active = value;
            UpdateResolvedForeground();
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether the item itself is a selectable activation target.</summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool Actionable
    {
        get => _actionable;
        set
        {
            if (_actionable == value) return;
            _actionable = value;
            SetStyle(ControlStyles.Selectable, value);
            TabStop = value;
            AccessibleRole = value ? AccessibleRole.PushButton : AccessibleRole.ListItem;
            if (!value)
            {
                ClearPressedState();
            }
            UpdateStyles();
            UpdateResolvedForeground();
            Invalidate();
        }
    }

    /// <summary>Gets or sets an optional semantic contextual variant; null uses neutral theme colors.</summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public BootstrapVariant? Variant
    {
        get => _variant;
        set
        {
            if (value.HasValue && (value.Value < BootstrapVariant.Primary || value.Value > BootstrapVariant.Dark))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported Bootstrap variant.");
            if (_variant == value) return;
            _variant = value;
            UpdateResolvedForeground();
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether theme typography owns the item font.</summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    public bool UseThemeFont
    {
        get => _useThemeFont;
        set
        {
            if (_useThemeFont == value) return;
            _useThemeFont = value;
            if (value) ApplyThemeFont(); else DisposeThemeFont();
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var padding = Padding == Padding.Empty ? BootstrapListGroupRenderLogic.GetContentPadding(theme.Metrics, dpi) : Padding;
        var textSize = string.IsNullOrEmpty(Text)
            ? Size.Empty
            : TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        var contentSize = Size.Empty;
        foreach (Control child in Controls)
        {
            if (!child.Visible) continue;
            var preferred = child.GetPreferredSize(Size.Empty);
            contentSize.Width = Math.Max(contentSize.Width, GetHorizontalContentExtent(child, preferred));
            contentSize.Height = Math.Max(contentSize.Height, GetVerticalContentExtent(child, preferred));
        }
        var contentPreferred = BootstrapListGroupRenderLogic.GetPreferredSize(textSize, contentSize, padding);
        var resolvedPreferred = AutoSize
            ? contentPreferred
            : new Size(
                Math.Max(contentPreferred.Width, _explicitMinimumSize.Width),
                Math.Max(contentPreferred.Height, _explicitMinimumSize.Height));
        resolvedPreferred.Width = Math.Max(resolvedPreferred.Width, MinimumSize.Width);
        resolvedPreferred.Height = Math.Max(resolvedPreferred.Height, MinimumSize.Height);
        if (MaximumSize.Width > 0)
        {
            resolvedPreferred.Width = Math.Min(resolvedPreferred.Width, MaximumSize.Width);
        }

        if (MaximumSize.Height > 0)
        {
            resolvedPreferred.Height = Math.Min(resolvedPreferred.Height, MaximumSize.Height);
        }

        return resolvedPreferred;
    }

    internal void ApplyGroupLayoutBounds(Rectangle bounds)
    {
        _applyingGroupLayoutBounds = true;
        try
        {
            Bounds = bounds;
        }
        finally
        {
            _applyingGroupLayoutBounds = false;
        }
    }

    internal void ApplyConnectedGeometry(CornerRadius corners, bool flushVertical)
    {
        if (_connectedCorners == corners && _flushVertical == flushVertical) return;
        _connectedCorners = corners;
        _flushVertical = flushVertical;
        Invalidate();
    }

    internal CornerRadius ConnectedCorners => _connectedCorners ?? CornerRadius.Empty;

    internal bool FlushVerticalGeometry => _flushVertical;

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (string.IsNullOrEmpty(AccessibleName) || AccessibleName == _automaticAccessibleName)
        {
            _automaticAccessibleName = Text;
            AccessibleName = Text;
        }
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_settingThemeFont) { _useThemeFont = false; DisposeThemeFont(); }
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); PerformLayout(); Invalidate(); }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        UpdateResolvedForeground();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is Control control) TrackDescendant(control);
        RequestOwningLayout();
    }

    /// <inheritdoc />
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is Control control) UntrackDescendant(control);
        base.OnControlRemoved(e);
        RequestOwningLayout();
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (_actionable && Enabled)
        {
            _hovered = true;
            UpdateResolvedForeground();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        UpdateResolvedForeground();
        if (!_pressed) Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && _actionable && Enabled && ClientRectangle.Contains(e.Location))
        {
            Focus();
            _pressed = true;
            Capture = true;
            UpdateResolvedForeground();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        var activate = _pressed && e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location);
        _pressed = false;
        Capture = false;
        base.OnMouseUp(e);
        UpdateResolvedForeground();
        Invalidate();
        if (activate) ActivateItem();
    }

    /// <inheritdoc />
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (!Capture && _pressed)
        {
            _pressed = false;
            UpdateResolvedForeground();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e)
    {
        ClearPressedState();
        base.OnLostFocus(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

    /// <inheritdoc />
    protected override bool IsInputKey(Keys keyData)
    {
        var key = keyData & Keys.KeyCode;
        if (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right || key == Keys.Home || key == Keys.End)
            return true;
        return base.IsInputKey(keyData);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!_actionable || !Enabled) return;
        if (e.KeyCode == Keys.Enter)
        {
            ActivateItem();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Space && !_spacePressed)
        {
            _spacePressed = true;
            _pressed = true;
            UpdateResolvedForeground();
            Invalidate();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode == Keys.Space && _spacePressed)
        {
            _spacePressed = false;
            _pressed = false;
            UpdateResolvedForeground();
            Invalidate();
            ActivateItem();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    /// <inheritdoc />
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (Focused && Parent is BootstrapListGroup group && group.NavigateFrom(this, keyData))
            return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); PerformLayout(); Invalidate(); }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var palette = ResolveCurrentPalette(theme.Colors);
        var radius = BootstrapListGroupRenderLogic.ResolveRadius(theme.Metrics, -1, dpi);
        var corners = _connectedCorners ?? new CornerRadius(radius);
        var borderWidth = Math.Max(1, DpiScaler.Scale(theme.Metrics.BorderWidth, dpi));
        var bounds = new RectangleF(borderWidth / 2f, borderWidth / 2f, Math.Max(0f, ClientSize.Width - borderWidth), Math.Max(0f, ClientSize.Height - borderWidth));
        if (bounds.Width <= 0f || bounds.Height <= 0f) return;
        var smoothing = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(bounds, corners);
            using var brush = new SolidBrush(palette.Surface);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(palette.Border, borderWidth);
            if (_flushVertical)
            {
                e.Graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Top);
                e.Graphics.DrawLine(pen, bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);
            }
            else e.Graphics.DrawPath(pen, path);
        }
        finally { e.Graphics.SmoothingMode = smoothing; }

        if (Controls.Count == 0 && !string.IsNullOrEmpty(Text))
        {
            var padding = Padding == Padding.Empty ? BootstrapListGroupRenderLogic.GetContentPadding(theme.Metrics, dpi) : Padding;
            var textBounds = new Rectangle(padding.Left, padding.Top, Math.Max(0, ClientSize.Width - padding.Horizontal), Math.Max(0, ClientSize.Height - padding.Vertical));
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, palette.Foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }
        if (_actionable && Focused && ShowFocusCues)
        {
            var focusBounds = Rectangle.Inflate(ClientRectangle, -borderWidth - 1, -borderWidth - 1);
            if (focusBounds.Width > 0 && focusBounds.Height > 0)
                ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds, palette.Focus, palette.Surface);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Layout -= OnItemLayout;
            AutoSizeChanged -= OnItemAutoSizeChanged;
            SizeChanged -= OnItemSizeChanged;
            foreach (var descendant in new List<Control>(_trackedDescendants)) UntrackDescendant(descendant);
            if (_themeSubscribed) { BootstrapThemeManager.ThemeChanged -= OnThemeChanged; _themeSubscribed = false; }
            DisposeThemeFont();
        }
        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed) return;
        if (_useThemeFont) ApplyThemeFont();
        UpdateResolvedForeground();
        PerformLayout();
        Invalidate();
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _settingThemeFont = true;
        try { Font = next; }
        finally { _settingThemeFont = false; }
        if (previous is not null && ReferenceEquals(Font, previous)) { next.Dispose(); return; }
        _themeFont = next;
        previous?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        if (font is null)
        {
            return;
        }

        if (ReferenceEquals(Font, font))
        {
            _settingThemeFont = true;
            try
            {
                Font = null!;
            }
            finally
            {
                _settingThemeFont = false;
            }
        }

        font.Dispose();
    }

    private void ActivateItem()
    {
        if (_actionable && Enabled) OnClick(EventArgs.Empty);
    }

    private void ClearPressedState()
    {
        var stateChanged = _pressed || _spacePressed;
        _pressed = false;
        _spacePressed = false;
        _forwardingPressedControl = null;
        if (Capture) Capture = false;
        if (stateChanged)
        {
            UpdateResolvedForeground();
        }
    }

    private BootstrapListGroupPalette ResolveCurrentPalette(BootstrapThemeColors colors)
    {
        var state = BootstrapListGroupRenderLogic.ResolveState(Enabled, _active, _actionable, _pressed, _hovered);
        return BootstrapListGroupRenderLogic.ResolvePalette(colors, _variant, state);
    }

    private void UpdateResolvedForeground()
    {
        if (IsDisposed)
        {
            return;
        }

        var foreground = ResolveCurrentPalette(BootstrapThemeManager.CurrentTheme.Colors).Foreground;
        if (ForeColor != foreground)
        {
            ForeColor = foreground;
        }
    }

    private int GetHorizontalContentExtent(Control child, Size preferred)
    {
        switch (child.Dock)
        {
            case DockStyle.Fill:
            case DockStyle.Top:
            case DockStyle.Bottom:
                return Math.Max(0, preferred.Width);
            case DockStyle.Left:
            case DockStyle.Right:
                return Math.Max(0, child.Width);
        }

        var anchoredLeft = (child.Anchor & AnchorStyles.Left) == AnchorStyles.Left;
        var anchoredRight = (child.Anchor & AnchorStyles.Right) == AnchorStyles.Right;
        if (anchoredLeft && anchoredRight)
        {
            var farEdgeDistance = Math.Max(0, ClientSize.Width - child.Right);
            return Math.Max(0, child.Left) + Math.Max(0, preferred.Width) + farEdgeDistance;
        }

        if (anchoredRight)
        {
            var farEdgeDistance = Math.Max(0, ClientSize.Width - child.Right);
            return Math.Max(0, child.Width) + farEdgeDistance;
        }

        return Math.Max(0, child.Right);
    }

    private int GetVerticalContentExtent(Control child, Size preferred)
    {
        switch (child.Dock)
        {
            case DockStyle.Fill:
            case DockStyle.Left:
            case DockStyle.Right:
                return Math.Max(0, preferred.Height);
            case DockStyle.Top:
            case DockStyle.Bottom:
                return Math.Max(0, child.Height);
        }

        var anchoredTop = (child.Anchor & AnchorStyles.Top) == AnchorStyles.Top;
        var anchoredBottom = (child.Anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom;
        if (anchoredTop && anchoredBottom)
        {
            var farEdgeDistance = Math.Max(0, ClientSize.Height - child.Bottom);
            return Math.Max(0, child.Top) + Math.Max(0, preferred.Height) + farEdgeDistance;
        }

        if (anchoredBottom)
        {
            var farEdgeDistance = Math.Max(0, ClientSize.Height - child.Bottom);
            return Math.Max(0, child.Height) + farEdgeDistance;
        }

        return Math.Max(0, child.Bottom);
    }

    private void TrackDescendant(Control control)
    {
        if (!_trackedDescendants.Add(control)) return;
        control.ControlAdded += OnDescendantControlAdded;
        control.ControlRemoved += OnDescendantControlRemoved;
        control.Layout += OnDescendantLayout;
        control.LocationChanged += OnDescendantPreferredSizeChanged;
        control.SizeChanged += OnDescendantPreferredSizeChanged;
        control.VisibleChanged += OnDescendantPreferredSizeChanged;
        control.TextChanged += OnDescendantPreferredSizeChanged;
        control.FontChanged += OnDescendantPreferredSizeChanged;
        control.PaddingChanged += OnDescendantPreferredSizeChanged;
        control.DockChanged += OnDescendantPreferredSizeChanged;
        if (IsDecorativeForwardingSurface(control))
        {
            control.MouseEnter += OnDecorativeMouseEnter;
            control.MouseDown += OnDecorativeMouseDown;
            control.MouseUp += OnDecorativeMouseUp;
            control.MouseLeave += OnDecorativeMouseLeave;
        }
        foreach (Control child in control.Controls) TrackDescendant(child);
    }

    private void UntrackDescendant(Control control)
    {
        foreach (Control child in control.Controls) UntrackDescendant(child);
        if (!_trackedDescendants.Remove(control)) return;
        control.ControlAdded -= OnDescendantControlAdded;
        control.ControlRemoved -= OnDescendantControlRemoved;
        control.Layout -= OnDescendantLayout;
        control.LocationChanged -= OnDescendantPreferredSizeChanged;
        control.SizeChanged -= OnDescendantPreferredSizeChanged;
        control.VisibleChanged -= OnDescendantPreferredSizeChanged;
        control.TextChanged -= OnDescendantPreferredSizeChanged;
        control.FontChanged -= OnDescendantPreferredSizeChanged;
        control.PaddingChanged -= OnDescendantPreferredSizeChanged;
        control.DockChanged -= OnDescendantPreferredSizeChanged;
        control.MouseEnter -= OnDecorativeMouseEnter;
        control.MouseDown -= OnDecorativeMouseDown;
        control.MouseUp -= OnDecorativeMouseUp;
        control.MouseLeave -= OnDecorativeMouseLeave;
        var interactionStateChanged = false;
        if (ReferenceEquals(_hoveredDecorativeControl, control))
        {
            _hoveredDecorativeControl = null;
            _hovered = false;
            interactionStateChanged = true;
        }
        if (ReferenceEquals(_forwardingPressedControl, control))
        {
            _forwardingPressedControl = null;
            _pressed = false;
            interactionStateChanged = true;
        }

        if (interactionStateChanged && !Disposing)
        {
            UpdateResolvedForeground();
            Invalidate();
        }
    }

    private static bool IsDecorativeForwardingSurface(Control control)
    {
        var type = control.GetType();
        return type == typeof(Label) ||
            type == typeof(BootstrapBadge) ||
            type == typeof(Panel) ||
            type == typeof(FlowLayoutPanel) ||
            type == typeof(TableLayoutPanel);
    }

    private void OnDescendantControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is Control control) TrackDescendant(control);
        RequestOwningLayout();
    }

    private void OnDescendantControlRemoved(object? sender, ControlEventArgs e)
    {
        if (e.Control is Control control) UntrackDescendant(control);
        RequestOwningLayout();
    }

    private void OnDescendantPreferredSizeChanged(object? sender, EventArgs e) => RequestOwningLayout();

    private void OnItemLayout(object? sender, LayoutEventArgs e)
    {
        if (IsAnchorOrDockLayout(e))
        {
            RequestOwningLayout();
        }
    }

    private void OnItemAutoSizeChanged(object? sender, EventArgs e)
    {
        if (_trackExplicitSize && !AutoSize && !_applyingGroupLayoutBounds)
        {
            _explicitMinimumSize = Size;
        }

        _lastObservedSize = Size;
        RequestOwningLayout();
    }

    private void OnItemSizeChanged(object? sender, EventArgs e)
    {
        var currentSize = Size;
        if (_trackExplicitSize && !AutoSize && !_applyingGroupLayoutBounds)
        {
            _explicitMinimumSize = new Size(
                currentSize.Width != _lastObservedSize.Width ? currentSize.Width : _explicitMinimumSize.Width,
                currentSize.Height != _lastObservedSize.Height ? currentSize.Height : _explicitMinimumSize.Height);
        }

        _lastObservedSize = currentSize;
    }

    private void OnDescendantLayout(object? sender, LayoutEventArgs e)
    {
        if (IsAnchorOrDockLayout(e))
        {
            RequestOwningLayout();
        }
    }

    private static bool IsAnchorOrDockLayout(LayoutEventArgs e)
    {
        return e.AffectedControl is not null &&
            (e.AffectedProperty == nameof(Control.Anchor) || e.AffectedProperty == nameof(Control.Dock));
    }

    private void RequestOwningLayout()
    {
        Parent?.PerformLayout(this, nameof(PreferredSize));
    }

    private void OnDecorativeMouseEnter(object? sender, EventArgs e)
    {
        if (sender is not Control control || !_actionable || !Enabled)
        {
            return;
        }

        _hoveredDecorativeControl = control;
        _hovered = true;
        UpdateResolvedForeground();
        Invalidate();
    }

    private void OnDecorativeMouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is Control control && e.Button == MouseButtons.Left && _actionable && Enabled && control.ClientRectangle.Contains(e.Location))
        {
            Focus();
            _forwardingPressedControl = control;
            _pressed = true;
            UpdateResolvedForeground();
            Invalidate();
        }
    }

    private void OnDecorativeMouseUp(object? sender, MouseEventArgs e)
    {
        var activate = sender is Control control && ReferenceEquals(control, _forwardingPressedControl) &&
            e.Button == MouseButtons.Left && control.ClientRectangle.Contains(e.Location);
        _forwardingPressedControl = null;
        _pressed = false;
        UpdateResolvedForeground();
        Invalidate();
        if (activate) ActivateItem();
    }

    private void OnDecorativeMouseLeave(object? sender, EventArgs e)
    {
        var stateChanged = false;
        if (ReferenceEquals(sender, _hoveredDecorativeControl))
        {
            _hoveredDecorativeControl = null;
            _hovered = false;
            stateChanged = true;
        }

        if (ReferenceEquals(sender, _forwardingPressedControl))
        {
            _forwardingPressedControl = null;
            _pressed = false;
            stateChanged = true;
        }

        if (stateChanged)
        {
            UpdateResolvedForeground();
            Invalidate();
        }
    }
}
