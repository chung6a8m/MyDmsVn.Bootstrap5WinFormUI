using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapPaginationContractTests
{
    [Test]
    public void DefaultsMatchPaginationContract()
    {
        using var pagination = new BootstrapPagination();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pagination.TotalItems, Is.EqualTo(0));
            Assert.That(pagination.PageSize, Is.EqualTo(20));
            Assert.That(pagination.CurrentPage, Is.EqualTo(1));
            Assert.That(pagination.TotalPages, Is.EqualTo(1));
            Assert.That(pagination.MaxVisiblePages, Is.EqualTo(5));
            Assert.That(pagination.ShowFirstLast, Is.True);
            Assert.That(pagination.ShowPreviousNext, Is.True);
            Assert.That(pagination.ButtonSize, Is.EqualTo(BootstrapButtonSize.Default));
            Assert.That(pagination.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(pagination.BorderRadius, Is.EqualTo(-1));
            Assert.That(pagination.AutoSize, Is.True);
            Assert.That(pagination.AutoSizeMode, Is.EqualTo(AutoSizeMode.GrowAndShrink));
            Assert.That(pagination.TabStop, Is.False);
            Assert.That(pagination.AccessibleRole, Is.EqualTo(AccessibleRole.Grouping));
            Assert.That(pagination.AccessibleDescription, Is.EqualTo("Pagination navigation."));
        }));
    }

    [Test]
    public void PublicPaginationSurfaceContainsOnlyPlannedDeclaredMembers()
    {
        var type = typeof(BootstrapPagination);
        var publicDeclared = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType is MemberTypes.Constructor or MemberTypes.Event or MemberTypes.Property)
            .Select(member => member.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(publicDeclared, Is.EqualTo(new[]
        {
            ".ctor",
            "BorderRadius",
            "ButtonSize",
            "CurrentPage",
            "MaxVisiblePages",
            "PageChanged",
            "PageSize",
            "ShowFirstLast",
            "ShowPreviousNext",
            "TotalItems",
            "TotalPages",
            "Variant"
        }));
    }

    [Test]
    public void PaginationDeclaresPageChangedAsDefaultEvent()
    {
        var attribute = typeof(BootstrapPagination).GetCustomAttribute<DefaultEventAttribute>();

        Assert.That(attribute?.Name, Is.EqualTo(nameof(BootstrapPagination.PageChanged)));
    }

    [Test]
    public void ReducingTotalItemsClampsCurrentPageAndRaisesOneEvent()
    {
        using var pagination = new BootstrapPagination { TotalItems = 100, PageSize = 10, CurrentPage = 10 };
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        pagination.TotalItems = 15;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pagination.TotalPages, Is.EqualTo(2));
            Assert.That(pagination.CurrentPage, Is.EqualTo(2));
            Assert.That(count, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ReducingPageSizeRangeClampsCurrentPageAndRaisesOneEvent()
    {
        using var pagination = new BootstrapPagination { TotalItems = 100, PageSize = 10, CurrentPage = 10 };
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        pagination.PageSize = 50;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pagination.TotalPages, Is.EqualTo(2));
            Assert.That(pagination.CurrentPage, Is.EqualTo(2));
            Assert.That(count, Is.EqualTo(1));
        }));
    }

    [Test]
    public void PageSizeChangeThatKeepsCurrentPageValidDoesNotRaisePageChanged()
    {
        using var pagination = new BootstrapPagination { TotalItems = 100, PageSize = 20, CurrentPage = 2 };
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        pagination.PageSize = 25;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pagination.TotalPages, Is.EqualTo(4));
            Assert.That(pagination.CurrentPage, Is.EqualTo(2));
            Assert.That(count, Is.EqualTo(0));
        }));
    }

    [Test]
    public void AssigningSameCurrentPageDoesNotRaisePageChanged()
    {
        using var pagination = new BootstrapPagination { TotalItems = 100, PageSize = 10, CurrentPage = 5 };
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        pagination.CurrentPage = 5;

        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void TotalPagesCalculationIsOverflowSafeAtIntMaxValue()
    {
        using var pagination = new BootstrapPagination { TotalItems = int.MaxValue, PageSize = int.MaxValue };

        Assert.That(pagination.TotalPages, Is.EqualTo(1));
    }

    [TestCase(-1)]
    public void TotalItemsRejectsNegativeValues(int value)
    {
        using var pagination = new BootstrapPagination();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => pagination.TotalItems = value));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void PageSizeRejectsNonPositiveValues(int value)
    {
        using var pagination = new BootstrapPagination();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => pagination.PageSize = value));
    }

    [TestCase(0)]
    [TestCase(4)]
    public void MaxVisiblePagesRejectsValuesBelowFive(int value)
    {
        using var pagination = new BootstrapPagination();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => pagination.MaxVisiblePages = value));
    }

    [Test]
    public void CurrentPageRejectsValuesOutsideCurrentRange()
    {
        using var pagination = new BootstrapPagination { TotalItems = 50, PageSize = 10 };

        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => pagination.CurrentPage = 0));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => pagination.CurrentPage = 6));
        }));
    }

    [Test]
    public void BorderRadiusRejectsValuesBelowThemeSentinel()
    {
        using var pagination = new BootstrapPagination();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => pagination.BorderRadius = -2));
        Assert.DoesNotThrow((Action)(() => pagination.BorderRadius = -1));
        Assert.DoesNotThrow((Action)(() => pagination.BorderRadius = 0));
    }
}
