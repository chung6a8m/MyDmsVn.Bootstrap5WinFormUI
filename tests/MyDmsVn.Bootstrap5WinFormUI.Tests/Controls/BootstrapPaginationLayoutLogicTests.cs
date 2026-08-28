using System;
using System.Collections.Generic;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapPaginationLayoutLogicTests
{
    [TestCase(5, 3, 5, "1,2,3,4,5")]
    [TestCase(20, 1, 5, "1,2,3,4,...,20")]
    [TestCase(20, 10, 5, "1,...,9,10,11,...,20")]
    [TestCase(20, 20, 5, "1,...,17,18,19,20")]
    [TestCase(20, 10, 7, "1,...,8,9,10,11,12,...,20")]
    public void BuildReturnsExpectedWindow(int totalPages, int currentPage, int maxVisiblePages, string expected)
    {
        var items = BootstrapPaginationLayoutLogic.Build(totalPages, currentPage, maxVisiblePages);

        Assert.That(Format(items), Is.EqualTo(expected));
    }

    [Test]
    public void OnePageReturnsOnlyPageOne()
    {
        var items = BootstrapPaginationLayoutLogic.Build(1, 1, 5);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].Kind, Is.EqualTo(BootstrapPaginationItemKind.Page));
            Assert.That(items[0].Page, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ExactVisiblePageCountHasNoEllipsis()
    {
        var items = BootstrapPaginationLayoutLogic.Build(7, 4, 7);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(items.Count(item => item.Kind == BootstrapPaginationItemKind.Page), Is.EqualTo(7));
            Assert.That(items.Any(item => item.Kind == BootstrapPaginationItemKind.Ellipsis), Is.False);
        }));
    }

    [TestCase(20, 2, 5)]
    [TestCase(20, 19, 5)]
    [TestCase(100, 50, 9)]
    public void CurrentPageIsAlwaysIncluded(int totalPages, int currentPage, int maxVisiblePages)
    {
        var items = BootstrapPaginationLayoutLogic.Build(totalPages, currentPage, maxVisiblePages);

        Assert.That(items.Any(item => item.Kind == BootstrapPaginationItemKind.Page && item.Page == currentPage), Is.True);
    }

    [TestCase(20, 1, 5)]
    [TestCase(20, 10, 5)]
    [TestCase(20, 20, 5)]
    [TestCase(100, 50, 9)]
    public void BuildNeverReturnsDuplicatePageNumbers(int totalPages, int currentPage, int maxVisiblePages)
    {
        var pages = BootstrapPaginationLayoutLogic.Build(totalPages, currentPage, maxVisiblePages)
            .Where(item => item.Kind == BootstrapPaginationItemKind.Page)
            .Select(item => item.Page)
            .ToArray();

        Assert.That(pages.Distinct().Count(), Is.EqualTo(pages.Length));
    }

    [TestCase(0, 1, 5)]
    [TestCase(-1, 1, 5)]
    public void BuildRejectsInvalidTotalPages(int totalPages, int currentPage, int maxVisiblePages)
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPaginationLayoutLogic.Build(totalPages, currentPage, maxVisiblePages)));
    }

    [TestCase(5, 0, 5)]
    [TestCase(5, 6, 5)]
    public void BuildRejectsCurrentPageOutsideRange(int totalPages, int currentPage, int maxVisiblePages)
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPaginationLayoutLogic.Build(totalPages, currentPage, maxVisiblePages)));
    }

    [TestCase(1)]
    [TestCase(4)]
    public void BuildRejectsMaxVisiblePagesBelowFive(int maxVisiblePages)
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPaginationLayoutLogic.Build(10, 1, maxVisiblePages)));
    }

    private static string Format(IReadOnlyList<BootstrapPaginationItem> items)
    {
        return string.Join(",", items.Select(item => item.Kind == BootstrapPaginationItemKind.Ellipsis ? "..." : item.Page.ToString()));
    }
}
