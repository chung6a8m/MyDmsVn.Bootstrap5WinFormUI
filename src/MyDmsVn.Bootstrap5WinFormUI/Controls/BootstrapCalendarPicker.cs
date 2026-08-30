using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides a Bootstrap-themed shell that hosts a fresh <see cref="BootstrapCalendar"/> in a native dropdown.</summary>
[DefaultEvent(nameof(SelectionChanged))]
public sealed class BootstrapCalendarPicker : Control
{
    private static readonly IconDescriptor DropDownIcon = IconDescriptor.Framework(FrameworkIconGlyph.ChevronDown);
    private readonly BootstrapCalendarSelectionModel _selectionModel;
    private readonly BootstrapDropdown _dropdown;
    private readonly IIconRenderer _iconRenderer;
    private BootstrapCalendar? _activeCalendar;
    private DateTime? _lastDisplayMonth;
    private string _dateFormat = "d";
    private string _placeholderText = string.Empty;
    private BootstrapValidationState _validationState;
    private int _borderRadius = -1;
    private bool _isDropDownOpen;
    private bool _synchronizingCalendar;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

    /// <summary>Initializes a designer-safe picker with an empty single-date selection.</summary>
    public BootstrapCalendarPicker()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        AccessibleRole = AccessibleRole.DropList;
        _selectionModel = new BootstrapCalendarSelectionModel(
            BootstrapCalendarSelectionModel.MinimumSupportedDate,
            BootstrapCalendarSelectionModel.MaximumSupportedDate);
        _iconRenderer = BootstrapIconRenderer.CreateDefault();
        _dropdown = new BootstrapDropdown();
        _dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = CreateHostedCalendar
        });
        _dropdown.Opened += OnDropDownOpened;
        _dropdown.Closed += OnDropDownClosed;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        Size = new Size(240, DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.ControlHeight, GetCurrentDpi()));
    }

    /// <summary>Gets or sets the selection mode.</summary>
    [Category("Behavior")]
    [DefaultValue(BootstrapCalendarSelectionMode.Single)]
    public BootstrapCalendarSelectionMode SelectionMode
    {
        get => _selectionModel.Mode;
        set
        {
            var changed = _selectionModel.SetMode(value);
            if (changed) OnSelectionChanged(EventArgs.Empty);
            SynchronizeActiveCalendar();
            Invalidate();
        }
    }

    /// <summary>Gets or sets the inclusive minimum selectable date.</summary>
    [Category("Behavior")]
    public DateTime MinDate
    {
        get => _selectionModel.MinDate;
        set => SetBounds(value, MaxDate);
    }

    /// <summary>Gets or sets the inclusive maximum selectable date.</summary>
    [Category("Behavior")]
    public DateTime MaxDate
    {
        get => _selectionModel.MaxDate;
        set => SetBounds(MinDate, value);
    }

    /// <summary>Gets or sets the selected date in single-selection mode.</summary>
    [Category("Behavior")]
    [DefaultValue(null)]
    public DateTime? SelectedDate
    {
        get => _selectionModel.SelectedDate;
        set
        {
            var changed = _selectionModel.SetSelectedDate(value);
            if (changed) OnSelectionChanged(EventArgs.Empty);
            SynchronizeActiveCalendar();
            Invalidate();
        }
    }

    /// <summary>Gets the range start in range-selection mode.</summary>
    public DateTime? RangeStart => _selectionModel.RangeStart;

    /// <summary>Gets the range end in range-selection mode.</summary>
    public DateTime? RangeEnd => _selectionModel.RangeEnd;

    /// <summary>Gets a sorted immutable snapshot of selected dates in multiple-selection mode.</summary>
    [Browsable(false)]
    public IReadOnlyList<DateTime> SelectedDates => _selectionModel.SelectedDates;

    /// <summary>Gets or sets the .NET date format used by the collapsed summary.</summary>
    [Category("Appearance")]
    [DefaultValue("d")]
    public string DateFormat
    {
        get => _dateFormat;
        set
        {
            var candidate = value ?? throw new ArgumentNullException(nameof(value));
            _ = BootstrapCalendarSelectionModel.MinimumSupportedDate.ToString(candidate, CultureInfo.CurrentCulture);
            if (string.Equals(_dateFormat, candidate, StringComparison.Ordinal)) return;
            _dateFormat = candidate;
            Invalidate();
        }
    }

    /// <summary>Gets or sets text used when the picker has no selection.</summary>
    [Category("Appearance")]
    [DefaultValue("")]
    public string PlaceholderText
    {
        get => _placeholderText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_placeholderText, normalized, StringComparison.Ordinal)) return;
            _placeholderText = normalized;
            Invalidate();
        }
    }

    /// <summary>Gets or sets the validation state used to render the border.</summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapValidationState.None)]
    public BootstrapValidationState ValidationState
    {
        get => _validationState;
        set
        {
            BootstrapTextBoxRenderLogic.ValidateState(value);
            if (_validationState == value) return;
            _validationState = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets a logical corner radius, or -1 for the current theme radius.</summary>
    [Category("Appearance")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1) throw new ArgumentOutOfRangeException(nameof(value));
            if (_borderRadius == value) return;
            _borderRadius = value;
            Invalidate();
        }
    }

    /// <summary>Occurs when the effective logical selection changes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Occurs after the hosted calendar opens.</summary>
    public event EventHandler? Opened;

    /// <summary>Occurs after the hosted calendar closes.</summary>
    public event EventHandler? Closed;

    /// <summary>Sets an incomplete or complete range selection.</summary>
    public void SetRange(DateTime? start, DateTime? end)
    {
        var changed = _selectionModel.SetRange(start, end);
        if (changed) OnSelectionChanged(EventArgs.Empty);
        SynchronizeActiveCalendar();
        Invalidate();
    }

    /// <summary>Replaces the selected dates in multiple-selection mode.</summary>
    public void SetSelectedDates(IEnumerable<DateTime> dates)
    {
        var changed = _selectionModel.SetSelectedDates(dates);
        if (changed) OnSelectionChanged(EventArgs.Empty);
        SynchronizeActiveCalendar();
        Invalidate();
    }

    /// <summary>Clears the effective selection.</summary>
    public void ClearSelection()
    {
        var changed = _selectionModel.Clear();
        if (changed) OnSelectionChanged(EventArgs.Empty);
        SynchronizeActiveCalendar();
        Invalidate();
    }

    /// <summary>Shows the hosted calendar when the picker is enabled and collapsed.</summary>
    public void ShowDropDown()
    {
        ThrowIfDisposed();
        if (_isDropDownOpen || !Enabled) return;
        try
        {
            _dropdown.ShowFrom(this, _iconRenderer, this, new Point(0, Height));
        }
        catch
        {
            DetachActiveCalendar();
            throw;
        }
    }

    /// <summary>Closes the hosted calendar when it is open.</summary>
    public void CloseDropDown()
    {
        if (!IsDisposed) _dropdown.Close();
    }

    /// <inheritdoc />
    protected override AccessibleObject CreateAccessibilityInstance() => new BootstrapCalendarPickerAccessibleObject(this);

    /// <inheritdoc />
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        ToggleDropDown();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.F4 ||
            (e.KeyCode == Keys.Down && e.Alt))
        {
            ToggleDropDown();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled) CloseDropDown();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_settingThemeFont) { _useThemeFont = false; DisposeThemeFont(); }
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetCurrentDpi();
        var borderWidth = Math.Max(1f, DpiScaler.Scale((float)(ContainsFocus ? theme.Metrics.FocusBorderWidth : theme.Metrics.BorderWidth), dpi));
        var inset = borderWidth / 2f;
        var bounds = new RectangleF(inset, inset, Math.Max(0f, ClientSize.Width - borderWidth), Math.Max(0f, ClientSize.Height - borderWidth));
        if (bounds.Width <= 0f || bounds.Height <= 0f) return;
        var radius = DpiScaler.Scale((float)(_borderRadius < 0 ? theme.Metrics.Radius : _borderRadius), dpi);
        var border = BootstrapTextBoxRenderLogic.ResolveBorderColor(theme.Colors, _validationState, ContainsFocus, Enabled);
        using var path = RoundedPath.Create(bounds, new CornerRadius(radius));
        using var surface = new SolidBrush(Enabled ? theme.Colors.Surface : theme.Colors.SurfaceSecondary);
        using var pen = new Pen(border, borderWidth);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(surface, path);
        e.Graphics.DrawPath(pen, path);
        var padding = DpiScaler.Scale(theme.Metrics.SpacingSM, dpi);
        var iconSize = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var textBounds = new Rectangle(padding, 0, Math.Max(0, Width - (padding * 2) - iconSize), Height);
        var color = Enabled ? (GetSummary().Length == 0 ? theme.Colors.MutedText : theme.Colors.Text) : theme.Colors.Disabled;
        TextRenderer.DrawText(e.Graphics, GetSummary(), Font, textBounds, color, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        _iconRenderer.TryRender(e.Graphics, DropDownIcon, new Rectangle(Math.Max(padding, Width - padding - iconSize), Math.Max(0, (Height - iconSize) / 2), iconSize, iconSize), Enabled ? theme.Colors.MutedText : theme.Colors.Disabled);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DetachActiveCalendar();
            _dropdown.Opened -= OnDropDownOpened;
            _dropdown.Closed -= OnDropDownClosed;
            _dropdown.Dispose();
            if (_themeSubscribed) { BootstrapThemeManager.ThemeChanged -= OnThemeChanged; _themeSubscribed = false; }
            DisposeThemeFont();
        }
        base.Dispose(disposing);
    }

    private BootstrapCalendar CreateHostedCalendar()
    {
        BootstrapCalendar? calendar = null;
        var selectionAttached = false;
        var displayAttached = false;
        try
        {
            calendar = new BootstrapCalendar();
            calendar.SelectionMode = SelectionMode;
            calendar.MinDate = MinDate;
            calendar.MaxDate = MaxDate;
            ApplySelectionToCalendar(calendar);
            calendar.DisplayMonth = ResolveDisplayMonth();
            var metrics = BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, GetCurrentDpi(), calendar.BorderRadius);
            calendar.Size = BootstrapCalendarRenderLogic.CalculatePreferredSize(metrics);
            calendar.SelectionActivated += OnCalendarSelectionActivated;
            selectionAttached = true;
            calendar.DisplayMonthChanged += OnCalendarDisplayMonthChanged;
            displayAttached = true;
            _activeCalendar = calendar;
            return calendar;
        }
        catch
        {
            if (calendar is not null)
            {
                if (selectionAttached) calendar.SelectionActivated -= OnCalendarSelectionActivated;
                if (displayAttached) calendar.DisplayMonthChanged -= OnCalendarDisplayMonthChanged;
                calendar.Dispose();
            }
            throw;
        }
    }

    private void OnDropDownOpened(object? sender, EventArgs e)
    {
        _isDropDownOpen = true;
        Opened?.Invoke(this, EventArgs.Empty);
        if (_activeCalendar is not null && !_activeCalendar.IsDisposed) _activeCalendar.Focus();
        Invalidate();
    }

    private void OnDropDownClosed(object? sender, EventArgs e)
    {
        DetachActiveCalendar();
        _isDropDownOpen = false;
        Closed?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void OnCalendarSelectionActivated(object? sender, BootstrapCalendarSelectionActivatedEventArgs e)
    {
        if (_synchronizingCalendar || !ReferenceEquals(sender, _activeCalendar)) return;
        CopySelectionFromCalendar(_activeCalendar!);
        if (e.Changed) OnSelectionChanged(EventArgs.Empty);
        Invalidate();
        if (e.Completed && SelectionMode != BootstrapCalendarSelectionMode.Multiple) CloseDropDown();
    }

    private void OnCalendarDisplayMonthChanged(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _activeCalendar) && _activeCalendar is not null)
            _lastDisplayMonth = ClampMonth(_activeCalendar.DisplayMonth);
    }

    private void SynchronizeActiveCalendar()
    {
        var calendar = _activeCalendar;
        if (calendar is null || calendar.IsDisposed) return;
        _synchronizingCalendar = true;
        try
        {
            calendar.SelectionMode = SelectionMode;
            calendar.MinDate = MinDate;
            calendar.MaxDate = MaxDate;
            ApplySelectionToCalendar(calendar);
            calendar.DisplayMonth = ClampMonth(calendar.DisplayMonth);
        }
        finally { _synchronizingCalendar = false; }
    }

    private void ApplySelectionToCalendar(BootstrapCalendar calendar)
    {
        if (SelectionMode == BootstrapCalendarSelectionMode.Single) calendar.SelectedDate = SelectedDate;
        else if (SelectionMode == BootstrapCalendarSelectionMode.Range) calendar.SetRange(RangeStart, RangeEnd);
        else calendar.SetSelectedDates(SelectedDates);
    }

    private void CopySelectionFromCalendar(BootstrapCalendar calendar)
    {
        if (SelectionMode == BootstrapCalendarSelectionMode.Single) _selectionModel.SetSelectedDate(calendar.SelectedDate);
        else if (SelectionMode == BootstrapCalendarSelectionMode.Range) _selectionModel.SetRange(calendar.RangeStart, calendar.RangeEnd);
        else _selectionModel.SetSelectedDates(calendar.SelectedDates);
    }

    private DateTime ResolveDisplayMonth()
    {
        if (_lastDisplayMonth.HasValue) return ClampMonth(_lastDisplayMonth.Value);
        if (SelectedDate.HasValue) return ClampMonth(SelectedDate.Value);
        if (RangeStart.HasValue) return ClampMonth(RangeStart.Value);
        if (SelectedDates.Count > 0) return ClampMonth(SelectedDates[0]);
        return ClampMonth(DateTime.Today);
    }

    private void SetBounds(DateTime minDate, DateTime maxDate)
    {
        var changed = _selectionModel.SetBounds(minDate, maxDate);
        _lastDisplayMonth = _lastDisplayMonth.HasValue ? ClampMonth(_lastDisplayMonth.Value) : (DateTime?)null;
        if (changed) OnSelectionChanged(EventArgs.Empty);
        SynchronizeActiveCalendar();
        Invalidate();
    }

    private void ToggleDropDown()
    {
        if (!Enabled) return;
        if (_isDropDownOpen) CloseDropDown(); else ShowDropDown();
    }

    private void DetachActiveCalendar()
    {
        var calendar = _activeCalendar;
        _activeCalendar = null;
        if (calendar is null) return;
        calendar.SelectionActivated -= OnCalendarSelectionActivated;
        calendar.DisplayMonthChanged -= OnCalendarDisplayMonthChanged;
    }

    private string GetSummary()
    {
        if (SelectionMode == BootstrapCalendarSelectionMode.Single)
            return SelectedDate.HasValue ? Format(SelectedDate.Value) : PlaceholderText;
        if (SelectionMode == BootstrapCalendarSelectionMode.Range)
        {
            if (!RangeStart.HasValue) return PlaceholderText;
            return RangeEnd.HasValue ? Format(RangeStart.Value) + " – " + Format(RangeEnd.Value) : Format(RangeStart.Value) + " – …";
        }
        if (SelectedDates.Count == 0) return PlaceholderText;
        return SelectedDates.Count == 1 ? Format(SelectedDates[0]) : Format(SelectedDates[0]) + " (+" + (SelectedDates.Count - 1).ToString(CultureInfo.CurrentCulture) + ")";
    }

    private string Format(DateTime value) => value.ToString(DateFormat, CultureInfo.CurrentCulture);
    private int GetCurrentDpi() => DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
    private DateTime ClampMonth(DateTime value)
    {
        var month = new DateTime(value.Year, value.Month, 1);
        var min = new DateTime(MinDate.Year, MinDate.Month, 1);
        var max = new DateTime(MaxDate.Year, MaxDate.Month, 1);
        return month < min ? min : month > max ? max : month;
    }
    private void OnSelectionChanged(EventArgs e) => SelectionChanged?.Invoke(this, e);
    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) { if (IsDisposed) return; if (_useThemeFont) ApplyThemeFont(); Invalidate(); }
    private void ApplyThemeFont() { var t = BootstrapThemeManager.CurrentTheme.Typography.Body; var next = new Font(t.FontFamilyName, t.SizeInPoints, t.Style); var previous = _themeFont; _settingThemeFont = true; try { Font = next; } finally { _settingThemeFont = false; } _themeFont = next; previous?.Dispose(); }
    private void DisposeThemeFont() { var font = _themeFont; _themeFont = null; font?.Dispose(); }
    private void ThrowIfDisposed() { if (IsDisposed) throw new ObjectDisposedException(nameof(BootstrapCalendarPicker)); }

    private sealed class BootstrapCalendarPickerAccessibleObject : ControlAccessibleObject
    {
        private readonly BootstrapCalendarPicker _owner;
        internal BootstrapCalendarPickerAccessibleObject(BootstrapCalendarPicker owner) : base(owner) { _owner = owner; }
        public override AccessibleRole Role => AccessibleRole.DropList;
        public override string? Value { get => _owner.GetSummary(); set { } }
        public override string? DefaultAction => _owner._isDropDownOpen ? "Close calendar" : "Open calendar";
        public override AccessibleStates State
        {
            get
            {
                var state = base.State | AccessibleStates.Focusable;
                state |= _owner._isDropDownOpen ? AccessibleStates.Expanded : AccessibleStates.Collapsed;
                if (!_owner.Enabled) state |= AccessibleStates.Unavailable;
                return state;
            }
        }
        public override void DoDefaultAction() { _owner.ToggleDropDown(); }
    }
}
