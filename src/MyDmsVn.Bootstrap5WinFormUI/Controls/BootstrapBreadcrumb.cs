using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Displays an ordered hierarchy whose ancestors are native links and whose final item is the current location.
/// </summary>
[DefaultEvent(nameof(ItemClicked))]
public class BootstrapBreadcrumb : Panel
{
    private readonly Dictionary<LinkLabel, BootstrapBreadcrumbItem> _activeLinks =
        new Dictionary<LinkLabel, BootstrapBreadcrumbItem>();
    private readonly Dictionary<BootstrapBreadcrumbItem, Control> _itemControls =
        new Dictionary<BootstrapBreadcrumbItem, Control>();
    private readonly Dictionary<BootstrapBreadcrumbItem, Label> _dividerControls =
        new Dictionary<BootstrapBreadcrumbItem, Label>();
    private readonly List<Control> _generatedControls = new List<Control>();
    private string _divider = "/";
    private string? _rightToLeftDivider;
    private bool _wrapContents = true;
    private bool _performingBreadcrumbLayout;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

    /// <summary>Initializes a designer-safe empty breadcrumb using the current application theme.</summary>
    public BootstrapBreadcrumb()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleName = "Breadcrumb";
        AccessibleDescription = "Breadcrumb navigation.";

        Items = new BootstrapBreadcrumbItemCollection(
            RebuildGeneratedChildren,
            UpdateGeneratedItemText);
        ApplyThemeFont();
    }

    /// <summary>Gets the caller-owned logical items in root-to-current order.</summary>
    [Category("Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapBreadcrumbItemCollection Items { get; }

    /// <summary>Gets or sets the text displayed between adjacent items. A null value becomes an empty divider.</summary>
    [Category("Appearance")]
    [DefaultValue("/")]
    public string Divider
    {
        get => _divider;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_divider, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _divider = normalized;
            UpdateDividerText();
        }
    }

    /// <summary>Gets or sets optional divider text for right-to-left layout, or null to reuse <see cref="Divider"/>.</summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public string? RightToLeftDivider
    {
        get => _rightToLeftDivider;
        set
        {
            if (string.Equals(_rightToLeftDivider, value, StringComparison.Ordinal))
            {
                return;
            }

            _rightToLeftDivider = value;
            UpdateDividerText();
        }
    }

    /// <summary>Gets or sets whether whole breadcrumb segments wrap when a width constraint is available.</summary>
    [Category("Layout")]
    [DefaultValue(true)]
    public bool WrapContents
    {
        get => _wrapContents;
        set
        {
            if (_wrapContents == value)
            {
                return;
            }

            _wrapContents = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>Occurs when a currently generated ancestor link is activated.</summary>
    public event EventHandler<BootstrapBreadcrumbItemClickedEventArgs>? ItemClicked;

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var contentWidth = 0;
        if (_wrapContents)
        {
            if (proposedSize.Width > 0)
            {
                contentWidth = Math.Max(0, proposedSize.Width - Padding.Horizontal);
            }
            else if (MaximumSize.Width > 0)
            {
                contentWidth = Math.Max(0, MaximumSize.Width - Padding.Horizontal);
            }
        }

        var measured = BootstrapBreadcrumbLayoutLogic.Measure(
            CreateSegmentSizes(),
            contentWidth,
            ResolveLayoutMetrics(),
            _wrapContents);
        var width = AddNonNegative(measured.Width, Padding.Horizontal);
        var height = AddNonNegative(measured.Height, Padding.Vertical);
        if (MaximumSize.Width > 0)
        {
            width = Math.Min(width, MaximumSize.Width);
        }

        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (_performingBreadcrumbLayout || IsDisposed)
        {
            return;
        }

        _performingBreadcrumbLayout = true;
        try
        {
            var contentBounds = new Rectangle(
                Padding.Left,
                Padding.Top,
                Math.Max(0, ClientSize.Width - Padding.Horizontal),
                Math.Max(0, ClientSize.Height - Padding.Vertical));
            var layouts = BootstrapBreadcrumbLayoutLogic.Arrange(
                CreateSegmentSizes(),
                contentBounds,
                ResolveLayoutMetrics(),
                _wrapContents,
                RightToLeft == RightToLeft.Yes);

            for (var index = 0; index < Items.Count; index++)
            {
                var item = Items[index];
                if (_itemControls.TryGetValue(item, out var itemControl))
                {
                    itemControl.Bounds = layouts[index].ItemBounds;
                }

                if (_dividerControls.TryGetValue(item, out var dividerControl))
                {
                    dividerControl.Bounds = layouts[index].DividerBounds;
                }
            }
        }
        finally
        {
            _performingBreadcrumbLayout = false;
        }
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        PerformLayout();
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        ApplyChildColors();
    }

    /// <inheritdoc />
    protected override void OnRightToLeftChanged(EventArgs e)
    {
        base.OnRightToLeftChanged(e);
        UpdateDividerText();
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
            DisposeGeneratedChildren();
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void RebuildGeneratedChildren()
    {
        if (IsDisposed)
        {
            return;
        }

        SuspendLayout();
        try
        {
            DisposeGeneratedChildren();
            for (var index = 0; index < Items.Count; index++)
            {
                var item = Items[index];
                if (index > 0)
                {
                    var dividerControl = CreateDividerLabel();
                    _dividerControls.Add(item, dividerControl);
                    AddGeneratedControl(dividerControl);
                }

                Control itemControl;
                if (index < Items.Count - 1)
                {
                    var link = CreateAncestorLink(item);
                    _activeLinks.Add(link, item);
                    itemControl = link;
                }
                else
                {
                    itemControl = CreateCurrentLabel(item);
                }

                _itemControls.Add(item, itemControl);
                AddGeneratedControl(itemControl);
            }

            ApplyChildColors();
        }
        finally
        {
            ResumeLayout(true);
        }

        PerformLayout();
        Invalidate();
    }

    private void DisposeGeneratedChildren()
    {
        _activeLinks.Clear();
        _itemControls.Clear();
        _dividerControls.Clear();

        foreach (var control in _generatedControls)
        {
            Controls.Remove(control);
            control.Dispose();
        }

        _generatedControls.Clear();
    }

    private void AddGeneratedControl(Control control)
    {
        _generatedControls.Add(control);
        Controls.Add(control);
    }

    private static LinkLabel CreateAncestorLink(BootstrapBreadcrumbItem item)
    {
        return new LinkLabel
        {
            AutoSize = true,
            TabStop = true,
            UseMnemonic = false,
            LinkBehavior = LinkBehavior.AlwaysUnderline,
            LinkVisited = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Text = item.Text,
            LinkArea = new LinkArea(0, item.Text.Length),
            AccessibleName = item.Text
        };
    }

    private static Label CreateCurrentLabel(BootstrapBreadcrumbItem item)
    {
        return new Label
        {
            AutoSize = true,
            TabStop = false,
            UseMnemonic = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Text = item.Text,
            AccessibleName = item.Text,
            AccessibleDescription = "Current page."
        };
    }

    private Label CreateDividerLabel()
    {
        return new Label
        {
            AutoSize = true,
            TabStop = false,
            UseMnemonic = false,
            BackColor = Color.Transparent,
            AccessibleRole = AccessibleRole.None,
            AccessibleName = string.Empty,
            AccessibleDescription = string.Empty,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Text = GetEffectiveDivider()
        };
    }

    private void UpdateGeneratedItemText(BootstrapBreadcrumbItem item)
    {
        if (!_itemControls.TryGetValue(item, out var control))
        {
            return;
        }

        control.Text = item.Text;
        control.AccessibleName = item.Text;
        if (control is LinkLabel link)
        {
            link.LinkArea = new LinkArea(0, item.Text.Length);
        }

        PerformLayout();
        Invalidate();
    }

    private void UpdateDividerText()
    {
        var effectiveDivider = GetEffectiveDivider();
        foreach (var dividerControl in _dividerControls.Values)
        {
            dividerControl.Text = effectiveDivider;
        }

        PerformLayout();
        Invalidate();
    }

    private string GetEffectiveDivider()
    {
        return RightToLeft == RightToLeft.Yes
            ? _rightToLeftDivider ?? _divider
            : _divider;
    }

    private BootstrapBreadcrumbSegmentSize[] CreateSegmentSizes()
    {
        var segments = new BootstrapBreadcrumbSegmentSize[Items.Count];
        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items[index];
            var itemSize = _itemControls.TryGetValue(item, out var itemControl)
                ? itemControl.GetPreferredSize(Size.Empty)
                : Size.Empty;
            var hasDivider = _dividerControls.TryGetValue(item, out var dividerControl);
            var dividerSize = hasDivider
                ? dividerControl!.GetPreferredSize(Size.Empty)
                : Size.Empty;
            segments[index] = new BootstrapBreadcrumbSegmentSize(itemSize, dividerSize, hasDivider);
        }

        return segments;
    }

    private BootstrapBreadcrumbLayoutMetrics ResolveLayoutMetrics()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var themeMetrics = BootstrapThemeManager.CurrentTheme.Metrics;
        return new BootstrapBreadcrumbLayoutMetrics(
            DpiScaler.Scale(themeMetrics.SpacingSM, dpi),
            DpiScaler.Scale(themeMetrics.SpacingXS, dpi));
    }

    private void ApplyChildColors()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        foreach (var link in _activeLinks.Keys)
        {
            link.LinkColor = colors.Primary;
            link.VisitedLinkColor = colors.Primary;
            link.ActiveLinkColor = colors.Primary;
            link.DisabledLinkColor = colors.Disabled;
        }

        var passiveColor = Enabled ? colors.MutedText : colors.Disabled;
        foreach (var itemControl in _itemControls.Values)
        {
            if (itemControl is Label label && itemControl is not LinkLabel)
            {
                label.ForeColor = passiveColor;
            }
        }

        foreach (var dividerControl in _dividerControls.Values)
        {
            dividerControl.ForeColor = passiveColor;
        }
    }

    private void RaiseItemClicked(BootstrapBreadcrumbItem item, int index)
    {
        ItemClicked?.Invoke(this, new BootstrapBreadcrumbItemClickedEventArgs(item, index));
    }

    private void ApplyThemeFont()
    {
        if (!_useThemeFont)
        {
            return;
        }

        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var nextFont = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = nextFont;
        _settingThemeFont = true;
        try
        {
            Font = nextFont;
        }
        finally
        {
            _settingThemeFont = false;
        }

        if (previous is not null && ReferenceEquals(Font, previous))
        {
            _themeFont = previous;
            nextFont.Dispose();
            return;
        }

        previous?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private static int AddNonNegative(int left, int right)
    {
        var result = (long)Math.Max(0, left) + Math.Max(0, right);
        return result > int.MaxValue ? int.MaxValue : (int)result;
    }
}
