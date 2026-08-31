using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
public sealed class FeedbackDemoFormTests
{
    [Test]
    public void FeedbackDemoCoversStage1BadgeMatrix()
    {
        using var form = new FeedbackDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var badges = FindControls<BootstrapBadge>(form).ToArray();
        var semanticVariants = Enum.GetValues(typeof(BootstrapVariant)).Cast<BootstrapVariant>().ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(form.AutoScaleMode, Is.EqualTo(AutoScaleMode.Dpi));
            Assert.That(badges, Is.Not.Empty);
            Assert.That(
                semanticVariants.All(variant => badges.Any(badge => badge.Variant == variant && badge.CustomColor.IsEmpty)),
                Is.True,
                "Every semantic variant should have a normal semantic Badge example.");
            Assert.That(badges.Any(badge => badge.Pill), Is.True);
            Assert.That(badges.Any(badge => !badge.CustomColor.IsEmpty), Is.True);
            Assert.That(badges.Any(badge => !badge.Enabled), Is.True);
            Assert.That(badges.Any(badge => badge.Text.Length >= 30), Is.True);
            Assert.That(badges.All(badge => badge.AutoSize), Is.True);
            Assert.That(badges.All(badge => !badge.TabStop), Is.True);
        }));
    }

    [Test]
    public void FeedbackDemoCoversStage2AlertMatrix()
    {
        using var form = new FeedbackDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var alerts = FindControls<BootstrapAlert>(form).ToArray();
        var semanticVariants = Enum.GetValues(typeof(BootstrapVariant)).Cast<BootstrapVariant>().ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alerts.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(
                semanticVariants.All(variant => alerts.Any(alert => alert.Variant == variant)),
                Is.True,
                "Every semantic variant should have an Alert example.");
            Assert.That(alerts.Any(alert => alert.Icon is not null), Is.True);
            Assert.That(alerts.Any(alert => alert.Dismissible), Is.True);
            Assert.That(alerts.Any(alert => alert.Text.Contains("\n")), Is.True);
            Assert.That(alerts.Any(alert => !alert.Enabled), Is.True);
            Assert.That(alerts.Any(alert => alert.BorderRadius >= 0), Is.True);
            Assert.That(alerts.All(alert => !alert.TabStop), Is.True);
        }));
    }

    [Test]
    public void FeedbackDemoCoversStage3TooltipMatrixAndLiveTiming()
    {
        using var form = new FeedbackDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var tooltips = GetTooltipComponents(form);
        Assert.That(tooltips.Length, Is.GreaterThanOrEqualTo(3), "Stage 3 requires default, semantic, and custom Tooltip examples.");

        var buttons = FindControls<Button>(form).ToArray();
        var defaultTarget = buttons.Single(button => button.AccessibleName == "Default tooltip target");
        var secondDefaultTarget = buttons.Single(button => button.AccessibleName == "Second default tooltip target");
        var semanticTarget = buttons.Single(button => button.AccessibleName == "Semantic tooltip target");
        var customTarget = buttons.Single(button => button.AccessibleName == "Custom tooltip target");
        var multilineTarget = buttons.Single(button => button.AccessibleName == "Multiline tooltip target");
        var longTarget = buttons.Single(button => button.AccessibleName == "Long tooltip target");

        var defaultTooltip = tooltips.Single(tooltip => !string.IsNullOrEmpty(tooltip.GetToolTip(defaultTarget)));
        var semanticTooltip = tooltips.Single(tooltip => !string.IsNullOrEmpty(tooltip.GetToolTip(semanticTarget)));
        var customTooltip = tooltips.Single(tooltip => !string.IsNullOrEmpty(tooltip.GetToolTip(customTarget)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(defaultTooltip.Variant, Is.EqualTo(BootstrapVariant.Dark));
            Assert.That(defaultTooltip.CustomColor, Is.EqualTo(Color.Empty));
            Assert.That(defaultTooltip.GetToolTip(secondDefaultTarget), Is.Not.Empty, "One Tooltip instance should serve multiple controls.");
            Assert.That(semanticTooltip.Variant, Is.Not.EqualTo(BootstrapVariant.Dark));
            Assert.That(semanticTooltip.CustomColor, Is.EqualTo(Color.Empty));
            Assert.That(customTooltip.CustomColor, Is.Not.EqualTo(Color.Empty));
            Assert.That(tooltips.Any(tooltip => tooltip.GetToolTip(multilineTarget).Contains("\n")), Is.True);
            Assert.That(tooltips.Any(tooltip => tooltip.GetToolTip(longTarget).Length >= 70), Is.True);
        }));

        var timingEditors = FindControls<NumericUpDown>(form).ToDictionary(editor => editor.AccessibleName ?? string.Empty, StringComparer.Ordinal);
        var active = FindControls<CheckBox>(form).Single(checkBox => checkBox.AccessibleName == "Tooltip Active");
        var showAlways = FindControls<CheckBox>(form).Single(checkBox => checkBox.AccessibleName == "Tooltip ShowAlways");

        timingEditors["Tooltip InitialDelay"].Value = 275;
        timingEditors["Tooltip ReshowDelay"].Value = 80;
        timingEditors["Tooltip AutoPopDelay"].Value = 4250;
        active.Checked = false;
        showAlways.Checked = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(defaultTooltip.InitialDelay, Is.EqualTo(275));
            Assert.That(defaultTooltip.ReshowDelay, Is.EqualTo(80));
            Assert.That(defaultTooltip.AutoPopDelay, Is.EqualTo(4250));
            Assert.That(defaultTooltip.Active, Is.False);
            Assert.That(defaultTooltip.ShowAlways, Is.True);
        }));
    }

    [Test]
    public void FeedbackDemoDismissAndRestoreReusesExistingAlerts()
    {
        using var form = new FeedbackDemoForm();
        form.Show();
        Application.DoEvents();

        var alerts = FindControls<BootstrapAlert>(form).ToArray();
        var dismissible = alerts.First(alert => alert.Dismissible && alert.Enabled);
        var closeButton = dismissible.Controls.OfType<Button>().Single();
        var restoreButton = FindControls<Button>(form).Single(button => button.Text == "Restore dismissed alerts");
        var initialCount = alerts.Length;

        for (var cycle = 0; cycle < 3; cycle++)
        {
            closeButton.PerformClick();
            Application.DoEvents();
            Assert.That(dismissible.Visible, Is.False, $"dismiss cycle {cycle}");

            restoreButton.PerformClick();
            Application.DoEvents();
            Assert.That(dismissible.Visible, Is.True, $"restore cycle {cycle}");
            Assert.That(FindControls<BootstrapAlert>(form).Count(), Is.EqualTo(initialCount));
        }
    }

    [Test]
    public void FeedbackDemoBadgesAlertsAndTooltipsRemainUsableAcrossRuntimeThemeSwitch()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var form = new FeedbackDemoForm();
            form.CreateControl();
            form.PerformLayout();
            var badges = FindControls<BootstrapBadge>(form).ToArray();
            var alerts = FindControls<BootstrapAlert>(form).ToArray();
            var tooltips = GetTooltipComponents(form);

            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            Assert.Multiple((Action)(() =>
            {
                foreach (var badge in badges)
                {
                    Assert.DoesNotThrow((Action)(() => badge.GetPreferredSize(Size.Empty)));
                }

                foreach (var alert in alerts)
                {
                    Assert.DoesNotThrow((Action)(() => alert.PerformLayout()));
                }

                Assert.That(tooltips, Is.Not.Empty);
                Assert.That(tooltips.All(tooltip => tooltip.Active), Is.True);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void FeedbackDemoOwnsAndDisposesTooltipComponents()
    {
        var form = new FeedbackDemoForm();
        var tooltips = GetTooltipComponents(form);
        Assert.That(tooltips, Is.Not.Empty);
        var disposeCounts = tooltips.ToDictionary(tooltip => tooltip, _ => 0);

        foreach (var tooltip in tooltips)
        {
            tooltip.Disposed += (_, _) => disposeCounts[tooltip]++;
        }

        form.Dispose();
        form.Dispose();

        Assert.That(disposeCounts.Values, Is.All.EqualTo(1));
    }

    [Test]
    public void FeedbackDemoCoversManagedPlacementAndInteractivePopover()
    {
        using var form = new FeedbackDemoForm();
        form.CreateControl();
        form.PerformLayout();
        var tooltips = GetTooltipComponents(form);
        var popovers = typeof(FeedbackDemoForm)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => field.FieldType == typeof(BootstrapPopover))
            .Select(field => (BootstrapPopover?)field.GetValue(form))
            .Where(popover => popover is not null)
            .Cast<BootstrapPopover>()
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltips.Any(tooltip => tooltip.Positioning == BootstrapTooltipPositioning.Managed), Is.True);
            Assert.That(tooltips.Any(tooltip => tooltip.Positioning == BootstrapTooltipPositioning.Managed && tooltip.Placement == BootstrapOverlayPlacement.Auto), Is.True);
            Assert.That(tooltips.Any(tooltip => tooltip.Positioning == BootstrapTooltipPositioning.Managed && tooltip.Placement != BootstrapOverlayPlacement.Auto), Is.True);
            Assert.That(popovers, Has.Length.EqualTo(1));
            Assert.That(popovers[0].Content, Is.Not.Null);
            Assert.That(FindControls<Button>(popovers[0].Content!).Any(button => button.TabStop), Is.True);
            Assert.That(FindControls<TextBox>(popovers[0].Content!).Any(textBox => textBox.TabStop), Is.True);
        }));

        var action = FindControls<Button>(popovers[0].Content!).Single(button => button.AccessibleName == "Popover apply action");
        var status = FindControls<Label>(form).Single(label => label.AccessibleName == "Popover interaction status");
        action.PerformClick();
        Assert.That(status.Text, Does.Contain("applied"));
    }

    [Test]
    public void FeedbackDemoDisposesPopoverBeforeItsCallerOwnedContentExactlyOnce()
    {
        var form = new FeedbackDemoForm();
        var popover = typeof(FeedbackDemoForm)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => field.FieldType == typeof(BootstrapPopover))
            .Select(field => (BootstrapPopover?)field.GetValue(form))
            .Single(value => value is not null)!;
        var content = popover.Content!;
        var popoverDisposed = 0;
        var contentDisposed = 0;
        popover.Disposed += (_, _) => popoverDisposed++;
        content.Disposed += (_, _) => contentDisposed++;

        form.Dispose();
        form.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(popoverDisposed, Is.EqualTo(1));
            Assert.That(contentDisposed, Is.EqualTo(1));
            Assert.That(content.Parent, Is.Null);
        }));
    }

    [Test]
    public void FeedbackDemoExercisesGlobalToastServiceAndNotificationCenterContract()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
            using var form = new FeedbackDemoForm();
            form.Show();
            Application.DoEvents();

            var service = (BootstrapToastService)typeof(FeedbackDemoForm)
                .GetField("_toastService", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(form)!;
            var unread = FindControls<Label>(form).Single(label => label.AccessibleName == "Global Toast unread count");
            var topMost = FindControls<CheckBox>(form).Single(checkBox => checkBox.AccessibleName == "Global Toast TopMost");
            Button Action(string name) => FindControls<Button>(form).Single(button => button.AccessibleName == name);

            Action("Show global Toast").PerformClick();
            Action("Show non-auto-hide Toast").PerformClick();
            Action("Burst 7 notifications").PerformClick();
            Application.DoEvents();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(service.UnreadCount, Is.EqualTo(9));
                Assert.That(unread.Text, Is.EqualTo("Unread: 9"));
            }));

            Action("Show history-disabled Toast").PerformClick();
            Assert.That(service.UnreadCount, Is.EqualTo(9));

            topMost.Checked = true;
            Assert.That(service.TopMost, Is.True);
            topMost.Checked = false;
            Assert.That(service.TopMost, Is.False);

            var placements = new[]
            {
                ("Set global Toast TopLeft", BootstrapToastPlacement.TopLeft),
                ("Set global Toast TopRight", BootstrapToastPlacement.TopRight),
                ("Set global Toast BottomLeft", BootstrapToastPlacement.BottomLeft),
                ("Set global Toast BottomRight", BootstrapToastPlacement.BottomRight)
            };
            foreach (var placement in placements)
            {
                Action(placement.Item1).PerformClick();
                Assert.That(service.Placement, Is.EqualTo(placement.Item2));
            }

            Action("Open notification center").PerformClick();
            Assert.That(service.IsNotificationCenterVisible, Is.True);
            Action("Mark all global notifications read").PerformClick();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(service.UnreadCount, Is.Zero);
                Assert.That(unread.Text, Is.EqualTo("Unread: 0"));
            }));

            Action("Clear global notification history").PerformClick();
            Assert.That(service.GetHistory(), Is.Empty);
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private static BootstrapTooltip[] GetTooltipComponents(FeedbackDemoForm form)
    {
        return typeof(FeedbackDemoForm)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => field.FieldType == typeof(BootstrapTooltip))
            .Select(field => (BootstrapTooltip?)field.GetValue(form))
            .Where(tooltip => tooltip is not null)
            .Cast<BootstrapTooltip>()
            .ToArray();
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
