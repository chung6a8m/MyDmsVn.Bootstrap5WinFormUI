using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSidebarTests
{
    [Test]
    public void DefaultsMatchPhase12Contract()
    {
        using var sidebar = new BootstrapSidebar();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(sidebar.ExpandedWidth, Is.EqualTo(260));
            Assert.That(sidebar.CollapsedWidth, Is.EqualTo(72));
            Assert.That(sidebar.Expanded, Is.True);
            Assert.That(sidebar.Width, Is.EqualTo(260));
            Assert.That(sidebar.SelectedItem, Is.Null);
            Assert.That(sidebar.Items, Is.Empty);
            Assert.That(sidebar.TabStop, Is.False);
            Assert.That(sidebar.AccessibleRole, Is.EqualTo(AccessibleRole.Grouping));
        }));
    }

    [Test]
    public void WidthEndpointsMustRemainOrdered()
    {
        using var sidebar = new BootstrapSidebar();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => sidebar.ExpandedWidth = sidebar.CollapsedWidth));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => sidebar.CollapsedWidth = sidebar.ExpandedWidth));
    }

    [Test]
    public void SelectedItemMustBelongToSidebarTree()
    {
        using var sidebar = new BootstrapSidebar();
        var root = new BootstrapSidebarItem { Text = "Root" };
        var child = new BootstrapSidebarItem { Text = "Child" };
        root.Items.Add(child);
        sidebar.Items.Add(root);

        sidebar.SelectedItem = child;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(sidebar.SelectedItem, Is.SameAs(child));
            Assert.That(child.Selected, Is.True);
        }));

        Assert.Throws<ArgumentException>((Action)(() => sidebar.SelectedItem = new BootstrapSidebarItem { Text = "Foreign" }));
    }

    [Test]
    public void ItemTreeBuildsButtonRowsAndCollapseBackedNestedSections()
    {
        using var sidebar = new BootstrapSidebar();
        var dashboard = new BootstrapSidebarItem { Text = "Dashboard", BadgeText = "3" };
        var reports = new BootstrapSidebarItem { Text = "Reports" };
        reports.Items.Add(new BootstrapSidebarItem { Text = "Sales" });
        sidebar.Items.Add(dashboard);
        sidebar.Items.Add(reports);

        var buttons = Descendants(sidebar).OfType<BootstrapButton>().ToArray();
        var collapses = Descendants(sidebar).OfType<BootstrapCollapse>().ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(buttons, Has.Length.EqualTo(3));
            Assert.That(collapses, Has.Length.EqualTo(1));
            Assert.That(buttons.Single(button => ReferenceEquals(button.Tag, dashboard)).AccessibleName, Is.EqualTo("Dashboard"));
            Assert.That(buttons.Single(button => ReferenceEquals(button.Tag, reports)).AccessibleName, Is.EqualTo("Reports"));
        }));
    }

    [Test]
    public void ActivatingEnabledItemsSelectsThemAndParentActivationTogglesNestedSection()
    {
        using var sidebar = new BootstrapSidebar();
        var home = new BootstrapSidebarItem { Text = "Home" };
        var reports = new BootstrapSidebarItem { Text = "Reports" };
        var sales = new BootstrapSidebarItem { Text = "Sales" };
        reports.Items.Add(sales);
        sidebar.Items.Add(home);
        sidebar.Items.Add(reports);

        FindButton(sidebar, home).PerformClick();
        Assert.That(sidebar.SelectedItem, Is.SameAs(home));

        FindButton(sidebar, reports).PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(sidebar.SelectedItem, Is.SameAs(reports));
            Assert.That(home.Selected, Is.False);
            Assert.That(reports.Selected, Is.True);
            Assert.That(reports.Expanded, Is.True);
            Assert.That(Descendants(sidebar).OfType<BootstrapCollapse>().Single().Expanded, Is.True);
        }));

        FindButton(sidebar, reports).PerformClick();
        Assert.That(reports.Expanded, Is.False);
    }

    [Test]
    public void DisabledItemCannotBecomeSelectedThroughActivation()
    {
        using var sidebar = new BootstrapSidebar();
        var enabled = new BootstrapSidebarItem { Text = "Enabled" };
        var disabled = new BootstrapSidebarItem { Text = "Disabled", Enabled = false };
        sidebar.Items.Add(enabled);
        sidebar.Items.Add(disabled);
        sidebar.SelectedItem = enabled;

        FindButton(sidebar, disabled).PerformClick();

        Assert.That(sidebar.SelectedItem, Is.SameAs(enabled));
    }

    [Test]
    public void CollapsedModeKeepsIconsFocusableButHidesTextAndNestedContent()
    {
        using var sidebar = new BootstrapSidebar();
        var reports = new BootstrapSidebarItem { Text = "Reports", Expanded = true };
        reports.Items.Add(new BootstrapSidebarItem { Text = "Sales" });
        sidebar.Items.Add(reports);

        sidebar.Collapse();

        var rootButton = FindButton(sidebar, reports);
        var nested = Descendants(sidebar).OfType<BootstrapCollapse>().Single();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(sidebar.Width, Is.EqualTo(sidebar.CollapsedWidth));
            Assert.That(rootButton.Text, Is.Empty);
            Assert.That(rootButton.AccessibleName, Is.EqualTo("Reports"));
            Assert.That(rootButton.TabStop, Is.True);
            Assert.That(nested.Expanded, Is.False);
        }));

        sidebar.Expand();
        Assert.That(FindButton(sidebar, reports).Text, Is.EqualTo("Reports"));
    }

    private static BootstrapButton FindButton(Control root, BootstrapSidebarItem item)
    {
        return Descendants(root)
            .OfType<BootstrapButton>()
            .Single(button => ReferenceEquals(button.Tag, item));
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
