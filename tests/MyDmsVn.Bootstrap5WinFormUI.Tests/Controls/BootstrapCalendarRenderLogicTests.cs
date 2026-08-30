using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapCalendarRenderLogicTests
{
    [TestCase(96, 8, 4, 32, 28, 32, 1, 2, 6)]
    [TestCase(120, 10, 5, 40, 35, 40, 1, 3, 8)]
    [TestCase(144, 12, 6, 48, 42, 48, 2, 3, 9)]
    [TestCase(168, 14, 7, 56, 49, 56, 2, 4, 11)]
    [TestCase(192, 16, 8, 64, 56, 64, 2, 4, 12)]
    public void MetricsScale(int dpi, int outer, int gap, int header, int weekday, int cell, int border, int focus, int radius)
    {
        var m = BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi, -1);
        Assert.That(m.OuterPadding, Is.EqualTo(outer)); Assert.That(m.CellGap, Is.EqualTo(gap));
        Assert.That(m.HeaderHeight, Is.EqualTo(header)); Assert.That(m.WeekdayHeight, Is.EqualTo(weekday));
        Assert.That(m.DayRowHeight, Is.EqualTo(cell)); Assert.That(m.PreferredDayCellWidth, Is.EqualTo(cell));
        Assert.That(m.BorderWidth, Is.EqualTo(border)); Assert.That(m.FocusBorderWidth, Is.EqualTo(focus)); Assert.That(m.Radius, Is.EqualTo(radius));
    }

    [Test] public void MetricsRejectInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapCalendarRenderLogic.ResolveMetrics(null!, 96, -1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 0, -1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -2)));
    }

    [Test] public void PreferredSizeUsesCalendarFormula()
    {
        var m = BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        Assert.That(BootstrapCalendarRenderLogic.CalculatePreferredSize(m), Is.EqualTo(new Size(264, 296)));
    }

    [TestCase(DayOfWeek.Sunday, 2026, 9, 2026, 8, 30)]
    [TestCase(DayOfWeek.Monday, 2026, 9, 2026, 8, 31)]
    public void ProjectionStartsAtCorrectWeekBoundary(DayOfWeek first, int year, int month, int expectedYear, int expectedMonth, int expectedDay)
    {
        var layout = Layout(new DateTime(year, month, 1), first);
        Assert.That(layout.DayCells, Has.Length.EqualTo(42));
        Assert.That(layout.DayCells[0].Date, Is.EqualTo(new DateTime(expectedYear, expectedMonth, expectedDay)));
        Assert.That(layout.DayCells[41].Date, Is.EqualTo(layout.DayCells[0].Date.AddDays(41)));
        Assert.That(layout.DayCells.Count(c => c.IsCurrentMonth), Is.EqualTo(DateTime.DaysInMonth(year, month)));
    }

    [Test] public void LeapMonthAndYearBoundaryRemainConsecutive() { AssertProjection(new DateTime(2028, 2, 1), DayOfWeek.Monday); AssertProjection(new DateTime(2026, 12, 1), DayOfWeek.Sunday); }

    [Test] public void SafeDomainBoundariesDoNotOverflow()
    {
        AssertProjection(DateTimePicker.MinimumDateTime.Date, DayOfWeek.Sunday);
        AssertProjection(DateTimePicker.MaximumDateTime.Date, DayOfWeek.Sunday);
    }

    [Test] public void LayoutAndHitTestAreContainedForTinyAndZeroSizes()
    {
        var m = BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        foreach (var size in new[] { new Size(300, 296), new Size(3, 2), Size.Empty })
        {
            var l = BootstrapCalendarRenderLogic.CalculateLayout(size, m, new DateTime(2026, 9, 1), DayOfWeek.Sunday, DateTimePicker.MinimumDateTime.Date, DateTimePicker.MaximumDateTime.Date, new DateTime(2026, 9, 15));
            Assert.That(l.WeekdayBounds, Has.Length.EqualTo(7)); Assert.That(l.DayCells, Has.Length.EqualTo(42));
            Assert.That(l.WeekdayBounds.All(r => r.Width >= 0 && r.Height >= 0), Is.True);
            Assert.That(l.DayCells.All(c => c.Bounds.Width >= 0 && c.Bounds.Height >= 0), Is.True);
            Assert.That(BootstrapCalendarRenderLogic.HitTestDay(new Point(1, 1), l), Is.EqualTo(-1));
        }
    }

    [Test] public void HitTestFindsRepresentativeCellAndRejectsGap()
    {
        var l = Layout(new DateTime(2026, 9, 1), DayOfWeek.Sunday);
        Assert.That(BootstrapCalendarRenderLogic.HitTestDay(new Point(l.DayCells[0].Bounds.Left + 1, l.DayCells[0].Bounds.Top + 1), l), Is.EqualTo(0));
        Assert.That(BootstrapCalendarRenderLogic.HitTestDay(new Point(0, 0), l), Is.EqualTo(-1));
    }

    [TestCase(2025, 1, 31, 1, 2025, 2, 28)]
    [TestCase(2028, 1, 31, 1, 2028, 2, 29)]
    [TestCase(2026, 3, 31, -1, 2026, 2, 28)]
    public void MoveByMonthClampsDay(int y, int m, int d, int delta, int ey, int em, int ed) => Assert.That(BootstrapCalendarRenderLogic.MoveByMonth(new DateTime(y, m, d), delta), Is.EqualTo(new DateTime(ey, em, ed)));

    [Test] public void MoveToWeekBoundaryUsesFirstDay() { var d = new DateTime(2026, 9, 16); Assert.That(BootstrapCalendarRenderLogic.MoveToWeekBoundary(d, DayOfWeek.Sunday, false).DayOfWeek, Is.EqualTo(DayOfWeek.Sunday)); Assert.That(BootstrapCalendarRenderLogic.MoveToWeekBoundary(d, DayOfWeek.Monday, true).DayOfWeek, Is.EqualTo(DayOfWeek.Sunday)); }

    private static BootstrapCalendarLayout Layout(DateTime month, DayOfWeek first) => BootstrapCalendarRenderLogic.CalculateLayout(new Size(300, 296), BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1), month, first, DateTimePicker.MinimumDateTime.Date, DateTimePicker.MaximumDateTime.Date, DateTime.Today);
    private static void AssertProjection(DateTime month, DayOfWeek first) { var l = Layout(month, first); Assert.That(l.DayCells, Has.Length.EqualTo(42)); for (var i = 1; i < 42; i++) Assert.That(l.DayCells[i].Date, Is.EqualTo(l.DayCells[i - 1].Date.AddDays(1))); }
}
