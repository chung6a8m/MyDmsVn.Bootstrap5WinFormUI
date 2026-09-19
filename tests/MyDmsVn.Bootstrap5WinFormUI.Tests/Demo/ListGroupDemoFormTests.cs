using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class ListGroupDemoFormTests
{
    [Test]
    public void DemoExposesBasicActionableReorderVariantRichFlushAndHorizontalScenarios()
    {
        using var form = new ListGroupDemoForm();
        form.CreateControl();
        form.PerformLayout();
        var groups = FindControls<BootstrapListGroup>(form).ToArray();
        var items = groups.SelectMany(group => group.Items).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(groups.Length, Is.GreaterThanOrEqualTo(7));
            Assert.That(items.Any(item => item.Active), Is.True);
            Assert.That(items.Any(item => !item.Enabled), Is.True);
            Assert.That(items.Count(item => item.Actionable), Is.GreaterThanOrEqualTo(3));
            Assert.That(items.Where(item => item.Variant.HasValue).Select(item => item.Variant!.Value).Distinct(),
                Is.EquivalentTo((BootstrapVariant[])Enum.GetValues(typeof(BootstrapVariant))));
            Assert.That(items.Any(item => FindControls<BootstrapBadge>(item).Any()), Is.True);
            Assert.That(items.Any(item => FindControls<Button>(item).Any()), Is.True);
            Assert.That(groups.Any(group => group.Flush && group.Orientation == Orientation.Vertical), Is.True);
            Assert.That(groups.Any(group => group.Flush && group.Orientation == Orientation.Horizontal), Is.True);
        }));
    }

    [Test]
    public void ReorderDemoUsesSetChildIndexAndIntegratedNavigationRegistersPage()
    {
        using var form = new ListGroupDemoForm();
        form.Show();
        form.PerformLayout();
        var reorderGroup = FindControls<BootstrapListGroup>(form).Single(group => group.AccessibleName == "Reorder list group");
        var button = FindControls<Button>(form).Single(control => control.Text == "Move last item first");
        var last = reorderGroup.Items.Last();

        button.PerformClick();

        Assert.That(reorderGroup.Items.First(), Is.SameAs(last));

        using var main = new MainForm();
        var sidebar = FindControls<BootstrapSidebar>(main).Single();
        Assert.That(sidebar.Items.Any(item => item.Text == "List Group"), Is.True);
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match) yield return match;
            foreach (var nested in FindControls<T>(child)) yield return nested;
        }
    }
}
