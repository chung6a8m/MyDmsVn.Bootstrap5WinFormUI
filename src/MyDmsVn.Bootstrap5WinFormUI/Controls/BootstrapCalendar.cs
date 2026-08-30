using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

[Flags]
internal enum BootstrapCalendarDayRenderState
{
    None = 0,
    AdjacentMonth = 1,
    Disabled = 2,
    Today = 4,
    Selected = 8,
    RangeInterior = 16,
    Preview = 32
}

internal readonly struct BootstrapCalendarDayOutlineMetrics
{
    public BootstrapCalendarDayOutlineMetrics(float selectionWidth, float focusWidth)
    {
        SelectionWidth = selectionWidth;
        FocusWidth = focusWidth;
    }

    public float SelectionWidth { get; }

    public float FocusWidth { get; }
}

/// <summary>
/// Provides a fully owner-drawn Bootstrap-inspired month calendar with single, range, and multiple-date state.
/// </summary>
[DefaultEvent(nameof(SelectionChanged))]
public class BootstrapCalendar : Control
{
    private readonly BootstrapCalendarSelectionModel _selectionModel;
    private DateTime _displayMonth;
    private DateTime _focusedDate;
    private int _borderRadius = -1;
    private BootstrapCalendarLayout _layout;
    private bool _layoutValid;
    private Size _layoutSize;
    private int _layoutDpi;
    private DateTime _layoutMonth;
    private DayOfWeek _layoutFirstDay;
    private DateTime _layoutMinDate;
    private DateTime _layoutMaxDate;
    private int _layoutRadius;
    private Font? _layoutFont;
    private BootstrapThemeMetrics? _layoutThemeMetrics;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;
    private bool _themeSubscribed;

    /// <summary>Initializes a designer-safe calendar using the current theme and supported date domain.</summary>
    public BootstrapCalendar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        AccessibleRole = AccessibleRole.Table;
        _selectionModel = new BootstrapCalendarSelectionModel(
            BootstrapCalendarSelectionModel.MinimumSupportedDate,
            BootstrapCalendarSelectionModel.MaximumSupportedDate);
        _focusedDate = Clamp(DateTime.Today.Date, MinDate, MaxDate);
        _displayMonth = ClampMonth(_focusedDate, MinDate, MaxDate);
        ApplyThemeFont();
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        Size = GetPreferredSize(Size.Empty);
    }

    /// <summary>Gets or sets how dates are selected.</summary>
    [Category("Behavior")]
    [DefaultValue(BootstrapCalendarSelectionMode.Single)]
    public BootstrapCalendarSelectionMode SelectionMode
    {
        get => _selectionModel.Mode;
        set
        {
            var selectionChanged = _selectionModel.SetMode(value);
            if (selectionChanged) OnSelectionChanged(EventArgs.Empty);
            Invalidate();
        }
    }

    /// <summary>Gets or sets the displayed month. The value is normalized to its first day and clamped to the bounds.</summary>
    [Category("Behavior")]
    public DateTime DisplayMonth
    {
        get => _displayMonth;
        set
        {
            var target = ClampMonth(value.Date, MinDate, MaxDate);
            if (target == _displayMonth) return;
            var oldFocus = _focusedDate;
            _displayMonth = target;
            if (oldFocus.Year != target.Year || oldFocus.Month != target.Month)
            {
                _focusedDate = Clamp(new DateTime(target.Year, target.Month, Math.Min(oldFocus.Day, DateTime.DaysInMonth(target.Year, target.Month))), MinDate, MaxDate);
            }
            InvalidateLayout();
            OnDisplayMonthChanged(EventArgs.Empty);
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
            if (_selectionModel.SelectedDate.HasValue) _focusedDate = _selectionModel.SelectedDate.Value;
            if (changed) OnSelectionChanged(EventArgs.Empty);
        }
    }

    /// <summary>Gets the inclusive range start in range-selection mode.</summary>
    public DateTime? RangeStart => _selectionModel.RangeStart;

    /// <summary>Gets the inclusive range end in range-selection mode.</summary>
    public DateTime? RangeEnd => _selectionModel.RangeEnd;

    /// <summary>Gets the sorted immutable selected-date snapshot in multiple-selection mode.</summary>
    [Browsable(false)]
    public IReadOnlyList<DateTime> SelectedDates => _selectionModel.SelectedDates;

    /// <summary>Gets or sets the logical corner radius, or -1 to use the current theme radius.</summary>
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
            InvalidateLayout();
        }
    }

    /// <summary>Occurs after the effective logical selection changes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Occurs after the effective displayed month changes.</summary>
    public event EventHandler? DisplayMonthChanged;

    /// <summary>Sets an incomplete or complete selection in range-selection mode.</summary>
    public void SetRange(DateTime? start, DateTime? end)
    {
        var changed = _selectionModel.SetRange(start, end);
        var anchor = _selectionModel.RangeEnd ?? _selectionModel.RangeStart;
        if (anchor.HasValue) _focusedDate = anchor.Value;
        if (changed) OnSelectionChanged(EventArgs.Empty);
    }

    /// <summary>Replaces the selected dates in multiple-selection mode.</summary>
    public void SetSelectedDates(IEnumerable<DateTime> dates)
    {
        var changed = _selectionModel.SetSelectedDates(dates);
        if (_selectionModel.SelectedDates.Count != 0) _focusedDate = _selectionModel.SelectedDates[0];
        if (changed) OnSelectionChanged(EventArgs.Empty);
    }

    /// <summary>Clears the effective selection while preserving the private focus anchor.</summary>
    public void ClearSelection()
    {
        if (_selectionModel.Clear()) OnSelectionChanged(EventArgs.Empty);
    }

    /// <summary>Shows the previous month when it intersects the configured date bounds.</summary>
    public void ShowPreviousMonth() => DisplayMonth = BootstrapCalendarRenderLogic.MoveByMonth(_displayMonth, -1);

    /// <summary>Shows the next month when it intersects the configured date bounds.</summary>
    public void ShowNextMonth() => DisplayMonth = BootstrapCalendarRenderLogic.MoveByMonth(_displayMonth, 1);

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return BootstrapCalendarRenderLogic.CalculatePreferredSize(
            BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, dpi, _borderRadius));
    }

    internal DateTime FocusedDate => _focusedDate;

    internal int LayoutBuildCount { get; private set; }

    internal static BootstrapCalendarDayOutlineMetrics ResolveDayOutlineMetrics(BootstrapThemeMetrics themeMetrics, int dpi)
    {
        if (themeMetrics is null) throw new ArgumentNullException(nameof(themeMetrics));
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        return new BootstrapCalendarDayOutlineMetrics(
            DpiScaler.Scale((float)themeMetrics.BorderWidth, dpi),
            DpiScaler.Scale((float)themeMetrics.FocusBorderWidth, dpi));
    }

    internal static BootstrapCalendarDayRenderState ClassifyDay(DateTime date, bool currentMonth, bool enabled, bool today,
        BootstrapCalendarSelectionMode mode, DateTime? selectedDate, DateTime? rangeStart, DateTime? rangeEnd,
        IReadOnlyList<DateTime> selectedDates, DateTime? previewDate)
    {
        var state = BootstrapCalendarDayRenderState.None;
        if (!currentMonth) state |= BootstrapCalendarDayRenderState.AdjacentMonth;
        if (!enabled) state |= BootstrapCalendarDayRenderState.Disabled;
        if (today) state |= BootstrapCalendarDayRenderState.Today;
        var normalized = date.Date;
        if ((mode == BootstrapCalendarSelectionMode.Single && selectedDate == normalized) ||
            (mode == BootstrapCalendarSelectionMode.Range && (rangeStart == normalized || rangeEnd == normalized)) ||
            (mode == BootstrapCalendarSelectionMode.Multiple && selectedDates.Contains(normalized)))
            state |= BootstrapCalendarDayRenderState.Selected;
        if (mode == BootstrapCalendarSelectionMode.Range && rangeStart.HasValue && rangeEnd.HasValue && normalized > rangeStart.Value && normalized < rangeEnd.Value)
            state |= BootstrapCalendarDayRenderState.RangeInterior;
        if (mode == BootstrapCalendarSelectionMode.Range && rangeStart.HasValue && !rangeEnd.HasValue && previewDate.HasValue &&
            normalized >= (rangeStart < previewDate ? rangeStart.Value : previewDate.Value) && normalized <= (rangeStart > previewDate ? rangeStart.Value : previewDate.Value))
            state |= BootstrapCalendarDayRenderState.Preview;
        return state;
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var theme = BootstrapThemeManager.CurrentTheme;
        var layout = GetLayout();
        var graphics = e.Graphics;
        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            PaintOuter(graphics, theme);
            PaintHeader(graphics, theme, layout);
            PaintWeekdays(graphics, theme, layout);
            PaintDays(graphics, theme, layout);
        }
        finally { graphics.SmoothingMode = previousSmoothing; }
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_settingThemeFont) { _useThemeFont = false; DisposeThemeFont(); }
        InvalidateLayout();
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); InvalidateLayout(); }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); InvalidateLayout(); }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed) { BootstrapThemeManager.ThemeChanged -= OnThemeChanged; _themeSubscribed = false; }
            DisposeThemeFont();
        }
        base.Dispose(disposing);
    }

    private void SetBounds(DateTime minDate, DateTime maxDate)
    {
        var oldMinDate = MinDate;
        var oldMaxDate = MaxDate;
        var oldMonth = _displayMonth;
        var selectionChanged = _selectionModel.SetBounds(minDate, maxDate);
        if (MinDate == oldMinDate && MaxDate == oldMaxDate) return;
        _focusedDate = Clamp(_focusedDate, MinDate, MaxDate);
        _displayMonth = ClampMonth(_displayMonth, MinDate, MaxDate);
        InvalidateLayout();
        if (selectionChanged) OnSelectionChanged(EventArgs.Empty);
        if (_displayMonth != oldMonth) OnDisplayMonthChanged(EventArgs.Empty);
    }

    private BootstrapCalendarLayout GetLayout()
    {
        return ResolveLayout(DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi);
    }

    internal BootstrapCalendarLayout ResolveLayout(int dpi)
    {
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        var firstDay = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var themeMetrics = BootstrapThemeManager.CurrentTheme.Metrics;
        if (!_layoutValid || _layoutSize != ClientSize || _layoutDpi != dpi || _layoutMonth != _displayMonth ||
            _layoutFirstDay != firstDay || _layoutMinDate != MinDate || _layoutMaxDate != MaxDate ||
            _layoutRadius != _borderRadius || !ReferenceEquals(_layoutFont, Font) ||
            !ReferenceEquals(_layoutThemeMetrics, themeMetrics))
        {
            var metrics = BootstrapCalendarRenderLogic.ResolveMetrics(themeMetrics, dpi, _borderRadius);
            _layout = BootstrapCalendarRenderLogic.CalculateLayout(ClientSize, metrics, _displayMonth, firstDay, MinDate, MaxDate, DateTime.Today);
            _layoutSize = ClientSize; _layoutDpi = dpi; _layoutMonth = _displayMonth; _layoutFirstDay = firstDay;
            _layoutMinDate = MinDate; _layoutMaxDate = MaxDate; _layoutRadius = _borderRadius; _layoutFont = Font;
            _layoutThemeMetrics = themeMetrics;
            _layoutValid = true; LayoutBuildCount++;
        }
        return _layout;
    }

    private void PaintOuter(Graphics graphics, BootstrapTheme theme)
    {
        var metrics = BootstrapCalendarRenderLogic.ResolveMetrics(theme.Metrics, DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi, _borderRadius);
        var width = Math.Max(1, metrics.BorderWidth);
        var bounds = new RectangleF(width / 2f, width / 2f, Math.Max(0, ClientSize.Width - width), Math.Max(0, ClientSize.Height - width));
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var path = RoundedPath.Create(bounds, new CornerRadius(metrics.Radius));
        using var brush = new SolidBrush(theme.Colors.Surface);
        using var pen = new Pen(theme.Colors.Border, width);
        graphics.FillPath(brush, path); graphics.DrawPath(pen, path);
    }

    private void PaintHeader(Graphics graphics, BootstrapTheme theme, BootstrapCalendarLayout layout)
    {
        var muted = Enabled ? theme.Colors.MutedText : theme.Colors.Disabled;
        var minimumMonth = new DateTime(MinDate.Year, MinDate.Month, 1);
        var maximumMonth = new DateTime(MaxDate.Year, MaxDate.Month, 1);
        DrawCentered(graphics, "‹", layout.PreviousButtonBounds, Enabled && _displayMonth > minimumMonth ? muted : theme.Colors.Disabled);
        DrawCentered(graphics, _displayMonth.ToString("Y", CultureInfo.CurrentCulture), layout.MonthTitleBounds, Enabled ? theme.Colors.Text : theme.Colors.Disabled);
        DrawCentered(graphics, "›", layout.NextButtonBounds, Enabled && _displayMonth < maximumMonth ? muted : theme.Colors.Disabled);
    }

    private void PaintWeekdays(Graphics graphics, BootstrapTheme theme, BootstrapCalendarLayout layout)
    {
        var names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
        var first = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        for (var i = 0; i < 7; i++) DrawCentered(graphics, names[(first + i) % 7], layout.WeekdayBounds[i], Enabled ? theme.Colors.MutedText : theme.Colors.Disabled);
    }

    private void PaintDays(Graphics graphics, BootstrapTheme theme, BootstrapCalendarLayout layout)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var outlines = ResolveDayOutlineMetrics(theme.Metrics, dpi);
        foreach (var cell in layout.DayCells)
        {
            var state = ClassifyDay(cell.Date, cell.IsCurrentMonth, cell.IsEnabled && Enabled, cell.IsToday,
                SelectionMode, SelectedDate, RangeStart, RangeEnd, SelectedDates, null);
            if ((state & BootstrapCalendarDayRenderState.RangeInterior) != 0) FillCell(graphics, cell.Bounds, theme.Colors.SurfaceSecondary);
            if ((state & BootstrapCalendarDayRenderState.Preview) != 0) FillCell(graphics, cell.Bounds, theme.Colors.Hover);
            if ((state & BootstrapCalendarDayRenderState.Selected) != 0)
            {
                FillCell(graphics, cell.Bounds, theme.Colors.Active);
                DrawCellOutline(graphics, cell.Bounds, theme.Colors.Primary, outlines.SelectionWidth);
            }
            if ((state & BootstrapCalendarDayRenderState.Today) != 0) DrawCellOutline(graphics, cell.Bounds, theme.Colors.Primary, outlines.SelectionWidth);
            var color = (state & BootstrapCalendarDayRenderState.Disabled) != 0 || (state & BootstrapCalendarDayRenderState.AdjacentMonth) != 0 ? theme.Colors.MutedText : theme.Colors.Text;
            DrawCentered(graphics, cell.Date.Day.ToString(CultureInfo.CurrentCulture), cell.Bounds, color);
            if (Focused && ShowFocusCues && cell.Date == _focusedDate) DrawCellOutline(graphics, cell.Bounds, theme.Colors.Focus, outlines.FocusWidth);
        }
    }

    private static void FillCell(Graphics graphics, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var brush = new SolidBrush(color); graphics.FillRectangle(brush, bounds);
    }

    private static void DrawCellOutline(Graphics graphics, Rectangle bounds, Color color, float width)
    {
        if (bounds.Width <= 1 || bounds.Height <= 1 || width <= 0f) return;
        using var pen = new Pen(color, width); graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
    }

    private void DrawCentered(Graphics graphics, string text, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        TextRenderer.DrawText(graphics, text, Font, bounds, color, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void OnSelectionChanged(EventArgs e) { Invalidate(); SelectionChanged?.Invoke(this, e); }
    private void OnDisplayMonthChanged(EventArgs e) { Invalidate(); DisplayMonthChanged?.Invoke(this, e); }
    private void InvalidateLayout() { _layoutValid = false; Invalidate(); }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed) return;
        if (_useThemeFont) ApplyThemeFont();
        InvalidateLayout();
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var old = _themeFont; _themeFont = next; _settingThemeFont = true;
        try { Font = next; } finally { _settingThemeFont = false; }
        old?.Dispose();
    }

    private void DisposeThemeFont() { var font = _themeFont; _themeFont = null; font?.Dispose(); }

    private static DateTime Clamp(DateTime date, DateTime minDate, DateTime maxDate) => date < minDate ? minDate : date > maxDate ? maxDate : date;
    private static DateTime ClampMonth(DateTime date, DateTime minDate, DateTime maxDate)
    {
        var month = new DateTime(date.Year, date.Month, 1);
        var minimum = new DateTime(minDate.Year, minDate.Month, 1);
        var maximum = new DateTime(maxDate.Year, maxDate.Month, 1);
        return month < minimum ? minimum : month > maximum ? maximum : month;
    }
}
