using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BreadcrumbDemoFormTests
{
    [Test]
    public void DemoExposesPlannedScenariosAndIntegratedNavigation()
    {
        using var form = new BreadcrumbDemoForm();
        form.CreateControl();
        form.PerformLayout();
        var breadcrumbs = FindControls<BootstrapBreadcrumb>(form).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumbs.Length, Is.GreaterThanOrEqualTo(9));
            Assert.That(breadcrumbs.Any(breadcrumb => breadcrumb.Items.Count == 1), Is.True);
            Assert.That(breadcrumbs.Any(breadcrumb => breadcrumb.Items.Count >= 4), Is.True);
            Assert.That(breadcrumbs.Any(breadcrumb => breadcrumb.Divider == ">"), Is.True);
            Assert.That(breadcrumbs.Any(breadcrumb => breadcrumb.Divider == string.Empty), Is.True);
            Assert.That(breadcrumbs.Any(breadcrumb => breadcrumb.MaximumSize.Width == 320), Is.True);
            Assert.That(breadcrumbs.Any(breadcrumb => breadcrumb.RightToLeft == RightToLeft.Yes && breadcrumb.RightToLeftDivider == "<"), Is.True);
            Assert.That(breadcrumbs.Any(breadcrumb => !breadcrumb.Enabled), Is.True);
            Assert.That(FindControls<Label>(form).Any(label => label.AccessibleName == "Breadcrumb activation output"), Is.True);
        }));

        using var main = new MainForm();
        var sidebar = FindControls<BootstrapSidebar>(main).Single();
        Assert.That(sidebar.Items.Any(item => item.Text == "Breadcrumb"), Is.True);
    }

    [Test]
    public void InteractiveScenarioMakesCallerOwnedNavigationMutationVisible()
    {
        using var form = new BreadcrumbDemoForm();
        var breadcrumb = FindControls<BootstrapBreadcrumb>(form)
            .Single(control => control.AccessibleName == "Interactive navigation breadcrumb");
        var output = FindControls<Label>(form)
            .Single(label => label.AccessibleName == "Breadcrumb activation output");
        var target = breadcrumb.Controls.OfType<LinkLabel>().Skip(1).First();

        Activate(target);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.Items.Count, Is.EqualTo(2));
            Assert.That(breadcrumb.Items[breadcrumb.Items.Count - 1].Text, Is.EqualTo("Library"));
            Assert.That(output.Text, Does.Contain("index 1"));
            Assert.That(output.Text, Does.Contain("Library"));
        }));
    }

    private static void Activate(LinkLabel link)
    {
        var method = typeof(LinkLabel).GetMethod(
            "OnLinkClicked",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var nativeLink = link.Links.Cast<LinkLabel.Link>().Single();
        method.Invoke(link, new object[] { new LinkLabelLinkClickedEventArgs(nativeLink) });
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
