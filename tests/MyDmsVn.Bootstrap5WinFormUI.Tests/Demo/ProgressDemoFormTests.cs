using System;
using System.Collections.Generic;
using System.Drawing;
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
public sealed class ProgressDemoFormTests
{
    [Test]
    public void Phase11DemoExposesAllRequiredProgressScenarios()
    {
        var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.ProgressDemoForm");

        Assert.That(demoType, Is.Not.Null, "Phase 11 requires a ProgressDemoForm.");
        using var form = (Form)Activator.CreateInstance(demoType!)!;
        form.CreateControl();
        form.PerformLayout();

        var progressBars = FindControls<BootstrapProgressBar>(form).ToArray();
        var variants = progressBars.Select(progress => progress.Variant).Distinct().ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(progressBars.Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(variants, Is.EquivalentTo(Enum.GetValues(typeof(BootstrapVariant)).Cast<BootstrapVariant>()));
            Assert.That(progressBars.Any(progress => progress.ShowText && progress.TextFormat.Contains("{1}")), Is.True);
            Assert.That(progressBars.Any(progress => progress.Striped && !progress.Animated), Is.True);
            Assert.That(progressBars.Any(progress => progress.Striped && progress.Animated), Is.True);
            Assert.That(progressBars.Any(progress => progress.Indeterminate), Is.True);
            Assert.That(progressBars.Any(progress => !progress.CustomColor.IsEmpty), Is.True);
            Assert.That(progressBars.Any(progress => progress.BorderRadius == 0), Is.True);
        }));
    }

    [Test]
    public void ContentRowsUseBodyBackgroundInsteadOfToolbarSurface()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            BootstrapThemeManager.CurrentTheme = theme;

            var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.ProgressDemoForm");
            Assert.That(demoType, Is.Not.Null);

            using var form = (Form)Activator.CreateInstance(demoType!)!;
            form.CreateControl();
            form.PerformLayout();

            var primary = FindControls<BootstrapProgressBar>(form)
                .Single(progress => progress.AccessibleName == "Primary progress");

            Assert.That(primary.Parent, Is.Not.Null);
            Assert.That(primary.Parent!.BackColor.ToArgb(), Is.EqualTo(theme.Colors.Body.ToArgb()));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void DemoExposesAnimateToCommands()
    {
        var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.ProgressDemoForm");
        Assert.That(demoType, Is.Not.Null);

        using var form = (Form)Activator.CreateInstance(demoType!)!;
        form.CreateControl();
        form.PerformLayout();

        var buttonTexts = FindControls<Button>(form).Select(button => button.Text).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(buttonTexts, Does.Contain("25%"));
            Assert.That(buttonTexts, Does.Contain("75%"));
            Assert.That(buttonTexts, Does.Contain("Complete"));
            Assert.That(buttonTexts, Does.Contain("Reset"));
        }));
    }

    [Test]
    public void MainDemoExposesProgressNavigationPage()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        Assert.That(
            sidebar.Items.Any(item => item.Text == "Progress"),
            Is.True,
            "Phase 11 needs to remain reachable from the integrated demo navigation.");
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
