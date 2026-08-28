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
[NonParallelizable]
public sealed class FeedbackDemoToastTests
{
    [Test]
    public void FeedbackDemoContainsToastHostAndAllStage8Actions()
    {
        using var form = new FeedbackDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var host = FindControls<BootstrapToastContainer>(form).Single();
        var actionNames = FindControls<Button>(form)
            .Select(button => button.AccessibleName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.AccessibleName, Is.EqualTo("Toast demo container"));
            Assert.That(host.MaximumVisibleToasts, Is.EqualTo(3));
            Assert.That(host.ToastSpacing, Is.EqualTo(8));
            Assert.That(host.Placement, Is.EqualTo(BootstrapToastPlacement.TopRight));
            Assert.That(actionNames, Does.Contain("Show manual Toast"));
            Assert.That(actionNames, Does.Contain("Show auto-hide Toast"));
            Assert.That(actionNames, Does.Contain("Show icon multiline Toast"));
            Assert.That(actionNames, Does.Contain("Burst 8 Toasts"));
            Assert.That(actionNames, Does.Contain("Dismiss all Toasts"));
            Assert.That(actionNames, Does.Contain("Cycle Toast placement"));
            Assert.That(actionNames, Does.Contain("Rapid show then dismiss Toast"));
            Assert.That(actionNames, Does.Contain("Show disabled Toast"));
            Assert.That(actionNames, Does.Contain("Stress 100 Toasts"));
        }));
    }

    [Test]
    public void ToastActionsExercisePublicSurfaceQueueAndSemanticExamples()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
            using var form = new FeedbackDemoForm();
            form.Show();
            Application.DoEvents();

            var host = FindControls<BootstrapToastContainer>(form).Single();
            var buttons = FindControls<Button>(form).ToDictionary(
                button => button.AccessibleName ?? string.Empty,
                StringComparer.Ordinal);

            buttons["Show manual Toast"].PerformClick();
            buttons["Show icon multiline Toast"].PerformClick();
            buttons["Show disabled Toast"].PerformClick();
            Application.DoEvents();

            var examples = host.Controls.OfType<BootstrapToast>().ToArray();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(examples.Any(toast => toast.Variant == BootstrapVariant.Success && !toast.AutoHide), Is.True);
                Assert.That(examples.Any(toast => toast.Variant == BootstrapVariant.Warning && toast.Icon is not null && toast.Text.Contains("\n")), Is.True);
                Assert.That(examples.Any(toast => toast.Variant == BootstrapVariant.Info && !toast.Enabled), Is.True);
                Assert.That(examples.Count(toast => toast.Visible), Is.EqualTo(3));
            }));

            buttons["Dismiss all Toasts"].PerformClick();
            Application.DoEvents();
            Assert.That(host.Controls.OfType<BootstrapToast>(), Is.Empty);

            buttons["Burst 8 Toasts"].PerformClick();
            Application.DoEvents();
            var burst = host.Controls.OfType<BootstrapToast>().ToArray();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(burst.Length, Is.EqualTo(8));
                Assert.That(burst.Count(toast => toast.Visible), Is.EqualTo(3));
                Assert.That(burst.Any(toast => toast.Variant == BootstrapVariant.Success), Is.True);
                Assert.That(burst.Any(toast => toast.Variant == BootstrapVariant.Warning), Is.True);
                Assert.That(burst.Any(toast => toast.Variant == BootstrapVariant.Danger), Is.True);
                Assert.That(burst.Any(toast => toast.Variant == BootstrapVariant.Info), Is.True);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void PlacementActionCyclesEverySupportedCorner()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
            using var form = new FeedbackDemoForm();
            form.CreateControl();
            var host = FindControls<BootstrapToastContainer>(form).Single();
            var cycle = FindControls<Button>(form).Single(button => button.AccessibleName == "Cycle Toast placement");

            var observed = new List<BootstrapToastPlacement> { host.Placement };
            for (var index = 0; index < 4; index++)
            {
                cycle.PerformClick();
                observed.Add(host.Placement);
            }

            Assert.Multiple((Action)(() =>
            {
                Assert.That(observed, Does.Contain(BootstrapToastPlacement.TopLeft));
                Assert.That(observed, Does.Contain(BootstrapToastPlacement.TopRight));
                Assert.That(observed, Does.Contain(BootstrapToastPlacement.BottomLeft));
                Assert.That(observed, Does.Contain(BootstrapToastPlacement.BottomRight));
                Assert.That(host.Placement, Is.EqualTo(BootstrapToastPlacement.TopRight));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ExistingToastSurvivesRuntimeThemeSwitchWithoutLifetimeReset()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
            using var form = new FeedbackDemoForm();
            form.CreateControl();
            var host = FindControls<BootstrapToastContainer>(form).Single();
            var manual = FindControls<Button>(form).Single(button => button.AccessibleName == "Show manual Toast");

            manual.PerformClick();
            var toast = host.Controls.OfType<BootstrapToast>().Single();

            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark, reducedMotion: true);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(toast.IsDisposed, Is.False);
                Assert.That(host.Controls.OfType<BootstrapToast>().Single(), Is.SameAs(toast));
                Assert.That(toast.AutoHide, Is.False);
                Assert.DoesNotThrow((Action)(() => toast.PerformLayout()));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ToastGuidanceCoversReducedMotionDpiAndResourceStress()
    {
        using var form = new FeedbackDemoForm();
        form.CreateControl();
        var labels = FindControls<Label>(form).Select(label => label.Text).ToArray();
        var allText = string.Join("\n", labels);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(allText, Does.Contain("Reduced motion"));
            Assert.That(allText, Does.Contain("AutoHide"));
            Assert.That(allText, Does.Contain("100%"));
            Assert.That(allText, Does.Contain("125%"));
            Assert.That(allText, Does.Contain("150%"));
            Assert.That(allText, Does.Contain("175%"));
            Assert.That(allText, Does.Contain("200%"));
            Assert.That(allText, Does.Contain("USER/GDI"));
            Assert.That(allText, Does.Contain("Stress 100"));
        }));
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
