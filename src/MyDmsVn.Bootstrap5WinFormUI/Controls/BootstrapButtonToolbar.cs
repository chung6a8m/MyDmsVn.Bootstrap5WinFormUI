using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Arranges multiple <see cref="BootstrapButtonGroup"/> controls as a toolbar without owning
/// or changing button selection state.
/// </summary>
public class BootstrapButtonToolbar : Panel
{
    private Orientation _orientation = Orientation.Horizontal;
    private int _groupSpacing = 8;
    private BootstrapToolbarAlignment _alignment = BootstrapToolbarAlignment.Left;
    private bool _performingLayout;

    /// <summary>
    /// Initializes a designer-safe toolbar with Bootstrap-inspired group spacing.
    /// </summary>
    public BootstrapButtonToolbar()
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
        AccessibleRole = AccessibleRole.ToolBar;
        AccessibleDescription = "Toolbar containing Bootstrap button groups.";
    }

    /// <summary>
    /// Gets or sets whether button groups flow horizontally or vertically.
    /// </summary>
    [Category("Layout")]
    [Description("Arranges button groups horizontally or vertically.")]
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
        }
    }

    /// <summary>
    /// Gets or sets the logical spacing between adjacent button groups.
    /// </summary>
    [Category("Layout")]
    [Description("Sets the logical spacing between adjacent button groups.")]
    [DefaultValue(8)]
    public int GroupSpacing
    {
        get => _groupSpacing;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Group spacing cannot be negative.");
            }

            if (_groupSpacing == value)
            {
                return;
            }

            _groupSpacing = value;
            PerformLayout();
        }
    }

    /// <summary>
    /// Gets or sets how the collection of groups is positioned on the toolbar's main axis.
    /// In vertical orientation Left and Right represent leading and trailing placement.
    /// </summary>
    [Category("Layout")]
    [Description("Aligns groups at the leading edge, center, trailing edge, or with space between them.")]
    [DefaultValue(BootstrapToolbarAlignment.Left)]
    public BootstrapToolbarAlignment Alignment
    {
        get => _alignment;
        set
        {
            ValidateAlignment(value);
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            PerformLayout();
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var groups = GetVisibleGroups();
        if (groups.Count == 0)
        {
            return new Size(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
        }

        var sizes = GetGroupSizes(groups);
        var spacing = GetScaledGroupSpacing();
        var width = 0;
        var height = 0;

        if (_orientation == Orientation.Horizontal)
        {
            for (var i = 0; i < sizes.Count; i++)
            {
                width += sizes[i].Width;
                height = Math.Max(height, sizes[i].Height);
            }

            width += spacing * Math.Max(0, sizes.Count - 1);
        }
        else
        {
            for (var i = 0; i < sizes.Count; i++)
            {
                width = Math.Max(width, sizes[i].Width);
                height += sizes[i].Height;
            }

            height += spacing * Math.Max(0, sizes.Count - 1);
        }

        return new Size(
            width + Padding.Left + Padding.Right,
            height + Padding.Top + Padding.Bottom);
    }

    /// <inheritdoc />
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is BootstrapButtonGroup group)
        {
            group.SizeChanged += OnGroupLayoutRelevantChanged;
            group.VisibleChanged += OnGroupLayoutRelevantChanged;
        }

        PerformLayout();
    }

    /// <inheritdoc />
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is BootstrapButtonGroup group)
        {
            group.SizeChanged -= OnGroupLayoutRelevantChanged;
            group.VisibleChanged -= OnGroupLayoutRelevantChanged;
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
            LayoutGroups();
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
            foreach (Control control in Controls)
            {
                if (control is BootstrapButtonGroup group)
                {
                    group.SizeChanged -= OnGroupLayoutRelevantChanged;
                    group.VisibleChanged -= OnGroupLayoutRelevantChanged;
                }
            }
        }

        base.Dispose(disposing);
    }

    private void LayoutGroups()
    {
        var groups = GetVisibleGroups();
        if (groups.Count == 0)
        {
            return;
        }

        var sizes = GetGroupSizes(groups);
        var spacing = GetScaledGroupSpacing();
        var content = new Rectangle(
            Padding.Left,
            Padding.Top,
            Math.Max(0, ClientSize.Width - Padding.Left - Padding.Right),
            Math.Max(0, ClientSize.Height - Padding.Top - Padding.Bottom));

        if (_orientation == Orientation.Horizontal)
        {
            LayoutHorizontal(groups, sizes, spacing, content);
        }
        else
        {
            LayoutVertical(groups, sizes, spacing, content);
        }
    }

    private void LayoutHorizontal(
        IReadOnlyList<BootstrapButtonGroup> groups,
        IReadOnlyList<Size> sizes,
        int spacing,
        Rectangle content)
    {
        var sumWidths = 0;
        for (var i = 0; i < sizes.Count; i++)
        {
            sumWidths += sizes[i].Width;
        }

        var naturalWidth = sumWidths + (spacing * Math.Max(0, sizes.Count - 1));
        var x = GetRegularStart(content.Left, content.Right, content.Width, naturalWidth);
        var gap = spacing;
        var distribute = _alignment == BootstrapToolbarAlignment.SpaceBetween && sizes.Count > 1;
        if (distribute)
        {
            var availableForGaps = content.Width - sumWidths;
            if (availableForGaps >= spacing * (sizes.Count - 1))
            {
                x = content.Left;
                gap = availableForGaps / (sizes.Count - 1);
            }
            else
            {
                distribute = false;
                x = content.Left;
            }
        }

        for (var i = 0; i < groups.Count; i++)
        {
            if (distribute && i == groups.Count - 1)
            {
                x = content.Right - sizes[i].Width;
            }

            groups[i].SetBounds(x, content.Top, sizes[i].Width, sizes[i].Height);
            x += sizes[i].Width + gap;
        }
    }

    private void LayoutVertical(
        IReadOnlyList<BootstrapButtonGroup> groups,
        IReadOnlyList<Size> sizes,
        int spacing,
        Rectangle content)
    {
        var sumHeights = 0;
        for (var i = 0; i < sizes.Count; i++)
        {
            sumHeights += sizes[i].Height;
        }

        var naturalHeight = sumHeights + (spacing * Math.Max(0, sizes.Count - 1));
        var y = GetRegularStart(content.Top, content.Bottom, content.Height, naturalHeight);
        var gap = spacing;
        var distribute = _alignment == BootstrapToolbarAlignment.SpaceBetween && sizes.Count > 1;
        if (distribute)
        {
            var availableForGaps = content.Height - sumHeights;
            if (availableForGaps >= spacing * (sizes.Count - 1))
            {
                y = content.Top;
                gap = availableForGaps / (sizes.Count - 1);
            }
            else
            {
                distribute = false;
                y = content.Top;
            }
        }

        for (var i = 0; i < groups.Count; i++)
        {
            if (distribute && i == groups.Count - 1)
            {
                y = content.Bottom - sizes[i].Height;
            }

            groups[i].SetBounds(content.Left, y, sizes[i].Width, sizes[i].Height);
            y += sizes[i].Height + gap;
        }
    }

    private int GetRegularStart(int leading, int trailing, int available, int natural)
    {
        switch (_alignment)
        {
            case BootstrapToolbarAlignment.Center:
                return leading + Math.Max(0, (available - natural) / 2);

            case BootstrapToolbarAlignment.Right:
                return Math.Max(leading, trailing - natural);

            case BootstrapToolbarAlignment.Left:
            case BootstrapToolbarAlignment.SpaceBetween:
            default:
                return leading;
        }
    }

    private void OnGroupLayoutRelevantChanged(object? sender, EventArgs e)
    {
        if (!_performingLayout)
        {
            PerformLayout();
        }
    }

    private List<BootstrapButtonGroup> GetVisibleGroups()
    {
        var groups = new List<BootstrapButtonGroup>();
        foreach (Control control in Controls)
        {
            if (control is BootstrapButtonGroup group && group.Visible)
            {
                groups.Add(group);
            }
        }

        return groups;
    }

    private static List<Size> GetGroupSizes(IReadOnlyList<BootstrapButtonGroup> groups)
    {
        var sizes = new List<Size>(groups.Count);
        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            sizes.Add(group.AutoSize ? group.GetPreferredSize(Size.Empty) : group.Size);
        }

        return sizes;
    }

    private int GetScaledGroupSpacing()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return DpiScaler.Scale(_groupSpacing, dpi);
    }

    private static void ValidateOrientation(Orientation value)
    {
        if (value != Orientation.Horizontal && value != Orientation.Vertical)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported toolbar orientation.");
        }
    }

    private static void ValidateAlignment(BootstrapToolbarAlignment value)
    {
        if (value != BootstrapToolbarAlignment.Left &&
            value != BootstrapToolbarAlignment.Center &&
            value != BootstrapToolbarAlignment.Right &&
            value != BootstrapToolbarAlignment.SpaceBetween)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported toolbar alignment.");
        }
    }
}
