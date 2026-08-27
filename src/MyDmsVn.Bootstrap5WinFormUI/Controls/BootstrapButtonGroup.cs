using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Arranges <see cref="BootstrapButton"/> controls as one connected horizontal or vertical group
/// and owns the group's optional selection policy.
/// </summary>
[DefaultEvent(nameof(SelectionChanged))]
public class BootstrapButtonGroup : Panel
{
    private Orientation _orientation = Orientation.Horizontal;
    private BootstrapButtonSelectionMode _selectionMode = BootstrapButtonSelectionMode.None;
    private bool _equalWidth;
    private int _borderRadius = -1;
    private bool _themeSubscribed;
    private bool _performingLayout;

    /// <summary>
    /// Initializes a designer-safe button group using the current application theme metrics.
    /// </summary>
    public BootstrapButtonGroup()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Connected Bootstrap button group.";

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
    }

    /// <summary>
    /// Occurs when the group changes one or more button selected states through its selection policy.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Gets or sets whether buttons are connected horizontally or vertically.
    /// </summary>
    [Category("Layout")]
    [Description("Arranges buttons horizontally or vertically.")]
    [DefaultValue(Orientation.Horizontal)]
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            ValidateOrientation(value);
            if (_orientation == value)
            {
                return;
            }

            _orientation = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets how button activation changes selected state within this group.
    /// </summary>
    [Category("Behavior")]
    [Description("Selects None, Single, or Multiple button-selection behavior.")]
    [DefaultValue(BootstrapButtonSelectionMode.None)]
    public BootstrapButtonSelectionMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            ValidateSelectionMode(value);
            if (_selectionMode == value)
            {
                return;
            }

            _selectionMode = value;
            if (_selectionMode == BootstrapButtonSelectionMode.Single)
            {
                NormalizeSingleSelection();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether every visible button in a horizontal group uses the widest preferred button width.
    /// Vertical groups always use the widest preferred button width so both connected edges stay aligned.
    /// </summary>
    [Category("Layout")]
    [Description("Makes horizontal buttons equal width. Vertical groups are always stretched to the widest preferred button width.")]
    [DefaultValue(false)]
    public bool EqualWidth
    {
        get => _equalWidth;
        set
        {
            if (_equalWidth == value)
            {
                return;
            }

            _equalWidth = value;
            PerformLayout();
        }
    }

    /// <summary>
    /// Gets or sets the logical radius used on the group's outer corners. Use -1 to derive
    /// outer radii from each button's configured or themed radius.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets the logical outer corner radius, or -1 to use each button's normal radius.")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or a non-negative value.");
            }

            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets a snapshot of the currently selected buttons in this group.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<BootstrapButton> SelectedButtons
    {
        get
        {
            var selected = new List<BootstrapButton>();
            foreach (Control control in Controls)
            {
                if (control is BootstrapButton button && button.Selected)
                {
                    selected.Add(button);
                }
            }

            return selected;
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var buttons = GetVisibleButtons();
        if (buttons.Count == 0)
        {
            return new Size(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
        }

        var sizes = GetPreferredButtonSizes(buttons);
        var overlap = GetSeamOverlap();
        var width = 0;
        var height = 0;
        var widest = GetWidest(sizes);

        if (_orientation == Orientation.Horizontal)
        {
            for (var i = 0; i < sizes.Count; i++)
            {
                width += _equalWidth ? widest : sizes[i].Width;
                height = Math.Max(height, sizes[i].Height);
            }

            width -= overlap * Math.Max(0, sizes.Count - 1);
        }
        else
        {
            for (var i = 0; i < sizes.Count; i++)
            {
                width = Math.Max(width, sizes[i].Width);
                height += sizes[i].Height;
            }

            height -= overlap * Math.Max(0, sizes.Count - 1);
        }

        return new Size(
            Math.Max(0, width) + Padding.Left + Padding.Right,
            Math.Max(0, height) + Padding.Top + Padding.Bottom);
    }

    /// <inheritdoc />
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is BootstrapButton button)
        {
            button.Click += OnButtonClick;
            button.SizeChanged += OnButtonLayoutRelevantChanged;
            button.VisibleChanged += OnButtonLayoutRelevantChanged;
            button.TextChanged += OnButtonLayoutRelevantChanged;
        }

        PerformLayout();
    }

    /// <inheritdoc />
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is BootstrapButton button)
        {
            button.Click -= OnButtonClick;
            button.SizeChanged -= OnButtonLayoutRelevantChanged;
            button.VisibleChanged -= OnButtonLayoutRelevantChanged;
            button.TextChanged -= OnButtonLayoutRelevantChanged;
            button.GroupCornerRadius = null;
        }

        base.OnControlRemoved(e);
        PerformLayout();
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (_performingLayout || IsDisposed)
        {
            return;
        }

        _performingLayout = true;
        try
        {
            LayoutButtons();
        }
        finally
        {
            _performingLayout = false;
        }
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        PerformLayout();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            foreach (Control control in Controls)
            {
                if (control is BootstrapButton button)
                {
                    button.Click -= OnButtonClick;
                    button.SizeChanged -= OnButtonLayoutRelevantChanged;
                    button.VisibleChanged -= OnButtonLayoutRelevantChanged;
                    button.TextChanged -= OnButtonLayoutRelevantChanged;
                    button.GroupCornerRadius = null;
                }
            }
        }

        base.Dispose(disposing);
    }

    private void LayoutButtons()
    {
        var buttons = GetVisibleButtons();
        ApplyCornerRadii(buttons);
        if (buttons.Count == 0)
        {
            return;
        }

        var sizes = GetPreferredButtonSizes(buttons);
        var overlap = GetSeamOverlap();
        var widest = GetWidest(sizes);
        var contentLeft = Padding.Left;
        var contentTop = Padding.Top;

        if (_orientation == Orientation.Horizontal)
        {
            var height = 0;
            for (var i = 0; i < sizes.Count; i++)
            {
                height = Math.Max(height, sizes[i].Height);
            }

            var x = contentLeft;
            for (var i = 0; i < buttons.Count; i++)
            {
                var width = _equalWidth ? widest : sizes[i].Width;
                buttons[i].SetBounds(x, contentTop, width, height);
                x += width - overlap;
            }
        }
        else
        {
            var y = contentTop;
            for (var i = 0; i < buttons.Count; i++)
            {
                buttons[i].SetBounds(contentLeft, y, widest, sizes[i].Height);
                y += sizes[i].Height - overlap;
            }
        }
    }

    private void ApplyCornerRadii(IReadOnlyList<BootstrapButton> buttons)
    {
        if (buttons.Count == 0)
        {
            return;
        }

        if (buttons.Count == 1)
        {
            var radius = ResolveLogicalRadius(buttons[0]);
            buttons[0].GroupCornerRadius = new CornerRadius(radius);
            return;
        }

        for (var i = 0; i < buttons.Count; i++)
        {
            var radius = ResolveLogicalRadius(buttons[i]);
            if (_orientation == Orientation.Horizontal)
            {
                buttons[i].GroupCornerRadius = i == 0
                    ? new CornerRadius(radius, 0f, 0f, radius)
                    : (i == buttons.Count - 1
                        ? new CornerRadius(0f, radius, radius, 0f)
                        : CornerRadius.Empty);
            }
            else
            {
                buttons[i].GroupCornerRadius = i == 0
                    ? new CornerRadius(radius, radius, 0f, 0f)
                    : (i == buttons.Count - 1
                        ? new CornerRadius(0f, 0f, radius, radius)
                        : CornerRadius.Empty);
            }
        }
    }

    private float ResolveLogicalRadius(BootstrapButton button)
    {
        if (_borderRadius >= 0)
        {
            return _borderRadius;
        }

        if (button.BorderRadius >= 0)
        {
            return button.BorderRadius;
        }

        return BootstrapButtonRenderLogic.GetThemeBorderRadius(
            BootstrapThemeManager.CurrentTheme.Metrics,
            button.ButtonSize);
    }

    private void OnButtonClick(object? sender, EventArgs e)
    {
        if (sender is not BootstrapButton button)
        {
            return;
        }

        var changed = false;
        switch (_selectionMode)
        {
            case BootstrapButtonSelectionMode.None:
                return;

            case BootstrapButtonSelectionMode.Single:
                foreach (Control control in Controls)
                {
                    if (control is BootstrapButton candidate)
                    {
                        var shouldSelect = ReferenceEquals(candidate, button);
                        if (candidate.Selected != shouldSelect)
                        {
                            candidate.Selected = shouldSelect;
                            changed = true;
                        }
                    }
                }
                break;

            case BootstrapButtonSelectionMode.Multiple:
                button.Selected = !button.Selected;
                changed = true;
                break;
        }

        if (changed)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void NormalizeSingleSelection()
    {
        BootstrapButton? retained = null;
        var changed = false;
        foreach (Control control in Controls)
        {
            if (control is not BootstrapButton button || !button.Selected)
            {
                continue;
            }

            if (retained is null)
            {
                retained = button;
                continue;
            }

            button.Selected = false;
            changed = true;
        }

        if (changed)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnButtonLayoutRelevantChanged(object? sender, EventArgs e)
    {
        if (!_performingLayout)
        {
            PerformLayout();
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (!IsDisposed)
        {
            PerformLayout();
            Invalidate();
        }
    }

    private List<BootstrapButton> GetVisibleButtons()
    {
        var buttons = new List<BootstrapButton>();
        foreach (Control control in Controls)
        {
            if (control is BootstrapButton button && button.Visible)
            {
                buttons.Add(button);
            }
        }

        return buttons;
    }

    private static List<Size> GetPreferredButtonSizes(IReadOnlyList<BootstrapButton> buttons)
    {
        var sizes = new List<Size>(buttons.Count);
        for (var i = 0; i < buttons.Count; i++)
        {
            sizes.Add(buttons[i].GetPreferredSize(Size.Empty));
        }

        return sizes;
    }

    private static int GetWidest(IReadOnlyList<Size> sizes)
    {
        var widest = 0;
        for (var i = 0; i < sizes.Count; i++)
        {
            widest = Math.Max(widest, sizes[i].Width);
        }

        return widest;
    }

    private int GetSeamOverlap()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return Math.Max(1, DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.BorderWidth, dpi));
    }

    private static void ValidateOrientation(Orientation value)
    {
        if (value != Orientation.Horizontal && value != Orientation.Vertical)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported button-group orientation.");
        }
    }

    private static void ValidateSelectionMode(BootstrapButtonSelectionMode value)
    {
        if (value != BootstrapButtonSelectionMode.None &&
            value != BootstrapButtonSelectionMode.Single &&
            value != BootstrapButtonSelectionMode.Multiple)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported button-group selection mode.");
        }
    }
}
