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
    public void FeedbackDemoBadgesRemainUsableAcrossRuntimeThemeSwitch()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var form = new FeedbackDemoForm();
            form.CreateControl();
            form.PerformLayout();
            var badges = FindControls<BootstrapBadge>(form).ToArray();

            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            Assert.Multiple((Action)(() =>
            {
                foreach (var badge in badges)
                {
                    Assert.DoesNotThrow((Action)(() => badge.GetPreferredSize(Size.Empty)));
                }
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
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
