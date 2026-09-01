using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Select2-style Bootstrap-themed WinForms selection surface with extensible search and rendering.
/// </summary>
[DefaultEvent(nameof(SelectionChanged))]
[DefaultProperty(nameof(Placeholder))]
public partial class BootstrapSelect : UserControl
{
    private BootstrapSelectMode _selectionMode = BootstrapSelectMode.Single;
    private BootstrapSelectSelectionState _selectionState;
    private IEqualityComparer<object> _valueComparer = EqualityComparer<object>.Default;
    private string _placeholder = "Select...";
    private bool _allowClear = true;
    private bool _allowCustomValues;
    private bool _searchEnabled = true;
    private bool? _closeOnSelectOverride;
    private int _minimumSearchLength;
    private TimeSpan _searchDebounce = TimeSpan.FromMilliseconds(250);
    private int _pageSize = 20;
    private int _dropDownWidth;
    private int _maxDropDownHeight = 320;
    private int _resultRowHeight = 32;
    private int _maximumSelectionRows = 3;
    private BootstrapValidationState _validationState;
    private int _borderRadius = -1;
    private IBootstrapSelectMatcher _matcher = new BootstrapSelectTextMatcher();
    private IBootstrapSelectRenderer _renderer = new BootstrapSelectRenderer();

    /// <summary>Initializes a designer-safe BootstrapSelect.</summary>
    public BootstrapSelect()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        Size = new Size(220, 32);
        MinimumSize = new Size(60, 24);
        _selectionState = new BootstrapSelectSelectionState(_selectionMode, _valueComparer);
        Items = new BootstrapSelectItemCollection(OnItemsChanged);
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
    }

    /// <summary>Occurs before an item is selected and allows the pending selection to be cancelled.</summary>
    public event EventHandler<BootstrapSelectItemCancelEventArgs>? Selecting;

    /// <summary>Occurs after an item has been selected.</summary>
    public event EventHandler<BootstrapSelectItemEventArgs>? Selected;

    /// <summary>Occurs before an item is deselected and allows the pending deselection to be cancelled.</summary>
    public event EventHandler<BootstrapSelectItemCancelEventArgs>? Deselecting;

    /// <summary>Occurs after an item has been deselected.</summary>
    public event EventHandler<BootstrapSelectItemEventArgs>? Deselected;

    /// <summary>Occurs after one logical selection mutation or selection batch completes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Gets or sets single or multiple selection behavior.</summary>
    [Category("Behavior")]
    [DefaultValue(BootstrapSelectMode.Single)]
    public BootstrapSelectMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            if (!Enum.IsDefined(typeof(BootstrapSelectMode), value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported BootstrapSelect selection mode.");
            }

            if (_selectionMode == value)
            {
                return;
            }

            var mutation = _selectionState.PreviewModeChange(value);
            for (var i = 0; i < mutation.RemovedItems.Count; i++)
            {
                if (!CanDeselect(mutation.RemovedItems[i], BootstrapSelectChangeReason.ModeChange))
                {
                    return;
                }
            }

            _selectionState.Apply(mutation);
            _selectionMode = value;
            for (var i = 0; i < mutation.RemovedItems.Count; i++)
            {
                OnDeselected(mutation.RemovedItems[i], BootstrapSelectChangeReason.ModeChange);
            }

            Invalidate();
            if (mutation.RemovedItems.Count > 0)
            {
                OnSelectionChanged();
            }
        }
    }

    /// <summary>Gets the caller-owned local item collection.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Category("Data")]
    public BootstrapSelectItemCollection Items { get; }

    /// <summary>Gets or sets the single logical selected item; assigning null clears the selection.</summary>
    [Browsable(false)]
    public BootstrapSelectItem? SelectedItem
    {
        get => _selectionState.SelectedItems.Count == 0 ? null : _selectionState.SelectedItems[0];
        set
        {
            if (value is null)
            {
                ClearSelectionCore(BootstrapSelectChangeReason.Programmatic);
            }
            else
            {
                SelectCore(value, BootstrapSelectChangeReason.Programmatic);
            }
        }
    }

    /// <summary>Gets selected items in logical selection order.</summary>
    [Browsable(false)]
    public IReadOnlyList<BootstrapSelectItem> SelectedItems => _selectionState.SelectedItems;

    /// <summary>Gets or sets the single logical selected value; assigning null clears the selection.</summary>
    [Browsable(false)]
    public object? SelectedValue
    {
        get => SelectedItem?.Value;
        set
        {
            if (value is null)
            {
                ClearSelectionCore(BootstrapSelectChangeReason.Programmatic);
            }
            else
            {
                SelectValue(value);
            }
        }
    }

    /// <summary>Gets selected logical values in selection order.</summary>
    [Browsable(false)]
    public IReadOnlyList<object> SelectedValues
    {
        get
        {
            var values = new List<object>(_selectionState.SelectedItems.Count);
            for (var i = 0; i < _selectionState.SelectedItems.Count; i++)
            {
                values.Add(_selectionState.SelectedItems[i].Value);
            }

            return new ReadOnlyCollection<object>(values);
        }
    }

    /// <summary>Gets or sets the comparer that defines logical item identity.</summary>
    [Browsable(false)]
    public IEqualityComparer<object> ValueComparer
    {
        get => _valueComparer;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (ReferenceEquals(_valueComparer, value))
            {
                return;
            }

            var snapshot = new List<BootstrapSelectItem>(_selectionState.SelectedItems);
            _valueComparer = value;
            _selectionState = new BootstrapSelectSelectionState(_selectionMode, _valueComparer);
            for (var i = 0; i < snapshot.Count; i++)
            {
                _selectionState.TrySelect(snapshot[i], BootstrapSelectChangeReason.Programmatic);
            }

            ResetRemoteSearchController();
            RefreshAndRestartRemoteSearchIfOpen();
            Invalidate();
        }
    }

    /// <summary>Gets or sets text shown when no value is selected.</summary>
    [Category("Appearance")]
    [DefaultValue("Select...")]
    public string Placeholder
    {
        get => _placeholder;
        set
        {
            _placeholder = value ?? throw new ArgumentNullException(nameof(value));
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether a clear action is available when selection exists.</summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool AllowClear
    {
        get => _allowClear;
        set
        {
            if (_allowClear == value)
            {
                return;
            }

            _allowClear = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether custom values may be created.</summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool AllowCustomValues
    {
        get => _allowCustomValues;
        set => _allowCustomValues = value;
    }

    /// <summary>Gets or sets whether search input is displayed in the popup.</summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool SearchEnabled
    {
        get => _searchEnabled;
        set => _searchEnabled = value;
    }

    /// <summary>Gets or sets whether a successful selection closes the popup. The untouched default is mode-sensitive.</summary>
    [Category("Behavior")]
    public bool CloseOnSelect
    {
        get => _closeOnSelectOverride ?? _selectionMode == BootstrapSelectMode.Single;
        set => _closeOnSelectOverride = value;
    }

    /// <summary>Gets or sets the minimum number of search characters required before querying.</summary>
    [Category("Behavior")]
    [DefaultValue(0)]
    public int MinimumSearchLength
    {
        get => _minimumSearchLength;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Minimum search length cannot be negative.");
            }

            _minimumSearchLength = value;
        }
    }

    /// <summary>Gets or sets the async-search debounce duration.</summary>
    [Category("Behavior")]
    public TimeSpan SearchDebounce
    {
        get => _searchDebounce;
        set
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Search debounce cannot be negative.");
            }

            _searchDebounce = value;
        }
    }

    /// <summary>Gets or sets the requested async page size.</summary>
    [Category("Behavior")]
    [DefaultValue(20)]
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Page size must be positive.");
            }

            _pageSize = value;
        }
    }

    /// <summary>Gets or sets popup width in logical pixels; zero selects automatic owner-relative width.</summary>
    [Category("Layout")]
    [DefaultValue(0)]
    public int DropDownWidth
    {
        get => _dropDownWidth;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Drop-down width cannot be negative.");
            }

            _dropDownWidth = value;
        }
    }

    /// <summary>Gets or sets maximum popup height in logical pixels.</summary>
    [Category("Layout")]
    [DefaultValue(320)]
    public int MaxDropDownHeight
    {
        get => _maxDropDownHeight;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum drop-down height must be positive.");
            }

            _maxDropDownHeight = value;
        }
    }

    /// <summary>Gets or sets the uniform popup result-row height in logical 96-DPI pixels.</summary>
    [Category("Layout")]
    [DefaultValue(32)]
    public int ResultRowHeight
    {
        get => _resultRowHeight;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Result row height must be positive.");
            }

            if (_resultRowHeight == value)
            {
                return;
            }

            _resultRowHeight = value;
            RefreshDropDownPresentationAndLayout();
        }
    }

    /// <summary>Gets or sets the maximum visible chip rows before overflow is applied.</summary>
    [Category("Layout")]
    [DefaultValue(3)]
    public int MaximumSelectionRows
    {
        get => _maximumSelectionRows;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum selection rows must be positive.");
            }

            _maximumSelectionRows = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets validation presentation.</summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapValidationState.None)]
    public BootstrapValidationState ValidationState
    {
        get => _validationState;
        set
        {
            BootstrapTextBoxRenderLogic.ValidateState(value);
            _validationState = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets logical corner radius; -1 uses the current theme radius.</summary>
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

            _borderRadius = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets the local-mode matcher. Caller-provided matchers remain caller-owned.</summary>
    [Browsable(false)]
    public IBootstrapSelectMatcher Matcher
    {
        get => _matcher;
        set => _matcher = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets visual rendering behavior. Caller-provided renderers remain caller-owned.</summary>
    [Browsable(false)]
    public IBootstrapSelectRenderer Renderer
    {
        get => _renderer;
        set
        {
            _renderer = value ?? throw new ArgumentNullException(nameof(value));
            Invalidate();
        }
    }

    /// <summary>Gets or sets the optional synchronous custom-value factory.</summary>
    [Browsable(false)]
    public Func<string, BootstrapSelectItem?>? CustomValueFactory { get; set; }

    /// <summary>Selects an item using logical value identity.</summary>
    /// <returns>true when the logical selection changed; otherwise false.</returns>
    public bool Select(BootstrapSelectItem item)
    {
        return SelectCore(item ?? throw new ArgumentNullException(nameof(item)), BootstrapSelectChangeReason.Programmatic);
    }

    /// <summary>Selects the local item whose logical value matches the supplied value.</summary>
    /// <returns>true when a matching value exists and the logical selection changed; otherwise false.</returns>
    public bool SelectValue(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var item = FindLocalItemByValue(value);
        return item is not null && SelectCore(item, BootstrapSelectChangeReason.Programmatic);
    }

    /// <summary>Deselects an item using logical value identity.</summary>
    /// <returns>true when the logical selection changed; otherwise false.</returns>
    public bool Deselect(BootstrapSelectItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return DeselectCore(item.Value, BootstrapSelectChangeReason.Programmatic);
    }

    /// <summary>Deselects the selected item whose logical value matches the supplied value.</summary>
    /// <returns>true when the logical selection changed; otherwise false.</returns>
    public bool DeselectValue(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return DeselectCore(value, BootstrapSelectChangeReason.Programmatic);
    }

    /// <summary>Clears all deselections that are not cancelled by a Deselecting handler.</summary>
    public void ClearSelection()
    {
        ClearSelectionCore(BootstrapSelectChangeReason.Clear);
    }

    internal bool SelectCore(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        var mutation = _selectionState.PreviewSelect(item, reason);
        if (!mutation.Changed)
        {
            return false;
        }

        for (var i = 0; i < mutation.RemovedItems.Count; i++)
        {
            if (!CanDeselect(mutation.RemovedItems[i], reason))
            {
                return false;
            }
        }

        for (var i = 0; i < mutation.AddedItems.Count; i++)
        {
            if (!CanSelect(mutation.AddedItems[i], reason))
            {
                return false;
            }
        }

        _selectionState.Apply(mutation);
        for (var i = 0; i < mutation.RemovedItems.Count; i++)
        {
            OnDeselected(mutation.RemovedItems[i], reason);
        }

        for (var i = 0; i < mutation.AddedItems.Count; i++)
        {
            OnSelected(mutation.AddedItems[i], reason);
        }

        Invalidate();
        OnSelectionChanged();
        return true;
    }

    internal bool DeselectCore(object value, BootstrapSelectChangeReason reason)
    {
        var mutation = _selectionState.PreviewRemove(value, reason);
        if (!mutation.Changed)
        {
            return false;
        }

        var item = mutation.RemovedItems[0];
        if (!CanDeselect(item, reason))
        {
            return false;
        }

        _selectionState.Apply(mutation);
        OnDeselected(item, reason);
        Invalidate();
        OnSelectionChanged();
        return true;
    }

    internal void ClearSelectionCore(BootstrapSelectChangeReason reason)
    {
        if (_selectionState.SelectedItems.Count == 0)
        {
            return;
        }

        var snapshot = new List<BootstrapSelectItem>(_selectionState.SelectedItems);
        var changed = false;
        for (var i = 0; i < snapshot.Count; i++)
        {
            var item = snapshot[i];
            if (!CanDeselect(item, reason))
            {
                continue;
            }

            var mutation = _selectionState.PreviewRemove(item.Value, reason);
            if (!mutation.Changed)
            {
                continue;
            }

            _selectionState.Apply(mutation);
            OnDeselected(item, reason);
            changed = true;
        }

        if (changed)
        {
            Invalidate();
            OnSelectionChanged();
        }
    }

    internal BootstrapSelectHitTestInfo HitTestSelectionSurface(Point point)
    {
        var layout = CreateSelectionLayout();
        if (!layout.ClearBounds.IsEmpty && layout.ClearBounds.Contains(point))
        {
            return new BootstrapSelectHitTestInfo(BootstrapSelectHitTarget.Clear, null, layout.ClearBounds);
        }

        if (layout.ArrowBounds.Contains(point))
        {
            return new BootstrapSelectHitTestInfo(BootstrapSelectHitTarget.Arrow, null, layout.ArrowBounds);
        }

        for (var i = 0; i < layout.Chips.Count; i++)
        {
            var chip = layout.Chips[i];
            if (chip.RemoveBounds.Contains(point))
            {
                return new BootstrapSelectHitTestInfo(BootstrapSelectHitTarget.ChipRemove, chip.Item, chip.RemoveBounds);
            }

            if (chip.Bounds.Contains(point))
            {
                return new BootstrapSelectHitTestInfo(BootstrapSelectHitTarget.Chip, chip.Item, chip.Bounds);
            }
        }

        return ClientRectangle.Contains(point)
            ? new BootstrapSelectHitTestInfo(BootstrapSelectHitTarget.Content, null, ClientRectangle)
            : new BootstrapSelectHitTestInfo(BootstrapSelectHitTarget.None, null, Rectangle.Empty);
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left)
        {
            return;
        }

        Focus();
        var hit = HitTestSelectionSurface(e.Location);
        if (hit.Target == BootstrapSelectHitTarget.Clear)
        {
            ClearSelectionCore(BootstrapSelectChangeReason.Clear);
        }
        else if (hit.Target == BootstrapSelectHitTarget.ChipRemove && hit.Item is not null)
        {
            DeselectCore(hit.Item.Value, BootstrapSelectChangeReason.ChipRemove);
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Enabled && e.KeyCode == Keys.Delete && _allowClear && _selectionMode == BootstrapSelectMode.Single)
        {
            ClearSelectionCore(BootstrapSelectChangeReason.Clear);
            e.Handled = true;
        }
        else if (Enabled && e.KeyCode == Keys.Back && _selectionMode == BootstrapSelectMode.Multiple && _selectionState.SelectedItems.Count > 0)
        {
            DeselectCore(_selectionState.SelectedItems[_selectionState.SelectedItems.Count - 1].Value, BootstrapSelectChangeReason.ChipRemove);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var containsFocus = ContainsFocus || Focused;
        var metrics = BootstrapSelectRenderLogic.ResolveMetrics(
            ClientSize,
            theme.Metrics,
            dpi,
            _borderRadius,
            containsFocus);

        var graphics = e.Graphics;
        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(
                metrics.BorderBounds,
                new CornerRadius(metrics.Radius));
            using var background = new SolidBrush(
                Enabled ? theme.Colors.Surface : theme.Colors.SurfaceSecondary);
            using var pen = new Pen(
                BootstrapTextBoxRenderLogic.ResolveBorderColor(
                    theme.Colors,
                    _validationState,
                    containsFocus,
                    Enabled),
                metrics.BorderWidth);

            graphics.FillPath(background, path);
            graphics.DrawPath(pen, path);
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothing;
        }

        var layout = CreateSelectionLayout();
        if (_selectionMode == BootstrapSelectMode.Multiple)
        {
            for (var i = 0; i < layout.Chips.Count; i++)
            {
                var chip = layout.Chips[i];
                _renderer.DrawChip(e.Graphics, new BootstrapSelectChipRenderContext(chip.Item, chip.Bounds, chip.RemoveBounds, Enabled ? BootstrapSelectRenderState.None : BootstrapSelectRenderState.Disabled, dpi, theme, Font));
            }
        }
        else
        {
            var item = SelectedItem;
            _renderer.DrawSelection(e.Graphics, new BootstrapSelectSelectionRenderContext(item, item?.Text ?? _placeholder, item is null, layout.ContentBounds, dpi, theme, Font));
        }

        DrawClearAndArrow(e.Graphics, layout, theme, dpi);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private new bool CanSelect(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        var args = new BootstrapSelectItemCancelEventArgs(item, reason);
        Selecting?.Invoke(this, args);
        return !args.Cancel;
    }

    private bool CanDeselect(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        var args = new BootstrapSelectItemCancelEventArgs(item, reason);
        Deselecting?.Invoke(this, args);
        return !args.Cancel;
    }

    private void OnSelected(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        Selected?.Invoke(this, new BootstrapSelectItemEventArgs(item, reason));
    }

    private void OnDeselected(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        Deselected?.Invoke(this, new BootstrapSelectItemEventArgs(item, reason));
    }

    private void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private BootstrapSelectItem? FindLocalItemByValue(object value)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (_valueComparer.Equals(Items[i].Value, value))
            {
                return Items[i];
            }
        }

        return null;
    }

    private BootstrapSelectSelectionLayoutResult CreateSelectionLayout()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return BootstrapSelectSelectionLayout.Create(ClientSize, _selectionMode, _selectionState.SelectedItems, _allowClear && Enabled, RightToLeft == RightToLeft.Yes, dpi, _maximumSelectionRows);
    }

    private void DrawClearAndArrow(Graphics graphics, BootstrapSelectSelectionLayoutResult layout, BootstrapTheme theme, int dpi)
    {
        var color = Enabled ? theme.Colors.MutedText : theme.Colors.Disabled;
        if (!layout.ClearBounds.IsEmpty)
        {
            TextRenderer.DrawText(graphics, "×", Font, layout.ClearBounds, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        var centerX = layout.ArrowBounds.Left + layout.ArrowBounds.Width / 2;
        var centerY = layout.ArrowBounds.Top + layout.ArrowBounds.Height / 2;
        var half = Math.Max(3, DpiScaler.Scale(4, dpi));
        using var pen = new Pen(color, Math.Max(1f, DpiScaler.Scale(1f, dpi)));
        graphics.DrawLine(pen, centerX - half, centerY - 2, centerX, centerY + 2);
        graphics.DrawLine(pen, centerX, centerY + 2, centerX + half, centerY - 2);
    }

    private void OnItemsChanged()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            _selectionState.RefreshSelectedItem(Items[i]);
        }

        Invalidate();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        Invalidate();
    }
}
