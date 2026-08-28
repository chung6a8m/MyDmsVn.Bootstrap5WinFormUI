using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapPaginationTests
{
    [TestCase(5, 3, "«,‹,1,2,3,4,5,›,»")]
    [TestCase(20, 1, "«,‹,1,2,3,4,…,20,›,»")]
    [TestCase(20, 10, "«,‹,1,…,9,10,11,…,20,›,»")]
    [TestCase(20, 20, "«,‹,1,…,17,18,19,20,›,»")]
    public void CompositionMatchesPlannedWindows(int totalPages, int currentPage, string expected)
    {
        using var pagination = CreatePagination(totalPages, currentPage);
        var buttons = GetButtons(pagination);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(string.Join(",", buttons.Select(button => button.Text)), Is.EqualTo(expected));
            Assert.That(buttons.Count(button => button.Selected), Is.EqualTo(1));
            Assert.That(buttons.Single(button => button.Selected).Text, Is.EqualTo(currentPage.ToString()));
        }));
    }

    [Test]
    public void OwnsOneHorizontalNonSelectingButtonGroup()
    {
        using var pagination = new BootstrapPagination();
        var group = GetGroup(pagination);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pagination.Controls.OfType<BootstrapButtonGroup>().Count(), Is.EqualTo(1));
            Assert.That(group.Orientation, Is.EqualTo(Orientation.Horizontal));
            Assert.That(group.SelectionMode, Is.EqualTo(BootstrapButtonSelectionMode.None));
        }));
    }

    [Test]
    public void BoundaryButtonsAreDisabledAndCurrentPageStaysFocusable()
    {
        using var firstPage = CreatePagination(20, 1);
        var firstButtons = GetButtons(firstPage);
        using var lastPage = CreatePagination(20, 20);
        var lastButtons = GetButtons(lastPage);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Find(firstButtons, "First page").Enabled, Is.False);
            Assert.That(Find(firstButtons, "Previous page").Enabled, Is.False);
            Assert.That(Find(firstButtons, "Next page").Enabled, Is.True);
            Assert.That(Find(firstButtons, "Last page").Enabled, Is.True);
            Assert.That(firstButtons.Single(button => button.Selected).Enabled, Is.True);
            Assert.That(firstButtons.Single(button => button.Selected).TabStop, Is.True);

            Assert.That(Find(lastButtons, "First page").Enabled, Is.True);
            Assert.That(Find(lastButtons, "Previous page").Enabled, Is.True);
            Assert.That(Find(lastButtons, "Next page").Enabled, Is.False);
            Assert.That(Find(lastButtons, "Last page").Enabled, Is.False);
        }));
    }

    [Test]
    public void EllipsisIsDisabledAndNotTabbable()
    {
        using var pagination = CreatePagination(20, 10);
        var ellipses = GetButtons(pagination).Where(button => button.Text == "…").ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ellipses, Has.Length.EqualTo(2));
            Assert.That(ellipses.All(button => !button.Enabled), Is.True);
            Assert.That(ellipses.All(button => !button.TabStop), Is.True);
        }));
    }

    [Test]
    public void NavigationButtonsMovePagesAndRaiseOneEventPerMove()
    {
        using var pagination = CreatePagination(20, 10);
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        Find(GetButtons(pagination), "First page").PerformClick();
        Assert.That(pagination.CurrentPage, Is.EqualTo(1));

        Find(GetButtons(pagination), "Next page").PerformClick();
        Assert.That(pagination.CurrentPage, Is.EqualTo(2));

        Find(GetButtons(pagination), "Page 4").PerformClick();
        Assert.That(pagination.CurrentPage, Is.EqualTo(4));

        Find(GetButtons(pagination), "Previous page").PerformClick();
        Assert.That(pagination.CurrentPage, Is.EqualTo(3));

        Find(GetButtons(pagination), "Last page").PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pagination.CurrentPage, Is.EqualTo(20));
            Assert.That(count, Is.EqualTo(5));
        }));
    }

    [Test]
    public void ClickingCurrentPageIsNoOp()
    {
        using var pagination = CreatePagination(20, 10);
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        GetButtons(pagination).Single(button => button.Selected).PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pagination.CurrentPage, Is.EqualTo(10));
            Assert.That(count, Is.EqualTo(0));
        }));
    }

    [Test]
    public void DisabledBoundaryAndEllipsisClicksAreNoOps()
    {
        using var pagination = CreatePagination(20, 1);
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        Find(GetButtons(pagination), "First page").PerformClick();
        Find(GetButtons(pagination), "Previous page").PerformClick();
        GetButtons(pagination).Single(button => button.Text == "…").PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pagination.CurrentPage, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(0));
        }));
    }

    [Test]
    public void VisualPropertyChangesUpdateExistingControlsWithoutChangingPage()
    {
        using var pagination = CreatePagination(20, 10);
        var group = GetGroup(pagination);
        var buttons = GetButtons(pagination);
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        pagination.ButtonSize = BootstrapButtonSize.Large;
        pagination.Variant = BootstrapVariant.Success;
        pagination.BorderRadius = 12;

        var updatedButtons = GetButtons(pagination);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetGroup(pagination), Is.SameAs(group));
            Assert.That(updatedButtons, Has.Length.EqualTo(buttons.Length));
            for (var index = 0; index < buttons.Length; index++)
            {
                Assert.That(updatedButtons[index], Is.SameAs(buttons[index]));
            }

            Assert.That(updatedButtons.All(button => button.ButtonSize == BootstrapButtonSize.Large), Is.True);
            Assert.That(updatedButtons.All(button => button.Variant == BootstrapVariant.Success), Is.True);
            Assert.That(group.BorderRadius, Is.EqualTo(12));
            Assert.That(pagination.CurrentPage, Is.EqualTo(10));
            Assert.That(count, Is.EqualTo(0));
        }));
    }

    [Test]
    public void RebuildDisposesEveryRemovedButton()
    {
        using var pagination = CreatePagination(20, 10);
        var oldButtons = GetButtons(pagination);

        pagination.TotalItems = 50;

        Assert.That(oldButtons.All(button => button.IsDisposed), Is.True);
    }

    [Test]
    public void ZeroItemsDisplaysSelectedPageOneAndDisablesDirectionalNavigation()
    {
        using var pagination = new BootstrapPagination();
        var buttons = GetButtons(pagination);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(buttons.Single(button => button.Selected).Text, Is.EqualTo("1"));
            Assert.That(buttons.Where(button => button.AccessibleName is "First page" or "Previous page" or "Next page" or "Last page").All(button => !button.Enabled), Is.True);
        }));
    }

    [Test]
    public void DisabledParentPreventsChildNavigationWithoutMutatingPageState()
    {
        using var pagination = CreatePagination(20, 10);
        pagination.Enabled = false;

        Find(GetButtons(pagination), "Next page").PerformClick();

        Assert.That(pagination.CurrentPage, Is.EqualTo(10));
    }

    [Test]
    public void ChildAccessibleNamesDescribeNavigationSemantics()
    {
        using var pagination = CreatePagination(20, 10);
        var buttons = GetButtons(pagination);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Find(buttons, "First page"), Is.Not.Null);
            Assert.That(Find(buttons, "Previous page"), Is.Not.Null);
            Assert.That(Find(buttons, "Current page 10").Selected, Is.True);
            Assert.That(Find(buttons, "Page 9"), Is.Not.Null);
            Assert.That(Find(buttons, "Next page"), Is.Not.Null);
            Assert.That(Find(buttons, "Last page"), Is.Not.Null);
        }));
    }

    [Test]
    public void GetPreferredSizeContainsOwnedGroup()
    {
        using var pagination = CreatePagination(20, 10);
        var group = GetGroup(pagination);
        var groupPreferred = group.GetPreferredSize(Size.Empty);
        var preferred = pagination.GetPreferredSize(Size.Empty);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(preferred.Width, Is.GreaterThanOrEqualTo(groupPreferred.Width));
            Assert.That(preferred.Height, Is.GreaterThanOrEqualTo(groupPreferred.Height));
            Assert.That(preferred.Width, Is.GreaterThan(0));
            Assert.That(preferred.Height, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void RepeatedStateChangesKeepCurrentPageVisibleWithoutDuplicateChildren()
    {
        using var pagination = CreatePagination(50, 25);

        for (var page = 1; page <= 50; page++)
        {
            pagination.CurrentPage = page;
            var buttons = GetButtons(pagination);
            Assert.That(buttons.Count(button => button.Selected), Is.EqualTo(1));
            Assert.That(buttons.Single(button => button.Selected).Text, Is.EqualTo(page.ToString()));
            Assert.That(buttons.Distinct().Count(), Is.EqualTo(buttons.Length));
        }
    }

    [Test]
    public void RepeatedVisualAssignmentsDoNotDuplicateChildrenOrPageEvents()
    {
        using var pagination = CreatePagination(20, 10);
        var initial = GetButtons(pagination);
        var count = 0;
        pagination.PageChanged += (_, _) => count++;

        for (var index = 0; index < 20; index++)
        {
            pagination.ButtonSize = BootstrapButtonSize.Default;
            pagination.Variant = BootstrapVariant.Primary;
            pagination.BorderRadius = -1;
            pagination.ShowFirstLast = true;
            pagination.ShowPreviousNext = true;
        }

        var current = GetButtons(pagination);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(current, Has.Length.EqualTo(initial.Length));
            for (var index = 0; index < initial.Length; index++)
            {
                Assert.That(current[index], Is.SameAs(initial[index]));
            }

            Assert.That(count, Is.EqualTo(0));
        }));
    }

    [Test]
    public void LifecycleStressDisposesOwnedGroupAndCurrentButtons()
    {
        var pagination = CreatePagination(200, 1);
        for (var index = 0; index < 100; index++)
        {
            pagination.CurrentPage = (index % pagination.TotalPages) + 1;
            pagination.MaxVisiblePages = 5 + (index % 4);
            pagination.ShowFirstLast = index % 2 == 0;
            pagination.ShowPreviousNext = index % 3 != 0;
        }

        var group = GetGroup(pagination);
        var currentButtons = GetButtons(pagination);

        pagination.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group.IsDisposed, Is.True);
            Assert.That(currentButtons.All(button => button.IsDisposed), Is.True);
        }));
    }

    private static BootstrapPagination CreatePagination(int totalPages, int currentPage)
    {
        return new BootstrapPagination
        {
            PageSize = 10,
            TotalItems = totalPages * 10,
            CurrentPage = currentPage
        };
    }

    private static BootstrapButtonGroup GetGroup(BootstrapPagination pagination)
    {
        return pagination.Controls.OfType<BootstrapButtonGroup>().Single();
    }

    private static BootstrapButton[] GetButtons(BootstrapPagination pagination)
    {
        return GetGroup(pagination).Controls.OfType<BootstrapButton>().ToArray();
    }

    private static BootstrapButton Find(BootstrapButton[] buttons, string accessibleName)
    {
        return buttons.Single(button => string.Equals(button.AccessibleName, accessibleName, StringComparison.Ordinal));
    }
}
