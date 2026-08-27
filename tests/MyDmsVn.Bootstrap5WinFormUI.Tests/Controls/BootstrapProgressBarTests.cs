using System;
using System.Drawing;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapProgressBarTests
{
    [Test]
    public void DefaultsMatchPhase11Contract()
    {
        using var progress = new BootstrapProgressBar();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(progress.Minimum, Is.EqualTo(0));
            Assert.That(progress.Maximum, Is.EqualTo(100));
            Assert.That(progress.Value, Is.EqualTo(0));
            Assert.That(progress.Percentage, Is.EqualTo(0));
            Assert.That(progress.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(progress.CustomColor, Is.EqualTo(Color.Empty));
            Assert.That(progress.BorderRadius, Is.EqualTo(-1));
            Assert.That(progress.ShowText, Is.False);
            Assert.That(progress.TextFormat, Is.EqualTo("{0}%"));
            Assert.That(progress.Striped, Is.False);
            Assert.That(progress.Animated, Is.False);
            Assert.That(progress.AnimationDuration, Is.EqualTo(TimeSpan.FromMilliseconds(600)));
            Assert.That(progress.Indeterminate, Is.False);
            Assert.That(progress.TabStop, Is.False);
            Assert.That(progress.AccessibleRole, Is.EqualTo(System.Windows.Forms.AccessibleRole.ProgressBar));
        }));
    }

    [Test]
    public void PercentageUsesConfiguredRange()
    {
        using var progress = new BootstrapProgressBar
        {
            Maximum = 120,
            Minimum = 20,
            Value = 70
        };

        Assert.That(progress.Percentage, Is.EqualTo(50));
    }

    [Test]
    public void DirectValueOutsideRangeIsRejected()
    {
        using var progress = new BootstrapProgressBar();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.Value = -1));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.Value = 101));
    }

    [Test]
    public void RangeEndpointsMustRemainOrdered()
    {
        using var progress = new BootstrapProgressBar();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.Minimum = 100));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.Maximum = 0));
    }

    [Test]
    public void RangeChangesClampExistingValueIntoNewRange()
    {
        using var progress = new BootstrapProgressBar { Value = 80 };

        progress.Maximum = 60;
        Assert.That(progress.Value, Is.EqualTo(60));

        progress.Minimum = 50;
        progress.Value = 55;
        progress.Minimum = 58;
        Assert.That(progress.Value, Is.EqualTo(58));
    }

    [Test]
    public void BorderRadiusRejectsValuesBelowThemeSentinel()
    {
        using var progress = new BootstrapProgressBar();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.BorderRadius = -2));
        Assert.DoesNotThrow((TestDelegate)(() => progress.BorderRadius = -1));
        Assert.DoesNotThrow((TestDelegate)(() => progress.BorderRadius = 0));
    }

    [Test]
    public void AnimationDurationRejectsNonPositiveValues()
    {
        using var progress = new BootstrapProgressBar();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.AnimationDuration = TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.AnimationDuration = TimeSpan.FromMilliseconds(-1)));
    }

    [Test]
    public void VariantRejectsUnknownValue()
    {
        using var progress = new BootstrapProgressBar();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.Variant = (BootstrapVariant)999));
    }

    [Test]
    public void TextFormatRejectsNull()
    {
        using var progress = new BootstrapProgressBar();

        Assert.Throws<ArgumentNullException>((Action)(() => progress.TextFormat = null!));
    }

    [Test]
    public void AnimateToRejectsTargetOutsideRange()
    {
        using var progress = new BootstrapProgressBar();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.AnimateTo(-1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => progress.AnimateTo(101)));
    }

    [Test]
    public void AnimateToCompletesImmediatelyWhenReducedMotionIsEnabled()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var progress = new BootstrapProgressBar { Value = 15 };
            progress.CreateControl();

            progress.AnimateTo(85);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(progress.Value, Is.EqualTo(85));
                Assert.That(progress.Percentage, Is.EqualTo(85));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void AnimateToInIndeterminateModeStillUpdatesLogicalValueWithoutFiniteTransition()
    {
        using var progress = new BootstrapProgressBar
        {
            Indeterminate = true,
            Value = 10
        };

        progress.AnimateTo(65);

        Assert.That(progress.Value, Is.EqualTo(65));
    }

    [Test]
    public void ZeroProgressPaintsThemeTrack()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            BootstrapThemeManager.CurrentTheme = theme;

            using var progress = new BootstrapProgressBar
            {
                Size = new Size(120, 20),
                Value = 0
            };
            using var bitmap = new Bitmap(progress.Width, progress.Height);

            progress.DrawToBitmap(bitmap, progress.ClientRectangle);

            Assert.That(bitmap.GetPixel(progress.Width / 2, progress.Height / 2).ToArgb(), Is.EqualTo(theme.Colors.SurfaceSecondary.ToArgb()));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void CustomColorOverridesVariantForDeterminateFill()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

            using var progress = new BootstrapProgressBar
            {
                Size = new Size(120, 20),
                Value = 100,
                Variant = BootstrapVariant.Danger,
                CustomColor = Color.Magenta
            };
            using var bitmap = new Bitmap(progress.Width, progress.Height);

            progress.DrawToBitmap(bitmap, progress.ClientRectangle);

            Assert.That(bitmap.GetPixel(progress.Width / 2, progress.Height / 2).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }
}
