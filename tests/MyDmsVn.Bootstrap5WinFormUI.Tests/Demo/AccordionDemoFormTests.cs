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
public sealed class AccordionDemoFormTests
{
    [Test]
    public void Phase10DemoExposesSingleMultipleFlushIconAndNestedScenarios()
    {
        using var form = new AccordionDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var accordions = FindControls<BootstrapAccordion>(form).ToArray();
        var allItems = accordions.SelectMany(accordion => accordion.Items).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(accordions.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(accordions.Any(accordion => !accordion.AllowMultipleOpen && !accordion.Flush), Is.True);
            Assert.That(accordions.Any(accordion => accordion.AllowMultipleOpen && accordion.Flush), Is.True);
            Assert.That(allItems.Any(item => item.Header.Icon is not null), Is.True);
            Assert.That(
                accordions.Any(accordion => accordion.Parent?.Parent is BootstrapCollapse),
                Is.True,
                "The demo should include an accordion nested inside another accordion item's Body panel.");
        }));
    }

    [Test]
    public void DynamicDemoCommandAddsAnotherFlushAccordionItem()
    {
        using var form = new AccordionDemoForm();
        form.Show();
        form.PerformLayout();

        var target = FindControls<BootstrapAccordion>(form)
            .Single(accordion => accordion.AllowMultipleOpen && accordion.Flush);
        var addButton = FindControls<Button>(form)
            .Single(button => button.Text == "Add dynamic item");
        var before = target.Items.Count;

        addButton.PerformClick();
        form.PerformLayout();

        Assert.That(target.Items.Count, Is.EqualTo(before + 1));
    }

    [Test]
    public void MainDemoExposesCollapseAccordionNavigationPage()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        Assert.That(
            sidebar.Items.Any(item => item.Text == "Collapse / Accordion"),
            Is.True,
            "Phase 10 needs to remain reachable from the integrated demo navigation.");
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }
}
