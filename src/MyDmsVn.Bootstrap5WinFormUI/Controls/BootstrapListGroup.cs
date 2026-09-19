using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Arranges a short collection of composed list-group items.</summary>
[DefaultEvent(nameof(ItemClick))]
public class BootstrapListGroup : Panel
{
    private readonly HashSet<BootstrapListGroupItem> _subscribedItems = new HashSet<BootstrapListGroupItem>();
    private Orientation _orientation = Orientation.Vertical;
    private bool _flush;
    private int _borderRadius = -1;
    private bool _performingLayout;
    private bool _themeSubscribed;

    /// <summary>Initializes a vertical, auto-sized list group.</summary>
    public BootstrapListGroup()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.List;
        AccessibleDescription = "Bootstrap-inspired list group.";
        Size = new Size(320, 48);
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
    }

    /// <summary>Gets or sets the direction in which items are arranged.</summary>
    [Category("Layout")]
    [DefaultValue(Orientation.Vertical)]
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (value != Orientation.Vertical && value != Orientation.Horizontal)
            {
                throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(Orientation));
            }

            if (_orientation == value)
            {
                return;
            }

            _orientation = value;
            PerformLayout();
            Invalidate(true);
        }
    }

    /// <summary>Gets or sets whether vertical items use edge-to-edge flush presentation.</summary>
    [Category("Appearance")]
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
            PerformLayout();
            Invalidate(true);
        }
    }

    /// <summary>Gets or sets the outer logical corner radius, or -1 for the theme radius.</summary>
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

            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            PerformLayout();
            Invalidate(true);
        }
    }

    /// <summary>Occurs when an actionable child item is activated.</summary>
    public event EventHandler<BootstrapListGroupItemEventArgs>? ItemClick;

    /// <summary>Gets a read-only snapshot of direct items in current WinForms child-index order.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<BootstrapListGroupItem> Items => GetItemsSnapshot();

    /// <summary>Creates, adds, and returns an item containing the supplied text.</summary>
    public BootstrapListGroupItem AddItem(string text)
    {
        var item = new BootstrapListGroupItem { Text = text ?? string.Empty };
        Controls.Add(item);
        return item;
    }

    /// <summary>Adds an existing item through normal WinForms child ownership.</summary>
    public void AddItem(BootstrapListGroupItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (!ReferenceEquals(item.Parent, this))
        {
            Controls.Add(item);
        }
    }

    /// <summary>Removes an item without disposing it.</summary>
    public bool RemoveItem(BootstrapListGroupItem item)
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

    /// <summary>Removes all direct list-group items without disposing them.</summary>
    public void ClearItems()
    {
        foreach (var item in GetItemsSnapshot())
        {
            Controls.Remove(item);
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

        var overlap = GetSeamOverlap();
        var width = 0;
        var height = 0;
        foreach (var item in visible)
        {
            var preferred = item.GetPreferredSize(Size.Empty);
            if (_orientation == Orientation.Vertical)
            {
                width = Math.Max(width, preferred.Width);
                height += Math.Max(0, preferred.Height);
            }
            else
            {
                width += Math.Max(0, preferred.Width);
                height = Math.Max(height, preferred.Height);
            }
        }

        if (visible.Count > 1)
        {
            if (_orientation == Orientation.Vertical)
            {
                height -= overlap * (visible.Count - 1);
            }
            else
            {
                width -= overlap * (visible.Count - 1);
            }
        }

        return new Size(Math.Max(0, width) + Padding.Horizontal, Math.Max(0, height) + Padding.Vertical);
    }

    /// <inheritdoc />
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is BootstrapListGroupItem item && _subscribedItems.Add(item))
        {
            item.Click += OnItemClick;
            item.SizeChanged += OnItemLayoutRelevantChanged;
            item.VisibleChanged += OnItemLayoutRelevantChanged;
            item.TextChanged += OnItemLayoutRelevantChanged;
            item.FontChanged += OnItemLayoutRelevantChanged;
            item.PaddingChanged += OnItemLayoutRelevantChanged;
        }


        PerformLayout();
    }

    /// <inheritdoc />
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is BootstrapListGroupItem item && _subscribedItems.Remove(item))
        {
            item.Click -= OnItemClick;
            item.SizeChanged -= OnItemLayoutRelevantChanged;
            item.VisibleChanged -= OnItemLayoutRelevantChanged;
            item.TextChanged -= OnItemLayoutRelevantChanged;
            item.FontChanged -= OnItemLayoutRelevantChanged;
            item.PaddingChanged -= OnItemLayoutRelevantChanged;
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
            foreach (var item in _subscribedItems)
            {
                item.Click -= OnItemClick;
                item.SizeChanged -= OnItemLayoutRelevantChanged;
                item.VisibleChanged -= OnItemLayoutRelevantChanged;
                item.TextChanged -= OnItemLayoutRelevantChanged;
                item.FontChanged -= OnItemLayoutRelevantChanged;
                item.PaddingChanged -= OnItemLayoutRelevantChanged;
            }

            _subscribedItems.Clear();
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }
        }

        base.Dispose(disposing);
    }

    internal void RaiseItemClick(BootstrapListGroupItem item)
    {
        ItemClick?.Invoke(this, new BootstrapListGroupItemEventArgs(item));
    }

    internal List<BootstrapListGroupItem> GetItemsSnapshot()
    {
        var result = new List<BootstrapListGroupItem>();
        foreach (Control control in Controls)
        {
            if (control is BootstrapListGroupItem item)
            {
                result.Add(item);
            }
        }

        return result;
    }

    internal bool NavigateFrom(BootstrapListGroupItem source, Keys keyData)
    {
        if (source is null || !source.Focused)
        {
            return false;
        }

        var key = keyData & Keys.KeyCode;
        var validDirectional = _orientation == Orientation.Vertical
            ? key == Keys.Up || key == Keys.Down
            : key == Keys.Left || key == Keys.Right;
        if (!validDirectional && key != Keys.Home && key != Keys.End)
        {
            return false;
        }

        var eligible = new List<BootstrapListGroupItem>();
        foreach (var item in GetItemsSnapshot())
        {
            if (item.Visible && item.Enabled && item.Actionable && item.CanSelect)
            {
                eligible.Add(item);
            }
        }

        var current = eligible.IndexOf(source);
        if (current < 0 || eligible.Count == 0)
        {
            return false;
        }

        int targetIndex;
        if (key == Keys.Home) targetIndex = 0;
        else if (key == Keys.End) targetIndex = eligible.Count - 1;
        else if (key == Keys.Down || key == Keys.Right) targetIndex = Math.Min(eligible.Count - 1, current + 1);
        else targetIndex = Math.Max(0, current - 1);
        if (targetIndex == current)
        {
            return true;
        }

        eligible[targetIndex].Focus();
        return true;
    }

    private List<BootstrapListGroupItem> GetVisibleItems()
    {
        var result = new List<BootstrapListGroupItem>();
        foreach (var item in GetItemsSnapshot())
        {
            if (item.Visible)
            {
                result.Add(item);
            }
        }

        return result;
    }

    private void LayoutItems()
    {
        var visible = GetVisibleItems();
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var radius = BootstrapListGroupRenderLogic.ResolveRadius(BootstrapThemeManager.CurrentTheme.Metrics, _borderRadius, dpi);
        var overlap = GetSeamOverlap();
        var x = Padding.Left;
        var y = Padding.Top;
        var availableWidth = Math.Max(0, ClientSize.Width - Padding.Horizontal);

        for (var index = 0; index < visible.Count; index++)
        {
            var item = visible[index];
            var preferred = item.GetPreferredSize(Size.Empty);
            var corners = BootstrapListGroupRenderLogic.GetCornerRadius(_orientation, index, visible.Count, _flush, radius);
            item.ApplyConnectedGeometry(corners, _orientation == Orientation.Vertical && _flush);
            if (_orientation == Orientation.Vertical)
            {
                item.Bounds = new Rectangle(x, y, availableWidth, Math.Max(0, preferred.Height));
                y += Math.Max(0, item.Height - (index < visible.Count - 1 ? overlap : 0));
            }
            else
            {
                item.Bounds = new Rectangle(x, y, Math.Max(0, preferred.Width), Math.Max(0, preferred.Height));
                x += Math.Max(0, item.Width - (index < visible.Count - 1 ? overlap : 0));
            }
        }

        if (AutoSize)
        {
            var preferred = GetPreferredSize(Size.Empty);
            if (_orientation == Orientation.Vertical)
            {
                if (Height != preferred.Height) Height = preferred.Height;
            }
            else if (Size != preferred)
            {
                Size = preferred;
            }
        }
    }

    private int GetSeamOverlap()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return BootstrapListGroupRenderLogic.GetSeamOverlap(BootstrapThemeManager.CurrentTheme.Metrics, dpi);
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

    private void OnItemClick(object? sender, EventArgs e)
    {
        if (sender is BootstrapListGroupItem item && item.Actionable && item.Enabled)
        {
            RaiseItemClick(item);
        }
    }
}
