using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class SidebarDemoFormTests
{
    [Test]
    public void Phase12DemoExposesNavigationBadgeDisabledAndNestedScenarios()
    {
        var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.SidebarDemoForm");

        Assert.That(demoType, Is.Not.Null, "Phase 12 requires a SidebarDemoForm.");
        using var form = (Form)Activator.CreateInstance(demoType!)!;
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        var allItems = Flatten(sidebar.Items).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(sidebar.Items.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(allItems.Any(item => item.Icon is not null), Is.True);
            Assert.That(allItems.Any(item => !string.IsNullOrEmpty(item.BadgeText)), Is.True);
            Assert.That(allItems.Any(item => !item.Enabled), Is.True);
            Assert.That(allItems.Any(item => item.Items.Count > 0), Is.True);
            Assert.That(sidebar.SelectedItem, Is.Not.Null);
        }));
    }

    [Test]
    public void DemoExposesInteractiveSidebarCommands()
    {
        var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.SidebarDemoForm");
        Assert.That(demoType, Is.Not.Null);

        using var form = (Form)Activator.CreateInstance(demoType!)!;
        form.CreateControl();
        form.PerformLayout();

        var buttonTexts = FindControls<Button>(form).Select(button => button.Text).ToArray();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(buttonTexts, Does.Contain("Toggle sidebar"));
            Assert.That(buttonTexts, Does.Contain("Select Sales"));
        }));
    }

    [Test]
    public void SidebarDemoUsesCurrentThemeSurface()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark, reducedMotion: true);
            BootstrapThemeManager.CurrentTheme = theme;
            var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.SidebarDemoForm");
            Assert.That(demoType, Is.Not.Null);

            using var form = (Form)Activator.CreateInstance(demoType!)!;
            form.CreateControl();
            form.PerformLayout();

            var sidebar = FindControls<BootstrapSidebar>(form).Single();
            Assert.That(sidebar.BackColor.ToArgb(), Is.EqualTo(theme.Colors.Surface.ToArgb()));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void MainDemoUsesSidebarForIntegratedNavigation()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single(control => control.AccessibleName == "Integrated demo navigation");
        Assert.That(
            sidebar.Items.Any(item => item.Text == "Sidebar"),
            Is.True,
            "Phase 12 needs to remain reachable from the integrated demo navigation.");
    }

    private static IEnumerable<BootstrapSidebarItem> Flatten(IEnumerable<BootstrapSidebarItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var child in Flatten(item.Items))
            {
                yield return child;
            }
        }
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
