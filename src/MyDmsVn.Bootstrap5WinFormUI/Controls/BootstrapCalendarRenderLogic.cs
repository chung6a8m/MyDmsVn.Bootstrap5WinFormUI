using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapCalendarMetrics
{
    public BootstrapCalendarMetrics(int outerPadding, int cellGap, int headerHeight, int weekdayHeight, int dayRowHeight, int preferredDayCellWidth, int borderWidth, int focusBorderWidth, int radius) { OuterPadding = outerPadding; CellGap = cellGap; HeaderHeight = headerHeight; WeekdayHeight = weekdayHeight; DayRowHeight = dayRowHeight; PreferredDayCellWidth = preferredDayCellWidth; BorderWidth = borderWidth; FocusBorderWidth = focusBorderWidth; Radius = radius; }
    public int OuterPadding { get; } public int CellGap { get; } public int HeaderHeight { get; } public int WeekdayHeight { get; } public int DayRowHeight { get; } public int PreferredDayCellWidth { get; } public int BorderWidth { get; } public int FocusBorderWidth { get; } public int Radius { get; }
}

internal readonly struct BootstrapCalendarDayCell
{
    public BootstrapCalendarDayCell(int index, DateTime date, Rectangle bounds, bool isCurrentMonth, bool isEnabled, bool isToday) { Index = index; Date = date; Bounds = bounds; IsCurrentMonth = isCurrentMonth; IsEnabled = isEnabled; IsToday = isToday; }
    public int Index { get; } public DateTime Date { get; } public Rectangle Bounds { get; } public bool IsCurrentMonth { get; } public bool IsEnabled { get; } public bool IsToday { get; }
}

internal readonly struct BootstrapCalendarLayout
{
    public BootstrapCalendarLayout(Rectangle headerBounds, Rectangle previousButtonBounds, Rectangle monthTitleBounds, Rectangle nextButtonBounds, Rectangle[] weekdayBounds, BootstrapCalendarDayCell[] dayCells) { HeaderBounds = headerBounds; PreviousButtonBounds = previousButtonBounds; MonthTitleBounds = monthTitleBounds; NextButtonBounds = nextButtonBounds; WeekdayBounds = weekdayBounds; DayCells = dayCells; }
    public Rectangle HeaderBounds { get; } public Rectangle PreviousButtonBounds { get; } public Rectangle MonthTitleBounds { get; } public Rectangle NextButtonBounds { get; } public Rectangle[] WeekdayBounds { get; } public BootstrapCalendarDayCell[] DayCells { get; }
}

internal static class BootstrapCalendarRenderLogic
{
    internal static BootstrapCalendarMetrics ResolveMetrics(BootstrapThemeMetrics themeMetrics, int dpi, int borderRadius)
    {
        if (themeMetrics is null) throw new ArgumentNullException(nameof(themeMetrics));
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        if (borderRadius < -1) throw new ArgumentOutOfRangeException(nameof(borderRadius));
        var radius = borderRadius >= 0 ? borderRadius : themeMetrics.Radius;
        return new BootstrapCalendarMetrics(DpiScaler.Scale(themeMetrics.SpacingSM, dpi), DpiScaler.Scale(themeMetrics.SpacingXS, dpi), DpiScaler.Scale(themeMetrics.ControlHeight, dpi), DpiScaler.Scale(themeMetrics.ControlHeightSmall, dpi), DpiScaler.Scale(themeMetrics.ControlHeight, dpi), DpiScaler.Scale(themeMetrics.ControlHeight, dpi), DpiScaler.Scale(themeMetrics.BorderWidth, dpi), DpiScaler.Scale(themeMetrics.FocusBorderWidth, dpi), DpiScaler.Scale(radius, dpi));
    }

    internal static Size CalculatePreferredSize(BootstrapCalendarMetrics metrics) => new Size(2 * metrics.OuterPadding + 7 * metrics.PreferredDayCellWidth + 6 * metrics.CellGap, 2 * metrics.OuterPadding + metrics.HeaderHeight + metrics.WeekdayHeight + 6 * metrics.DayRowHeight + 7 * metrics.CellGap);

    internal static BootstrapCalendarLayout CalculateLayout(Size clientSize, BootstrapCalendarMetrics metrics, DateTime displayMonth, DayOfWeek firstDayOfWeek, DateTime minDate, DateTime maxDate, DateTime today)
    {
        var width = Math.Max(0, clientSize.Width); var height = Math.Max(0, clientSize.Height); var outer = Math.Min(Math.Max(0, metrics.OuterPadding), Math.Min(width / 2, height / 2)); var gap = Math.Max(0, metrics.CellGap);
        var contentWidth = Math.Max(0, width - 2 * outer); var header = new Rectangle(outer, outer, contentWidth, Math.Min(Math.Max(0, metrics.HeaderHeight), Math.Max(0, height - outer)));
        var buttonWidth = contentWidth / 7; var previous = new Rectangle(header.Left, header.Top, buttonWidth, header.Height); var next = new Rectangle(header.Right - buttonWidth, header.Top, buttonWidth, header.Height); var title = new Rectangle(previous.Right, header.Top, Math.Max(0, header.Width - 2 * buttonWidth), header.Height);
        var weekdayTop = Math.Min(height, header.Bottom + gap); var weekdayHeight = Math.Min(Math.Max(0, metrics.WeekdayHeight), Math.Max(0, height - weekdayTop - outer)); var weekday = MakeColumns(outer, weekdayTop, contentWidth, weekdayHeight, gap, 7);
        var daysTop = Math.Min(height, weekdayTop + weekdayHeight + gap); var availableDayHeight = Math.Max(0, height - daysTop - outer); var dayRows = MakeGrid(outer, daysTop, contentWidth, availableDayHeight, gap);
        var cells = new BootstrapCalendarDayCell[42]; var month = new DateTime(displayMonth.Year, displayMonth.Month, 1); var offset = ((int)month.DayOfWeek - (int)firstDayOfWeek + 7) % 7; var start = month.AddDays(-offset);
        for (var i = 0; i < cells.Length; i++) { var row = i / 7; var col = i % 7; var date = start.AddDays(i); cells[i] = new BootstrapCalendarDayCell(i, date, dayRows[row][col], date.Month == month.Month && date.Year == month.Year, date.Date >= minDate.Date && date.Date <= maxDate.Date, date.Date == today.Date); }
        return new BootstrapCalendarLayout(header, previous, title, next, weekday, cells);
    }

    internal static int HitTestDay(Point location, BootstrapCalendarLayout layout) { for (var i = 0; i < layout.DayCells.Length; i++) if (layout.DayCells[i].Bounds.Contains(location)) return i; return -1; }

    internal static DateTime MoveByMonth(DateTime date, int months) { var originalDay = date.Day; var target = date.AddMonths(months); return new DateTime(target.Year, target.Month, Math.Min(originalDay, DateTime.DaysInMonth(target.Year, target.Month))); }
    internal static DateTime MoveToWeekBoundary(DateTime date, DayOfWeek firstDayOfWeek, bool endOfWeek) { var offset = ((int)date.DayOfWeek - (int)firstDayOfWeek + 7) % 7; return date.Date.AddDays(endOfWeek ? 6 - offset : -offset); }

    private static Rectangle[] MakeColumns(int left, int top, int width, int height, int gap, int count)
    {
        var result = new Rectangle[count]; var effectiveGap = width >= gap * (count - 1) ? gap : 0; var usable = Math.Max(0, width - effectiveGap * (count - 1)); for (var i = 0; i < count; i++) { var x1 = left + (int)Math.Floor((double)usable * i / count) + effectiveGap * i; var x2 = left + (int)Math.Floor((double)usable * (i + 1) / count) + effectiveGap * i; result[i] = new Rectangle(x1, top, Math.Max(0, x2 - x1), Math.Max(0, height)); } return result;
    }

    private static Rectangle[][] MakeGrid(int left, int top, int width, int height, int gap)
    {
        var rows = new Rectangle[6][]; var effectiveGap = height >= gap * 5 ? gap : 0; var usable = Math.Max(0, height - effectiveGap * 5); var total = usable + effectiveGap * 5;
        for (var row = 0; row < 6; row++) { var y1 = top + (int)Math.Floor((double)usable * row / 6) + effectiveGap * row; var y2 = top + (int)Math.Floor((double)usable * (row + 1) / 6) + effectiveGap * row; rows[row] = MakeColumns(left, y1, width, Math.Max(0, y2 - y1), gap, 7); }
        return rows;
    }
}
