using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectResultsViewTests
{
    [Test]
    public void VisibleRangeStartsAtScrollOffsetRow()
    {
        var layout = BootstrapSelectResultLayout.Create(1000, rowHeight: 32, viewportHeight: 160, scrollOffset: 320);

        Assert.Multiple((System.Action)(() =>
        {
            Assert.That(layout.FirstVisibleIndex, Is.EqualTo(10));
            Assert.That(layout.LastVisibleIndex, Is.EqualTo(14));
            Assert.That(layout.TotalHeight, Is.EqualTo(32000));
            Assert.That(layout.HitTestIndex(0), Is.EqualTo(10));
            Assert.That(layout.HitTestIndex(159), Is.EqualTo(14));
        }));
    }

    [Test]
    public void LayoutClampsScrollOffsetToLastViewport()
    {
        var layout = BootstrapSelectResultLayout.Create(4, rowHeight: 32, viewportHeight: 64, scrollOffset: 999);

        Assert.That(layout.ScrollOffset, Is.EqualTo(64));
        Assert.That(layout.FirstVisibleIndex, Is.EqualTo(2));
        Assert.That(layout.LastVisibleIndex, Is.EqualTo(3));
    }

    [Test]
    public void ConstantHeightLayoutSupportsFortyEightPixelRows()
    {
        var layout = BootstrapSelectResultLayout.Create(
            rowCount: 100,
            rowHeight: 48,
            viewportHeight: 144,
            scrollOffset: 96);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.FirstVisibleIndex, Is.EqualTo(2));
            Assert.That(layout.LastVisibleIndex, Is.EqualTo(4));
            Assert.That(layout.HitTestIndex(0), Is.EqualTo(2));
            Assert.That(layout.HitTestIndex(143), Is.EqualTo(4));
        }));
    }

    [Test]
    public void PreferredSizeAndPagingUseConfiguredEffectiveRowHeight()
    {
        using var view = new BootstrapSelectResultsView { Size = new Size(320, 144) };
        view.ApplyPresentation(
            new BootstrapSelectRenderer(),
            BootstrapThemeManager.CurrentTheme,
            96,
            logicalRowHeight: 48);
        view.SetResults(CreateItemResults(8));

        Assert.That(view.GetPreferredSize(new Size(320, 500)).Height, Is.EqualTo(8 * 48));
        Assert.That(view.Page(1), Is.True);
        Assert.That(view.HighlightedRow!.Item!.Value, Is.EqualTo(4));
        Assert.That(view.Page(-1), Is.True);
        Assert.That(view.HighlightedRow!.Item!.Value, Is.EqualTo(1));
    }

    [Test]
    public void WheelScrollingUsesRowsUntilExactEndClamp()
    {
        using var view = new BootstrapSelectResultsView { Size = new Size(320, 130) };
        view.ApplyPresentation(
            new BootstrapSelectRenderer(),
            BootstrapThemeManager.CurrentTheme,
            96,
            logicalRowHeight: 48);
        view.SetResults(CreateItemResults(10));

        view.SetScrollOffset(48);
        view.ScrollByWheelDelta(delta: -120, scrollLines: 2);
        Assert.That(view.ScrollOffset, Is.EqualTo(144));

        view.SetScrollOffset(300);
        view.ScrollByWheelDelta(delta: -120, scrollLines: 3);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(view.ScrollOffset, Is.EqualTo(350));
            Assert.That(view.ScrollOffset % 48, Is.Not.EqualTo(0));
        }));
    }

    [TestCase(96, 48)]
    [TestCase(144, 72)]
    [TestCase(192, 96)]
    public void ApplyPresentationScalesConfiguredLogicalRowHeight(int dpi, int expectedHeight)
    {
        using var view = new BootstrapSelectResultsView();

        view.ApplyPresentation(
            new BootstrapSelectRenderer(),
            BootstrapThemeManager.CurrentTheme,
            dpi,
            logicalRowHeight: 48);

        Assert.That(view.RowHeight, Is.EqualTo(expectedHeight));
    }

    [Test]
    public void PreserveNavigationKeepsHighlightedItemAndScrollWhenRowsAppend()
    {
        using var view = new BootstrapSelectResultsView { Size = new Size(320, 96) };
        view.SetResults(CreateGroupedResults(1, 10, "Group A"));
        Assert.That(view.MoveHighlight(7), Is.True);
        var highlightedValue = view.HighlightedRow!.Item!.Value;
        var scrollOffset = view.ScrollOffset;

        var appendedRows = CreateGroupedResults(1, 10, "Group A").Rows
            .Concat(CreateGroupedResults(11, 10, "Group B").Rows);
        view.SetResults(
            new BootstrapSelectResultSet(appendedRows),
            BootstrapSelectResultsUpdateMode.PreserveNavigation,
            EqualityComparer<object>.Default);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(view.HighlightedRow!.Item!.Value, Is.EqualTo(highlightedValue));
            Assert.That(view.ScrollOffset, Is.EqualTo(scrollOffset));
        }));
    }

    [Test]
    public void PreserveNavigationMatchesRefreshedItemByValueComparer()
    {
        using var view = new BootstrapSelectResultsView { Size = new Size(320, 96) };
        view.SetResults(new BootstrapSelectResultSet(new[]
        {
            BootstrapSelectResultRow.GroupHeader("Original"),
            BootstrapSelectResultRow.ItemRow(new BootstrapSelectItem("ABC", "Original"), false)
        }));

        var refreshed = new BootstrapSelectItem("abc", "Refreshed");
        view.SetResults(
            new BootstrapSelectResultSet(new[]
            {
                BootstrapSelectResultRow.GroupHeader("Changed"),
                BootstrapSelectResultRow.ItemRow(new BootstrapSelectItem("other", "Other"), false),
                BootstrapSelectResultRow.ItemRow(refreshed, false)
            }),
            BootstrapSelectResultsUpdateMode.PreserveNavigation,
            new OrdinalIgnoreCaseObjectComparer());

        Assert.That(view.HighlightedRow!.Item, Is.SameAs(refreshed));
    }

    [Test]
    public void ResetNavigationDiscardsPreviousViewportAndUsesFirstSelectable()
    {
        using var view = new BootstrapSelectResultsView { Size = new Size(320, 96) };
        view.SetResults(CreateGroupedResults(1, 12, "Old"));
        Assert.That(view.MoveHighlight(8), Is.True);
        Assert.That(view.ScrollOffset, Is.GreaterThan(0));

        view.SetResults(
            CreateGroupedResults(101, 6, "New"),
            BootstrapSelectResultsUpdateMode.ResetNavigation,
            EqualityComparer<object>.Default);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(view.HighlightedIndex, Is.EqualTo(1));
            Assert.That(view.HighlightedRow!.Item!.Value, Is.EqualTo(101));
            Assert.That(view.ScrollOffset, Is.Zero);
        }));
    }

    [Test]
    public void ResetNavigationRevealsPreferredSelectedItemOutsideInitialViewport()
    {
        using var view = new BootstrapSelectResultsView { Size = new Size(320, 64) };
        var rows = Enumerable.Range(1, 8)
            .Select(value => BootstrapSelectResultRow.ItemRow(
                new BootstrapSelectItem(value, "Item " + value),
                value == 7));

        view.SetResults(
            new BootstrapSelectResultSet(rows),
            BootstrapSelectResultsUpdateMode.ResetNavigation,
            EqualityComparer<object>.Default);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(view.HighlightedRow!.IsSelected, Is.True);
            Assert.That(view.HighlightedRow!.Item!.Value, Is.EqualTo(7));
            Assert.That(view.ScrollOffset, Is.GreaterThan(0));
        }));
    }

    private static BootstrapSelectResultSet CreateGroupedResults(int startValue, int count, string group)
    {
        var rows = new[] { BootstrapSelectResultRow.GroupHeader(group) }
            .Concat(Enumerable.Range(startValue, count).Select(value =>
                BootstrapSelectResultRow.ItemRow(new BootstrapSelectItem(value, "Item " + value), false)));
        return new BootstrapSelectResultSet(rows);
    }

    private static BootstrapSelectResultSet CreateItemResults(int count)
    {
        return new BootstrapSelectResultSet(
            Enumerable.Range(1, count).Select(value =>
                BootstrapSelectResultRow.ItemRow(
                    new BootstrapSelectItem(value, "Item " + value),
                    false)));
    }

    private sealed class OrdinalIgnoreCaseObjectComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y)
        {
            return string.Equals(x as string, y as string, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(object obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode((string)obj);
        }
    }
}
