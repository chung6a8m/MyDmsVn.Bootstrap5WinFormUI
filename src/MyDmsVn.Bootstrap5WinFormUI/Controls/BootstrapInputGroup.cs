using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Arranges supported Bootstrap controls as one connected horizontal input surface.</summary>
public class BootstrapInputGroup : Panel
{
    private readonly List<Control> _canonicalChildren = new List<Control>();
    private readonly HashSet<Control> _interactionSources = new HashSet<Control>();
    private readonly HashSet<Control> _hoveredSources = new HashSet<Control>();
    private readonly HashSet<Control> _pressedSources = new HashSet<Control>();
    private BootstrapInputGroupSize _inputGroupSize = BootstrapInputGroupSize.Default;
    private bool _performingLayout;
    private bool _updatingVisualOrder;
    private bool _themeSubscribed;

    /// <summary>Initializes a designer-safe horizontal input group.</summary>
    public BootstrapInputGroup()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Connected Bootstrap input group.";
        var theme = BootstrapThemeManager.CurrentTheme;
        Size = new Size(320, DpiScaler.Scale(theme.Metrics.ControlHeight, EffectiveDpi));
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
    }

    /// <summary>Gets or sets the shared Small, Default, or Large presentation applied internally to child controls.</summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapInputGroupSize.Default)]
    public BootstrapInputGroupSize InputGroupSize
    {
        get => _inputGroupSize;
        set
        {
            if (!Enum.IsDefined(typeof(BootstrapInputGroupSize), value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported input group size.");
            }
            if (_inputGroupSize == value) return;
            _inputGroupSize = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var visible = GetVisibleCanonicalChildren();
        var rowHeight = ResolveRowHeight(visible);
        var items = BuildLayoutItems(visible);
        var naturalWidth = items.Sum(item => item.PreferredWidth) - Math.Max(0, visible.Count - 1) * ResolveSeamOverlap();
        return new Size(Math.Max(1, naturalWidth), Math.Max(1, rowHeight));
    }

    /// <inheritdoc />
    protected override Control.ControlCollection CreateControlsInstance()
    {
        return new InputGroupControlCollection(this);
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (_performingLayout || IsDisposed) return;
        _performingLayout = true;
        try
        {
            LayoutConnectedChildren();
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
    protected override void OnRightToLeftChanged(EventArgs e)
    {
        base.OnRightToLeftChanged(e);
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
            foreach (var child in _canonicalChildren.ToArray()) DetachChild(child);
            _canonicalChildren.Clear();
        }
        base.Dispose(disposing);
    }

    private int EffectiveDpi => DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;

    private void ValidateChild(Control child)
    {
        if (child is null) throw new ArgumentNullException(nameof(child));
        var supported = child is BootstrapInputGroupText || child is BootstrapTextBox ||
            child is BootstrapNumericBox || child is BootstrapButton || child is BootstrapSplitButton ||
            child is BootstrapSelect;
        if (!supported)
        {
            throw new NotSupportedException("BootstrapInputGroup does not support direct child type " + child.GetType().FullName + ".");
        }
        if (child is BootstrapSelect select && select.SelectionMode != BootstrapSelectMode.Single)
        {
            throw new NotSupportedException("BootstrapInputGroup supports BootstrapSelect only in Single mode.");
        }
    }

    private void ChildAdded(Control child)
    {
        if (_canonicalChildren.Contains(child)) return;
        _canonicalChildren.Add(child);
        child.VisibleChanged += OnChildVisibleChanged;
        child.EnabledChanged += OnChildEnabledChanged;
        child.SizeChanged += OnChildLayoutChanged;
        child.TextChanged += OnChildLayoutChanged;
        AttachInteractionTree(child);
        PerformLayout();
        UpdateVisualOrder();
    }

    private void ChildRemoved(Control child)
    {
        _canonicalChildren.Remove(child);
        DetachChild(child);
        PerformLayout();
    }

    private void ChildReordered(Control child, int newIndex)
    {
        var current = _canonicalChildren.IndexOf(child);
        if (current < 0) return;
        _canonicalChildren.RemoveAt(current);
        _canonicalChildren.Insert(Math.Max(0, Math.Min(newIndex, _canonicalChildren.Count)), child);
        PerformLayout();
        UpdateVisualOrder();
    }

    private void DetachChild(Control child)
    {
        child.VisibleChanged -= OnChildVisibleChanged;
        child.EnabledChanged -= OnChildEnabledChanged;
        child.SizeChanged -= OnChildLayoutChanged;
        child.TextChanged -= OnChildLayoutChanged;
        DetachInteractionTree(child);
        ClearInteractionState(child);
        if (child is IBootstrapConnectedControl connected)
        {
            connected.ConnectedCornerRadius = null;
            connected.ConnectedSizeOverride = null;
        }
    }

    private void AttachInteractionTree(Control source)
    {
        if (!_interactionSources.Add(source)) return;
        source.Enter += OnChildFocusChanged;
        source.Leave += OnChildFocusChanged;
        source.MouseEnter += OnChildMouseEnter;
        source.MouseLeave += OnChildMouseLeave;
        source.MouseDown += OnChildMouseDown;
        source.MouseUp += OnChildMouseUp;
        source.ControlAdded += OnInteractionControlAdded;
        source.ControlRemoved += OnInteractionControlRemoved;
        foreach (Control child in source.Controls) AttachInteractionTree(child);
    }

    private void DetachInteractionTree(Control source)
    {
        foreach (Control child in source.Controls) DetachInteractionTree(child);
        if (!_interactionSources.Remove(source)) return;
        source.Enter -= OnChildFocusChanged;
        source.Leave -= OnChildFocusChanged;
        source.MouseEnter -= OnChildMouseEnter;
        source.MouseLeave -= OnChildMouseLeave;
        source.MouseDown -= OnChildMouseDown;
        source.MouseUp -= OnChildMouseUp;
        source.ControlAdded -= OnInteractionControlAdded;
        source.ControlRemoved -= OnInteractionControlRemoved;
        _hoveredSources.Remove(source);
        _pressedSources.Remove(source);
    }

    private void ClearInteractionState(Control child)
    {
        _hoveredSources.RemoveWhere(source => IsWithin(source, child));
        _pressedSources.RemoveWhere(source => IsWithin(source, child));
        UpdateVisualOrder();
    }

    private void UpdateVisualOrder()
    {
        if (_updatingVisualOrder || IsDisposed) return;
        var ordered = _canonicalChildren
            .Select((child, index) => new { Child = child, Index = index, Priority = GetVisualPriority(child) })
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.Index)
            .Select(item => item.Child)
            .ToArray();
        _updatingVisualOrder = true;
        try
        {
            for (var i = 0; i < ordered.Length; i++)
            {
                if (Controls.GetChildIndex(ordered[i]) != i)
                {
                    Controls.SetChildIndex(ordered[i], i);
                }
            }
        }
        finally
        {
            _updatingVisualOrder = false;
        }
    }

    private int GetVisualPriority(Control child)
    {
        if (child.ContainsFocus) return 3;
        if (_pressedSources.Any(source => IsWithin(source, child))) return 2;
        if (_hoveredSources.Any(source => IsWithin(source, child))) return 1;
        return 0;
    }

    private static bool IsWithin(Control source, Control ancestor)
    {
        for (Control? current = source; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor)) return true;
        }
        return false;
    }

    private void LayoutConnectedChildren()
    {
        foreach (var hidden in _canonicalChildren.Where(child => !child.Visible))
        {
            var connected = (IBootstrapConnectedControl)hidden;
            connected.ConnectedCornerRadius = null;
            connected.ConnectedSizeOverride = null;
        }
        var visible = GetVisibleCanonicalChildren();
        var rowHeight = ResolveRowHeight(visible);
        if (ClientSize.Height != rowHeight)
        {
            SetClientSizeCore(ClientSize.Width, rowHeight);
        }
        if (visible.Count == 0) return;
        ApplyConnectedOverrides(visible);
        var result = BootstrapInputGroupLayoutLogic.Calculate(
            BuildLayoutItems(visible), Math.Max(0, ClientSize.Width), rowHeight,
            ResolveSeamOverlap(), RightToLeft == RightToLeft.Yes);
        for (var i = 0; i < visible.Count; i++) visible[i].Bounds = result.Bounds[i];
    }

    private void ApplyConnectedOverrides(IReadOnlyList<Control> visible)
    {
        var size = MapSize(_inputGroupSize);
        var theme = BootstrapThemeManager.CurrentTheme.Metrics;
        var radius = _inputGroupSize == BootstrapInputGroupSize.Small ? theme.RadiusSmall :
            (_inputGroupSize == BootstrapInputGroupSize.Large ? theme.RadiusLarge : theme.Radius);
        for (var i = 0; i < visible.Count; i++)
        {
            var connected = (IBootstrapConnectedControl)visible[i];
            connected.ConnectedSizeOverride = size;
            var visualIndex = RightToLeft == RightToLeft.Yes ? visible.Count - 1 - i : i;
            connected.ConnectedCornerRadius = BootstrapConnectedControlLayoutLogic.ResolveCornerRadius(
                Orientation.Horizontal, visualIndex, visible.Count, radius);
        }
    }

    private int ResolveRowHeight(IReadOnlyList<Control> visible)
    {
        var connectedSize = MapSize(_inputGroupSize);
        var metrics = BootstrapThemeManager.CurrentTheme.Metrics;
        var logical = _inputGroupSize == BootstrapInputGroupSize.Small ? metrics.ControlHeightSmall :
            (_inputGroupSize == BootstrapInputGroupSize.Large ? metrics.ControlHeightLarge : metrics.ControlHeight);
        var height = DpiScaler.Scale(logical, EffectiveDpi);
        foreach (var child in visible)
        {
            var connected = (IBootstrapConnectedControl)child;
            connected.ConnectedSizeOverride = connectedSize;
            height = Math.Max(height, connected.GetConnectedSafeMinimumHeight(connectedSize, EffectiveDpi));
        }
        return Math.Max(1, height);
    }

    private IReadOnlyList<BootstrapInputGroupLayoutItem> BuildLayoutItems(IReadOnlyList<Control> visible)
    {
        var minimumInput = DpiScaler.Scale(60, EffectiveDpi);
        var minimumFixed = DpiScaler.Scale(20, EffectiveDpi);
        return visible.Select(child =>
        {
            var preferred = child.GetPreferredSize(Size.Empty).Width;
            var stretch = child is BootstrapTextBox || child is BootstrapNumericBox || child is BootstrapSelect;
            return new BootstrapInputGroupLayoutItem(preferred, stretch ? minimumInput : Math.Min(preferred, minimumFixed), stretch);
        }).ToArray();
    }

    private List<Control> GetVisibleCanonicalChildren() => _canonicalChildren.Where(child => child.Visible).ToList();
    private int ResolveSeamOverlap() => BootstrapConnectedControlLayoutLogic.ResolveSeamOverlap(BootstrapThemeManager.CurrentTheme.Metrics, EffectiveDpi);
    private static BootstrapConnectedControlSize MapSize(BootstrapInputGroupSize size) =>
        size == BootstrapInputGroupSize.Small ? BootstrapConnectedControlSize.Small :
        (size == BootstrapInputGroupSize.Large ? BootstrapConnectedControlSize.Large : BootstrapConnectedControlSize.Default);

    private void OnChildLayoutChanged(object? sender, EventArgs e)
    {
        if (!_performingLayout) PerformLayout();
    }

    private void OnChildVisibleChanged(object? sender, EventArgs e)
    {
        if (sender is Control child && !child.Visible) ClearInteractionState(child);
        OnChildLayoutChanged(sender, e);
    }

    private void OnChildEnabledChanged(object? sender, EventArgs e)
    {
        if (sender is Control child && !child.Enabled) ClearInteractionState(child);
        UpdateVisualOrder();
    }

    private void OnChildFocusChanged(object? sender, EventArgs e) => UpdateVisualOrder();

    private void OnChildMouseEnter(object? sender, EventArgs e)
    {
        if (sender is Control source) _hoveredSources.Add(source);
        UpdateVisualOrder();
    }

    private void OnChildMouseLeave(object? sender, EventArgs e)
    {
        if (sender is Control source)
        {
            _hoveredSources.Remove(source);
            _pressedSources.Remove(source);
        }
        UpdateVisualOrder();
    }

    private void OnChildMouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is Control source && e.Button == MouseButtons.Left) _pressedSources.Add(source);
        UpdateVisualOrder();
    }

    private void OnChildMouseUp(object? sender, MouseEventArgs e)
    {
        if (sender is Control source) _pressedSources.Remove(source);
        UpdateVisualOrder();
    }

    private void OnInteractionControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is not null) AttachInteractionTree(e.Control);
    }

    private void OnInteractionControlRemoved(object? sender, ControlEventArgs e)
    {
        if (e.Control is not null) DetachInteractionTree(e.Control);
        UpdateVisualOrder();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        PerformLayout();
        Invalidate();
    }

    private sealed class InputGroupControlCollection : Control.ControlCollection
    {
        private readonly BootstrapInputGroup _owner;
        internal InputGroupControlCollection(BootstrapInputGroup owner) : base(owner) { _owner = owner; }

        public override void Add(Control? value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            _owner.ValidateChild(value);
            base.Add(value);
            _owner.ChildAdded(value);
        }

        public override void Remove(Control? value)
        {
            if (value is null) return;
            if (!Contains(value)) return;
            base.Remove(value);
            _owner.ChildRemoved(value);
        }

        public override void SetChildIndex(Control child, int newIndex)
        {
            base.SetChildIndex(child, newIndex);
            if (!_owner._updatingVisualOrder)
            {
                _owner.ChildReordered(child, GetChildIndex(child));
            }
        }
    }
}
