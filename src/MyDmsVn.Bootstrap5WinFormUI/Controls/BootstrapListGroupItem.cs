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
    private Control? _forwardingPressedControl;
    private readonly HashSet<Control> _trackedDescendants = new HashSet<Control>();
    private string? _automaticAccessibleName;

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
    }

    /// <summary>Gets or sets the application-owned active presentation state.</summary>
    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Active
    {
        get => _active;
        set { if (_active != value) { _active = value; Invalidate(); } }
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
            contentSize.Width = Math.Max(contentSize.Width, child.Right);
            contentSize.Height = Math.Max(contentSize.Height, child.Bottom);
        }
        return BootstrapListGroupRenderLogic.GetPreferredSize(textSize, contentSize, padding);
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
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

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
        if (_actionable && Enabled) { _hovered = true; Invalidate(); }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
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
        Invalidate();
        if (activate) ActivateItem();
    }

    /// <inheritdoc />
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (!Capture && _pressed) { _pressed = false; Invalidate(); }
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
        var state = BootstrapListGroupRenderLogic.ResolveState(Enabled, _active, _actionable, _pressed, _hovered);
        var palette = BootstrapListGroupRenderLogic.ResolvePalette(theme.Colors, _variant, state);
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
        _pressed = false;
        _spacePressed = false;
        _forwardingPressedControl = null;
        if (Capture) Capture = false;
    }

    private void TrackDescendant(Control control)
    {
        if (!_trackedDescendants.Add(control)) return;
        control.ControlAdded += OnDescendantControlAdded;
        control.ControlRemoved += OnDescendantControlRemoved;
        control.LocationChanged += OnDescendantPreferredSizeChanged;
        control.SizeChanged += OnDescendantPreferredSizeChanged;
        control.VisibleChanged += OnDescendantPreferredSizeChanged;
        if (IsDecorativeForwardingSurface(control))
        {
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
        control.LocationChanged -= OnDescendantPreferredSizeChanged;
        control.SizeChanged -= OnDescendantPreferredSizeChanged;
        control.VisibleChanged -= OnDescendantPreferredSizeChanged;
        control.MouseDown -= OnDecorativeMouseDown;
        control.MouseUp -= OnDecorativeMouseUp;
        control.MouseLeave -= OnDecorativeMouseLeave;
        if (ReferenceEquals(_forwardingPressedControl, control)) _forwardingPressedControl = null;
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

    private void RequestOwningLayout()
    {
        Parent?.PerformLayout(this, nameof(PreferredSize));
    }

    private void OnDecorativeMouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is Control control && e.Button == MouseButtons.Left && _actionable && Enabled && control.ClientRectangle.Contains(e.Location))
            _forwardingPressedControl = control;
    }

    private void OnDecorativeMouseUp(object? sender, MouseEventArgs e)
    {
        var activate = sender is Control control && ReferenceEquals(control, _forwardingPressedControl) &&
            e.Button == MouseButtons.Left && control.ClientRectangle.Contains(e.Location);
        _forwardingPressedControl = null;
        if (activate) ActivateItem();
    }

    private void OnDecorativeMouseLeave(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _forwardingPressedControl)) _forwardingPressedControl = null;
    }
}
