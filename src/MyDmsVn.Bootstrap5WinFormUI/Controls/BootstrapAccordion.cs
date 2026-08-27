using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Arranges <see cref="BootstrapAccordionItem"/> controls vertically and owns only
/// the single-open or multiple-open coordination policy.
/// </summary>
[DefaultProperty(nameof(AllowMultipleOpen))]
public class BootstrapAccordion : Panel
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(200);

    private readonly List<BootstrapAccordionItem> _items = new List<BootstrapAccordionItem>();
    private bool _allowMultipleOpen;
    private bool _flush;
    private TimeSpan _animationDuration = DefaultAnimationDuration;
    private bool _performingLayout;
    private bool _normalizingExpansion;
    private bool _themeSubscribed;

    /// <summary>
    /// Initializes a designer-safe single-open accordion.
    /// </summary>
    public BootstrapAccordion()
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
        AccessibleDescription = "Accordion section collection.";
        Size = new Size(360, 48);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
    }

    /// <summary>
    /// Gets a snapshot of the accordion items in display order.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<BootstrapAccordionItem> Items => _items.ToArray();

    /// <summary>
    /// Gets or sets whether more than one item may remain expanded at a time.
    /// </summary>
    [Category("Behavior")]
    [Description("Allows more than one accordion item to remain expanded.")]
    [DefaultValue(false)]
    public bool AllowMultipleOpen
    {
        get => _allowMultipleOpen;
        set
        {
            if (_allowMultipleOpen == value)
            {
                return;
            }

            _allowMultipleOpen = value;
            if (!_allowMultipleOpen)
            {
                NormalizeSingleOpenState();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether items use the borderless-edge flush presentation.
    /// </summary>
    [Category("Appearance")]
    [Description("Uses square flush items with separators instead of rounded outer item borders.")]
    [DefaultValue(false)]
    public bool Flush
    {
        get => _flush;
        set
        {
            if (_flush == value)
            {
                return;
            }

            _flush = value;
            foreach (var item in _items)
            {
                item.Flush = value;
            }

            PerformLayout();
            Invalidate(true);
        }
    }

    /// <summary>
    /// Gets or sets the full expand/collapse transition duration applied to all items.
    /// </summary>
    [Category("Behavior")]
    [Description("Sets the full expand/collapse animation duration for current and future items.")]
    [DefaultValue(typeof(TimeSpan), "00:00:00.2000000")]
    public TimeSpan AnimationDuration
    {
        get => _animationDuration;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Animation duration must be greater than zero.");
            }

            if (_animationDuration == value)
            {
                return;
            }

            _animationDuration = value;
            foreach (var item in _items)
            {
                item.AnimationDuration = value;
            }
        }
    }

    /// <summary>
    /// Creates, adds, and returns a collapsed item with the supplied header text.
    /// </summary>
    public BootstrapAccordionItem AddItem(string text)
    {
        var item = new BootstrapAccordionItem();
        item.Header.Text = text ?? string.Empty;
        Controls.Add(item);
        return item;
    }

    /// <summary>
    /// Adds an existing item to the accordion using the normal WinForms child-control ownership model.
    /// </summary>
    public void AddItem(BootstrapAccordionItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (ReferenceEquals(item.Parent, this))
        {
            return;
        }

        Controls.Add(item);
    }

    /// <summary>
    /// Removes an item without disposing it so the caller may reuse or dispose it explicitly.
    /// </summary>
    public bool RemoveItem(BootstrapAccordionItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (!ReferenceEquals(item.Parent, this))
        {
            return false;
        }

        Controls.Remove(item);
        return true;
    }

    /// <summary>
    /// Removes all accordion items without disposing them.
    /// </summary>
    public void ClearItems()
    {
        var snapshot = _items.ToArray();
        foreach (var item in snapshot)
        {
            Controls.Remove(item);
        }
    }

    /// <summary>
    /// Collapses every item.
    /// </summary>
    public void CollapseAll()
    {
        _normalizingExpansion = true;
        try
        {
            foreach (var item in _items)
            {
                item.Expanded = false;
            }
        }
        finally
        {
            _normalizingExpansion = false;
        }
    }

    /// <summary>
    /// Expands every item when multiple-open mode is enabled. In single-open mode,
    /// only the first item is expanded and all remaining items are collapsed.
    /// </summary>
    public void ExpandAll()
    {
        _normalizingExpansion = true;
        try
        {
            for (var index = 0; index < _items.Count; index++)
            {
                _items[index].Expanded = _allowMultipleOpen || index == 0;
            }
        }
        finally
        {
            _normalizingExpansion = false;
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var visible = GetVisibleItems();
        if (visible.Count == 0)
        {
            return new Size(Padding.Horizontal, Padding.Vertical);
        }

        var availableWidth = proposedSize.Width > 0
            ? Math.Max(0, proposedSize.Width - Padding.Horizontal)
            : 0;
        var width = 0;
        var height = 0;
        var overlap = GetItemOverlap();

        for (var index = 0; index < visible.Count; index++)
        {
            var preferred = visible[index].GetPreferredSize(new Size(availableWidth, 0));
            width = Math.Max(width, preferred.Width);
            height += preferred.Height;
            if (index > 0)
            {
                height -= overlap;
            }
        }

        return new Size(
            Math.Max(0, width) + Padding.Horizontal,
            Math.Max(0, height) + Padding.Vertical);
    }

    /// <inheritdoc />
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is BootstrapAccordionItem item && !_items.Contains(item))
        {
            _items.Add(item);
            item.Flush = _flush;
            item.AnimationDuration = _animationDuration;
            item.ExpandedChanged += OnItemExpandedChanged;
            item.SizeChanged += OnItemLayoutRelevantChanged;
            item.VisibleChanged += OnItemLayoutRelevantChanged;

            if (!_allowMultipleOpen && item.Expanded)
            {
                NormalizeSingleOpenState(item);
            }
        }

        PerformLayout();
    }

    /// <inheritdoc />
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is BootstrapAccordionItem item && _items.Remove(item))
        {
            item.ExpandedChanged -= OnItemExpandedChanged;
            item.SizeChanged -= OnItemLayoutRelevantChanged;
            item.VisibleChanged -= OnItemLayoutRelevantChanged;
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
            LayoutItems();
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
        Invalidate(true);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var item in _items.ToArray())
            {
                item.ExpandedChanged -= OnItemExpandedChanged;
                item.SizeChanged -= OnItemLayoutRelevantChanged;
                item.VisibleChanged -= OnItemLayoutRelevantChanged;
            }

            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }
        }

        base.Dispose(disposing);
    }

    private void OnItemExpandedChanged(object? sender, EventArgs e)
    {
        if (sender is not BootstrapAccordionItem item)
        {
            return;
        }

        if (!_normalizingExpansion && !_allowMultipleOpen && item.Expanded)
        {
            NormalizeSingleOpenState(item);
        }

        PerformLayout();
    }

    private void NormalizeSingleOpenState(BootstrapAccordionItem? preferredExpanded = null)
    {
        if (_allowMultipleOpen || _normalizingExpansion)
        {
            return;
        }

        _normalizingExpansion = true;
        try
        {
            BootstrapAccordionItem? retained = preferredExpanded;
            if (retained is null || !retained.Expanded)
            {
                foreach (var item in _items)
                {
                    if (item.Expanded)
                    {
                        retained = item;
                        break;
                    }
                }
            }

            foreach (var item in _items)
            {
                if (!ReferenceEquals(item, retained) && item.Expanded)
                {
                    item.Expanded = false;
                }
            }
        }
        finally
        {
            _normalizingExpansion = false;
        }
    }

    private void LayoutItems()
    {
        var visible = GetVisibleItems();
        var x = Padding.Left;
        var y = Padding.Top;
        var width = Math.Max(0, ClientSize.Width - Padding.Horizontal);
        var overlap = GetItemOverlap();

        for (var index = 0; index < visible.Count; index++)
        {
            var item = visible[index];
            if (item.Width != width)
            {
                item.Width = width;
            }

            item.PerformLayout();
            item.Location = new Point(x, y);
            y += Math.Max(0, item.Height - (index < visible.Count - 1 ? overlap : 0));
        }

        if (AutoSize)
        {
            var desiredHeight = Math.Max(0, y + Padding.Bottom);
            if (Height != desiredHeight)
            {
                Height = desiredHeight;
            }
        }
    }

    private List<BootstrapAccordionItem> GetVisibleItems()
    {
        var result = new List<BootstrapAccordionItem>();
        foreach (var item in _items)
        {
            if (item.Visible)
            {
                result.Add(item);
            }
        }

        return result;
    }

    private int GetItemOverlap()
    {
        if (_flush)
        {
            return 0;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return Math.Max(0, DpiScaler.Scale(theme.Metrics.BorderWidth, dpi));
    }

    private void OnItemLayoutRelevantChanged(object? sender, EventArgs e)
    {
        if (!_performingLayout)
        {
            PerformLayout();
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        PerformLayout();
        Invalidate(true);
    }
}
