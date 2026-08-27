using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired application navigation sidebar composed from framework Button,
/// Collapse, icon, theme, DPI, and animation primitives.
/// </summary>
[DefaultProperty(nameof(Items))]
[DefaultEvent(nameof(SelectedItemChanged))]
public class BootstrapSidebar : Panel
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(200);
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();

    private readonly Func<TimeSpan, Func<double, double>, Control, BootstrapAnimation> _animationFactory;
    private readonly BindingList<BootstrapSidebarItem> _items = new BindingList<BootstrapSidebarItem>();
    private readonly FlowLayoutPanel _itemsHost = new FlowLayoutPanel();
    private readonly Dictionary<BootstrapSidebarItem, BootstrapSidebarItemButton> _buttons = new Dictionary<BootstrapSidebarItem, BootstrapSidebarItemButton>();
    private readonly Dictionary<BootstrapSidebarItem, BootstrapCollapse> _collapses = new Dictionary<BootstrapSidebarItem, BootstrapCollapse>();
    private readonly List<BindingList<BootstrapSidebarItem>> _subscribedCollections = new List<BindingList<BootstrapSidebarItem>>();
    private readonly ToolTip _toolTip = new ToolTip();
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private BootstrapAnimation? _widthAnimation;
    private int _expandedWidth = 260;
    private int _collapsedWidth = 72;
    private bool _expanded = true;
    private TimeSpan _animationDuration = DefaultAnimationDuration;
    private BootstrapSidebarItem? _selectedItem;
    private bool _themeSubscribed;
    private bool _rebuilding;
    private int _transitionStartWidth;
    private int _transitionTargetWidth;

    /// <summary>
    /// Initializes a designer-safe expanded sidebar.
    /// </summary>
    public BootstrapSidebar()
        : this((duration, easing, owner) => new BootstrapAnimation(duration, easing, owner))
    {
    }

    internal BootstrapSidebar(Func<TimeSpan, Func<double, double>, Control, BootstrapAnimation> animationFactory)
    {
        _animationFactory = animationFactory ?? throw new ArgumentNullException(nameof(animationFactory));

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Application navigation sidebar.";
        AutoScroll = true;

        _itemsHost.AutoSize = true;
        _itemsHost.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _itemsHost.Dock = DockStyle.Top;
        _itemsHost.FlowDirection = FlowDirection.TopDown;
        _itemsHost.WrapContents = false;
        _itemsHost.Margin = Padding.Empty;
        _itemsHost.Padding = Padding.Empty;
        Controls.Add(_itemsHost);

        RewireCollectionSubscriptions();
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);

        Size = new Size(GetTargetPixelWidth(true), DpiScaler.Scale(480, CurrentDpi));
        RebuildVisualTree();
    }

    /// <summary>
    /// Gets or sets the logical width used while expanded.
    /// </summary>
    [Category("Layout")]
    [Description("Sets the logical expanded sidebar width. It must remain greater than CollapsedWidth.")]
    [DefaultValue(260)]
    public int ExpandedWidth
    {
        get => _expandedWidth;
        set
        {
            if (value <= _collapsedWidth)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Expanded width must be greater than CollapsedWidth.");
            }

            if (_expandedWidth == value)
            {
                return;
            }

            _expandedWidth = value;
            if (_expanded)
            {
                StartWidthTransitionToCurrentState();
            }
        }
    }

    /// <summary>
    /// Gets or sets the logical width used while collapsed.
    /// </summary>
    [Category("Layout")]
    [Description("Sets the logical collapsed sidebar width. It must remain less than ExpandedWidth.")]
    [DefaultValue(72)]
    public int CollapsedWidth
    {
        get => _collapsedWidth;
        set
        {
            if (value <= 0 || value >= _expandedWidth)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Collapsed width must be positive and less than ExpandedWidth.");
            }

            if (_collapsedWidth == value)
            {
                return;
            }

            _collapsedWidth = value;
            if (!_expanded)
            {
                StartWidthTransitionToCurrentState();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the sidebar is logically expanded.
    /// </summary>
    [Category("Behavior")]
    [Description("Gets or sets whether navigation text and nested sections are expanded.")]
    [DefaultValue(true)]
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value)
            {
                return;
            }

            _expanded = value;
            ApplyPresentationState();
            StartWidthTransitionToCurrentState();
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets or sets the currently selected navigation item. A non-null value must belong to this sidebar tree.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BootstrapSidebarItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value is not null && !ContainsItem(_items, value))
            {
                throw new ArgumentException("SelectedItem must belong to this sidebar.", nameof(value));
            }

            SetSelectedItemCore(value);
        }
    }

    /// <summary>
    /// Gets the root navigation items.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BindingList<BootstrapSidebarItem> Items => _items;

    /// <summary>
    /// Gets or sets the full expanded/collapsed width transition duration.
    /// Nested Collapse sections use the same duration.
    /// </summary>
    [Category("Behavior")]
    [Description("Sets the sidebar width and nested-section animation duration. The value must be greater than zero.")]
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
            foreach (var collapse in _collapses.Values)
            {
                collapse.AnimationDuration = value;
            }

            if (_widthAnimation is not null)
            {
                StartWidthTransitionToCurrentState();
            }
        }
    }

    /// <summary>
    /// Gets or sets the source-neutral icon renderer used by navigation items.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer
    {
        get => _iconRenderer;
        set
        {
            _iconRenderer = value ?? throw new ArgumentNullException(nameof(value));
            foreach (var button in _buttons.Values)
            {
                button.NavigationIconRenderer = _iconRenderer;
            }
        }
    }

    /// <summary>
    /// Occurs when the requested expanded state changes.
    /// </summary>
    public event EventHandler? ExpandedChanged;

    /// <summary>
    /// Occurs when the selected navigation item changes.
    /// </summary>
    public event EventHandler? SelectedItemChanged;

    /// <summary>
    /// Expands the sidebar.
    /// </summary>
    public void Expand()
    {
        Expanded = true;
    }

    /// <summary>
    /// Collapses the sidebar.
    /// </summary>
    public void Collapse()
    {
        Expanded = false;
    }

    /// <summary>
    /// Toggles the expanded state.
    /// </summary>
    public void Toggle()
    {
        Expanded = !Expanded;
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_widthAnimation is null)
        {
            Width = GetTargetPixelWidth(_expanded);
        }

        LayoutVisualTree();
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        LayoutVisualTree();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        DisposeWidthAnimation();
        Width = GetTargetPixelWidth(_expanded);
        RebuildVisualTree();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeWidthAnimation();
            DetachCollectionSubscriptions();
            _toolTip.Dispose();

            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }
        }

        base.Dispose(disposing);
    }

    private int CurrentDpi => DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;

    private void SetSelectedItemCore(BootstrapSidebarItem? value)
    {
        if (ReferenceEquals(_selectedItem, value))
        {
            return;
        }

        var previous = _selectedItem;
        _selectedItem = value;
        if (previous is not null)
        {
            previous.Selected = false;
            UpdateItemVisual(previous);
        }

        if (_selectedItem is not null)
        {
            _selectedItem.Selected = true;
            UpdateItemVisual(_selectedItem);
        }

        SelectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    private void StartWidthTransitionToCurrentState()
    {
        if (IsDisposed)
        {
            return;
        }

        DisposeWidthAnimation();
        var target = GetTargetPixelWidth(_expanded);
        var start = Math.Max(1, Width);
        if (!IsHandleCreated || IsDesignerHosted || start == target)
        {
            Width = target;
            LayoutVisualTree();
            return;
        }

        _transitionStartWidth = start;
        _transitionTargetWidth = target;
        var fullDistance = Math.Max(1, Math.Abs(GetTargetPixelWidth(true) - GetTargetPixelWidth(false)));
        var remainingDistance = Math.Abs(target - start);
        var ratio = Math.Min(1.0, remainingDistance / (double)fullDistance);
        var duration = TimeSpan.FromMilliseconds(Math.Max(1.0, _animationDuration.TotalMilliseconds * ratio));

        var animation = _animationFactory(duration, BootstrapEasing.EaseInOut, this);
        _widthAnimation = animation;
        animation.ProgressChanged += OnWidthAnimationProgressChanged;
        animation.Completed += OnWidthAnimationCompleted;
        animation.Start();
    }

    private void OnWidthAnimationProgressChanged(object? sender, EventArgs e)
    {
        if (sender is not BootstrapAnimation animation || !ReferenceEquals(animation, _widthAnimation))
        {
            return;
        }

        var width = _transitionStartWidth + ((_transitionTargetWidth - _transitionStartWidth) * animation.Progress);
        Width = Math.Max(1, (int)Math.Round(width, MidpointRounding.AwayFromZero));
        LayoutVisualTree();
    }

    private void OnWidthAnimationCompleted(object? sender, EventArgs e)
    {
        if (sender is not BootstrapAnimation animation || !ReferenceEquals(animation, _widthAnimation))
        {
            return;
        }

        Width = _transitionTargetWidth;
        LayoutVisualTree();
        DisposeWidthAnimation();
    }

    private void DisposeWidthAnimation()
    {
        var animation = _widthAnimation;
        if (animation is null)
        {
            return;
        }

        _widthAnimation = null;
        animation.ProgressChanged -= OnWidthAnimationProgressChanged;
        animation.Completed -= OnWidthAnimationCompleted;
        animation.Dispose();
    }

    private void RebuildVisualTree()
    {
        if (_rebuilding || IsDisposed)
        {
            return;
        }

        _rebuilding = true;
        _itemsHost.SuspendLayout();
        try
        {
            ClearVisualControls();
            _buttons.Clear();
            _collapses.Clear();

            var availableWidth = Math.Max(1, ClientSize.Width - Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
            ConfigureHostWidth(_itemsHost, availableWidth);
            BuildItems(_items, _itemsHost, availableWidth, false);
            ApplyPresentationState();
        }
        finally
        {
            _itemsHost.ResumeLayout(true);
            _rebuilding = false;
        }

        LayoutVisualTree();
    }

    private void BuildItems(
        BindingList<BootstrapSidebarItem> items,
        FlowLayoutPanel host,
        int availableWidth,
        bool nested)
    {
        foreach (var item in items)
        {
            var button = CreateItemButton(item);
            button.Margin = CreateRowMargin();
            button.Width = Math.Max(1, availableWidth - host.Padding.Horizontal - button.Margin.Horizontal);
            host.Controls.Add(button);
            _buttons[item] = button;

            if (item.Items.Count == 0)
            {
                continue;
            }

            var childHost = CreateNestedHost(availableWidth);
            BuildItems(item.Items, childHost, availableWidth, true);
            childHost.PerformLayout();

            var collapse = new BootstrapCollapse
            {
                ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed,
                AnimationDuration = _animationDuration,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Width = Math.Max(1, availableWidth - host.Padding.Horizontal)
            };
            collapse.Controls.Add(childHost);
            childHost.Location = Point.Empty;
            childHost.Width = collapse.ClientSize.Width;
            var expandedHeight = MeasureNestedHeight(childHost, collapse.Width);
            collapse.ExpandedHeight = expandedHeight;

            var shouldExpand = _expanded && item.Expanded;
            if (shouldExpand)
            {
                collapse.Height = expandedHeight;
            }
            else
            {
                collapse.Height = 0;
                collapse.Expanded = false;
            }

            childHost.SizeChanged += (_, _) =>
            {
                if (!collapse.IsDisposed)
                {
                    collapse.ExpandedHeight = MeasureNestedHeight(childHost, collapse.Width);
                }
            };

            host.Controls.Add(collapse);
            _collapses[item] = collapse;
        }
    }

    private BootstrapSidebarItemButton CreateItemButton(BootstrapSidebarItem item)
    {
        var button = new BootstrapSidebarItemButton(_iconRenderer)
        {
            Height = GetRowHeight(),
            Tag = item,
            TabStop = true,
            AccessibleRole = AccessibleRole.PushButton
        };
        button.Click += OnItemButtonClick;
        button.KeyDown += OnItemButtonKeyDown;
        UpdateButton(button, item);
        return button;
    }

    private FlowLayoutPanel CreateNestedHost(int availableWidth)
    {
        var host = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = Color.Transparent
        };
        var indent = DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.SpacingLG, CurrentDpi);
        host.Padding = new Padding(indent, 0, 0, 0);
        ConfigureHostWidth(host, Math.Max(1, availableWidth));
        return host;
    }

    private void UpdateButton(BootstrapSidebarItemButton button, BootstrapSidebarItem item)
    {
        button.Text = _expanded ? item.Text : string.Empty;
        button.NavigationIcon = item.Icon;
        button.NavigationIconRenderer = _iconRenderer;
        button.BadgeText = _expanded ? item.BadgeText : string.Empty;
        button.HasChildren = item.Items.Count > 0;
        button.SectionExpanded = item.Expanded;
        button.CollapsedMode = !_expanded;
        button.Selected = item.Selected;
        button.Enabled = item.Enabled;
        button.AccessibleName = item.Text;
        button.AccessibleDescription = BuildItemAccessibleDescription(item);
        _toolTip.SetToolTip(button, _expanded ? string.Empty : item.Text);
    }

    private string BuildItemAccessibleDescription(BootstrapSidebarItem item)
    {
        var state = item.Selected ? "Selected navigation item." : "Navigation item.";
        if (item.Items.Count > 0)
        {
            state += item.Expanded ? " Section expanded." : " Section collapsed.";
        }

        if (!string.IsNullOrEmpty(item.BadgeText))
        {
            state += " Badge " + item.BadgeText + ".";
        }

        return state;
    }

    private void ApplyPresentationState()
    {
        foreach (var pair in _buttons)
        {
            UpdateButton(pair.Value, pair.Key);
        }

        foreach (var pair in _collapses)
        {
            var shouldExpand = _expanded && pair.Key.Expanded;
            if (shouldExpand)
            {
                pair.Value.Expand();
            }
            else
            {
                pair.Value.Collapse();
            }
        }

        RefreshNavigationInteractivity();
    }

    private void UpdateItemVisual(BootstrapSidebarItem item)
    {
        if (_buttons.TryGetValue(item, out var button))
        {
            UpdateButton(button, item);
        }

        if (_collapses.TryGetValue(item, out var collapse))
        {
            if (_expanded && item.Expanded)
            {
                collapse.Expand();
            }
            else
            {
                collapse.Collapse();
            }
        }

        RefreshNavigationInteractivity();
    }

    private void RefreshNavigationInteractivity()
    {
        RefreshNavigationInteractivity(_items, true);
    }

    private void RefreshNavigationInteractivity(BindingList<BootstrapSidebarItem> items, bool rowsVisible)
    {
        foreach (var item in items)
        {
            if (_buttons.TryGetValue(item, out var button))
            {
                button.TabStop = rowsVisible && item.Enabled;
            }

            if (item.Items.Count > 0)
            {
                var childrenVisible = rowsVisible && _expanded && item.Expanded;
                RefreshNavigationInteractivity(item.Items, childrenVisible);
            }
        }
    }

    private void OnItemButtonClick(object? sender, EventArgs e)
    {
        if (sender is not BootstrapSidebarItemButton button || button.Tag is not BootstrapSidebarItem item || !item.Enabled)
        {
            return;
        }

        SelectedItem = item;
        if (item.Items.Count == 0)
        {
            return;
        }

        if (!_expanded)
        {
            Expanded = true;
            item.Expanded = true;
        }
        else
        {
            item.Expanded = !item.Expanded;
        }
    }

    private void OnItemButtonKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not BootstrapSidebarItemButton button || button.Tag is not BootstrapSidebarItem item)
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Up:
                e.Handled = FocusRelativeItem(item, -1);
                break;
            case Keys.Down:
                e.Handled = FocusRelativeItem(item, 1);
                break;
            case Keys.Home:
                e.Handled = FocusBoundaryItem(first: true);
                break;
            case Keys.End:
                e.Handled = FocusBoundaryItem(first: false);
                break;
            case Keys.Left:
                if (item.Items.Count > 0 && item.Expanded)
                {
                    item.Expanded = false;
                    e.Handled = true;
                }
                else if (_expanded)
                {
                    Collapse();
                    e.Handled = true;
                }

                break;
            case Keys.Right:
                if (!_expanded)
                {
                    Expand();
                    e.Handled = true;
                }
                else if (item.Items.Count > 0 && !item.Expanded)
                {
                    item.Expanded = true;
                    e.Handled = true;
                }
                else if (item.Items.Count > 0)
                {
                    e.Handled = FocusFirstEnabledChild(item);
                }

                break;
        }

        if (e.Handled)
        {
            e.SuppressKeyPress = true;
        }
    }

    private bool FocusRelativeItem(BootstrapSidebarItem current, int direction)
    {
        var visible = new List<BootstrapSidebarItem>();
        CollectVisibleEnabledItems(_items, visible);
        var index = visible.IndexOf(current);
        if (index < 0 || visible.Count == 0)
        {
            return false;
        }

        var next = index + direction;
        if (next < 0 || next >= visible.Count)
        {
            return false;
        }

        return FocusItem(visible[next]);
    }

    private bool FocusBoundaryItem(bool first)
    {
        var visible = new List<BootstrapSidebarItem>();
        CollectVisibleEnabledItems(_items, visible);
        if (visible.Count == 0)
        {
            return false;
        }

        return FocusItem(first ? visible[0] : visible[visible.Count - 1]);
    }

    private bool FocusFirstEnabledChild(BootstrapSidebarItem item)
    {
        foreach (var child in item.Items)
        {
            if (child.Enabled && FocusItem(child))
            {
                return true;
            }
        }

        return false;
    }

    private bool FocusItem(BootstrapSidebarItem item)
    {
        return _buttons.TryGetValue(item, out var button) && button.CanFocus && button.Focus();
    }

    private void CollectVisibleEnabledItems(BindingList<BootstrapSidebarItem> items, List<BootstrapSidebarItem> result)
    {
        foreach (var item in items)
        {
            if (item.Enabled)
            {
                result.Add(item);
            }

            if (_expanded && item.Expanded && item.Items.Count > 0)
            {
                CollectVisibleEnabledItems(item.Items, result);
            }
        }
    }

    private void RewireCollectionSubscriptions()
    {
        DetachCollectionSubscriptions();
        AttachCollectionRecursive(_items);
    }

    private void AttachCollectionRecursive(BindingList<BootstrapSidebarItem> collection)
    {
        if (_subscribedCollections.Contains(collection))
        {
            return;
        }

        _subscribedCollections.Add(collection);
        collection.ListChanged += OnItemCollectionChanged;
        foreach (var item in collection)
        {
            AttachCollectionRecursive(item.Items);
        }
    }

    private void DetachCollectionSubscriptions()
    {
        foreach (var collection in _subscribedCollections)
        {
            collection.ListChanged -= OnItemCollectionChanged;
        }

        _subscribedCollections.Clear();
    }

    private void OnItemCollectionChanged(object? sender, ListChangedEventArgs e)
    {
        if (_rebuilding || IsDisposed)
        {
            return;
        }

        if (sender is BindingList<BootstrapSidebarItem> collection &&
            e.ListChangedType == ListChangedType.ItemChanged &&
            e.PropertyDescriptor is not null &&
            e.NewIndex >= 0 &&
            e.NewIndex < collection.Count)
        {
            UpdateItemVisual(collection[e.NewIndex]);
            return;
        }

        RewireCollectionSubscriptions();
        if (_selectedItem is not null && !ContainsItem(_items, _selectedItem))
        {
            SetSelectedItemCore(null);
        }

        RebuildVisualTree();
    }

    private void LayoutVisualTree()
    {
        if (_rebuilding || IsDisposed)
        {
            return;
        }

        var scrollbarReserve = VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        var availableWidth = Math.Max(1, ClientSize.Width - Padding.Horizontal - scrollbarReserve);
        ConfigureHostWidth(_itemsHost, availableWidth);
        LayoutHost(_itemsHost, availableWidth);
    }

    private void LayoutHost(FlowLayoutPanel host, int availableWidth)
    {
        ConfigureHostWidth(host, availableWidth);
        foreach (Control control in host.Controls)
        {
            var width = Math.Max(1, availableWidth - host.Padding.Horizontal - control.Margin.Horizontal);
            control.Width = width;
            if (control is BootstrapCollapse collapse && collapse.Controls.Count > 0 && collapse.Controls[0] is FlowLayoutPanel childHost)
            {
                var childWidth = Math.Max(1, width);
                LayoutHost(childHost, childWidth);
                collapse.ExpandedHeight = MeasureNestedHeight(childHost, childWidth);
            }
        }
    }

    private void ConfigureHostWidth(FlowLayoutPanel host, int width)
    {
        width = Math.Max(1, width);
        host.MinimumSize = new Size(width, 0);
        host.MaximumSize = new Size(width, 0);
        host.Width = width;
    }

    private int MeasureNestedHeight(FlowLayoutPanel host, int width)
    {
        host.PerformLayout();
        var preferred = host.GetPreferredSize(new Size(Math.Max(1, width), 0));
        return Math.Max(host.Padding.Vertical, preferred.Height);
    }

    private Padding CreateRowMargin()
    {
        var spacing = DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.SpacingXS, CurrentDpi);
        return new Padding(0, spacing, 0, spacing);
    }

    private int GetRowHeight()
    {
        return DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.ControlHeight, CurrentDpi);
    }

    private int GetTargetPixelWidth(bool expanded)
    {
        return DpiScaler.Scale(expanded ? _expandedWidth : _collapsedWidth, CurrentDpi);
    }

    private static bool ContainsItem(BindingList<BootstrapSidebarItem> items, BootstrapSidebarItem target)
    {
        foreach (var item in items)
        {
            if (ReferenceEquals(item, target) || ContainsItem(item.Items, target))
            {
                return true;
            }
        }

        return false;
    }

    private void ClearVisualControls()
    {
        while (_itemsHost.Controls.Count > 0)
        {
            var control = _itemsHost.Controls[0];
            _itemsHost.Controls.RemoveAt(0);
            control.Dispose();
        }
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Surface;
        ForeColor = theme.Colors.Text;
        _itemsHost.BackColor = theme.Colors.Surface;
        Invalidate();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        ApplyTheme(e.NewTheme);
        if (e.OldTheme.ReducedMotion != e.NewTheme.ReducedMotion && _widthAnimation is not null)
        {
            StartWidthTransitionToCurrentState();
        }

        RebuildVisualTree();
    }

    private bool IsDesignerHosted => DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
}
