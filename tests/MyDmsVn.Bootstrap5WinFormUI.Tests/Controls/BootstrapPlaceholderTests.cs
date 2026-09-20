using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapPlaceholderTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void EnumValuesMatchBootstrapPlaceholderContract()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That((int)BootstrapPlaceholderSize.ExtraSmall, Is.EqualTo(0));
            Assert.That((int)BootstrapPlaceholderSize.Small, Is.EqualTo(1));
            Assert.That((int)BootstrapPlaceholderSize.Default, Is.EqualTo(2));
            Assert.That((int)BootstrapPlaceholderSize.Large, Is.EqualTo(3));
            Assert.That((int)BootstrapPlaceholderAnimation.None, Is.EqualTo(0));
            Assert.That((int)BootstrapPlaceholderAnimation.Glow, Is.EqualTo(1));
            Assert.That((int)BootstrapPlaceholderAnimation.Wave, Is.EqualTo(2));
        }));
    }

    [Test]
    public void DefaultsMatchPlaceholderContract()
    {
        using var placeholder = new BootstrapPlaceholder();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(placeholder.PlaceholderSize, Is.EqualTo(BootstrapPlaceholderSize.Default));
            Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.None));
            Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Secondary));
            Assert.That(placeholder.CustomColor, Is.EqualTo(Color.Empty));
            Assert.That(placeholder.BorderRadius, Is.Zero);
            Assert.That(placeholder.AnimationDuration, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(placeholder.AutoSize, Is.True);
            Assert.That(placeholder.BackColor, Is.EqualTo(Color.Transparent));
            Assert.That(placeholder.TabStop, Is.False);
            Assert.That(placeholder.AccessibleRole, Is.EqualTo(AccessibleRole.None));
            Assert.That(placeholder.Cursor, Is.SameAs(Cursors.WaitCursor));
        }));
    }

    [Test]
    public void InvalidAssignmentsThrowBeforeMutatingState()
    {
        using var placeholder = new BootstrapPlaceholder
        {
            PlaceholderSize = BootstrapPlaceholderSize.Small,
            Animation = BootstrapPlaceholderAnimation.Glow,
            Variant = BootstrapVariant.Success,
            CustomColor = Color.MediumPurple,
            BorderRadius = 8,
            AnimationDuration = TimeSpan.FromMilliseconds(750)
        };

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.PlaceholderSize = (BootstrapPlaceholderSize)(-1)));
        Assert.That(placeholder.PlaceholderSize, Is.EqualTo(BootstrapPlaceholderSize.Small));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.PlaceholderSize = (BootstrapPlaceholderSize)99));
        Assert.That(placeholder.PlaceholderSize, Is.EqualTo(BootstrapPlaceholderSize.Small));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.Animation = (BootstrapPlaceholderAnimation)(-1)));
        Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.Glow));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.Animation = (BootstrapPlaceholderAnimation)99));
        Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.Glow));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.Variant = (BootstrapVariant)(-1)));
        Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Success));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.Variant = (BootstrapVariant)99));
        Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Success));

        Assert.Throws<ArgumentException>((Action)(() => placeholder.CustomColor = Color.FromArgb(128, 12, 34, 56)));
        Assert.That(placeholder.CustomColor, Is.EqualTo(Color.MediumPurple));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.BorderRadius = -2));
        Assert.That(placeholder.BorderRadius, Is.EqualTo(8));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.AnimationDuration = TimeSpan.Zero));
        Assert.That(placeholder.AnimationDuration, Is.EqualTo(TimeSpan.FromMilliseconds(750)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.AnimationDuration = TimeSpan.FromTicks(-1)));
        Assert.That(placeholder.AnimationDuration, Is.EqualTo(TimeSpan.FromMilliseconds(750)));
    }

    [Test]
    public void PresentationChangesPreserveCallerOwnedExplicitBounds()
    {
        using var placeholder = new BootstrapPlaceholder
        {
            AutoSize = false,
            Size = new Size(237, 41)
        };
        var expected = placeholder.Size;

        placeholder.PlaceholderSize = BootstrapPlaceholderSize.Large;
        Assert.That(placeholder.Size, Is.EqualTo(expected));
        placeholder.Variant = BootstrapVariant.Danger;
        Assert.That(placeholder.Size, Is.EqualTo(expected));
        placeholder.CustomColor = Color.CornflowerBlue;
        Assert.That(placeholder.Size, Is.EqualTo(expected));
        placeholder.BorderRadius = 11;
        Assert.That(placeholder.Size, Is.EqualTo(expected));
        placeholder.AnimationDuration = TimeSpan.FromSeconds(3);
        Assert.That(placeholder.Size, Is.EqualTo(expected));
    }

    [Test]
    public void AutoSizeUsesIntrinsicWidthAndPlaceholderSizeHeight()
    {
        using var placeholder = new BootstrapPlaceholder();

        var defaultSize = placeholder.GetPreferredSize(Size.Empty);
        placeholder.PlaceholderSize = BootstrapPlaceholderSize.ExtraSmall;
        var extraSmallSize = placeholder.GetPreferredSize(Size.Empty);
        placeholder.PlaceholderSize = BootstrapPlaceholderSize.Large;
        var largeSize = placeholder.GetPreferredSize(Size.Empty);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(defaultSize.Width, Is.EqualTo(100));
            Assert.That(extraSmallSize.Width, Is.EqualTo(100));
            Assert.That(largeSize.Width, Is.EqualTo(100));
            Assert.That(extraSmallSize.Height, Is.EqualTo((int)Math.Ceiling(placeholder.Font.Height * 0.6)));
            Assert.That(largeSize.Height, Is.EqualTo((int)Math.Ceiling(placeholder.Font.Height * 1.2)));
            Assert.That(largeSize.Height, Is.GreaterThan(defaultSize.Height));
            Assert.That(placeholder.Size, Is.EqualTo(largeSize));
        }));
    }

    [Test]
    public void CallerAssignedFontSurvivesThemeChangesAndPlaceholderDisposal()
    {
        using var callerFont = new Font("Segoe UI", 10f, FontStyle.Italic);
        var placeholder = new BootstrapPlaceholder
        {
            Font = callerFont
        };

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Assert.That(placeholder.Font, Is.SameAs(callerFont));

        placeholder.Dispose();

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.DoesNotThrow((Action)(() => graphics.MeasureString("x", callerFont)));
    }

    [Test]
    public void ThemeOwnedFontAndPreferredSizeFollowRuntimeBodyTypography()
    {
        using var placeholder = new BootstrapPlaceholder();
        var originalFont = placeholder.Font;
        var originalSize = placeholder.Size;
        var typography = CreateTypography(new BootstrapFontToken("Segoe UI", 15f, FontStyle.Bold));

        BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
            BootstrapThemeMode.Dark,
            BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Dark),
            BootstrapThemeMetrics.Default,
            typography);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(placeholder.Font, Is.Not.SameAs(originalFont));
            Assert.That(placeholder.Font.SizeInPoints, Is.EqualTo(15f).Within(0.1f));
            Assert.That(placeholder.Font.Style, Is.EqualTo(FontStyle.Bold));
            Assert.That(placeholder.Size.Height, Is.GreaterThan(originalSize.Height));
        }));
    }

    [Test]
    public void ThemeAndDpiRefreshPreservePresentationPropertiesAndExplicitBounds()
    {
        using var placeholder = new DpiProbePlaceholder
        {
            AutoSize = false,
            Size = new Size(213, 37),
            Variant = BootstrapVariant.Success,
            CustomColor = Color.MediumPurple
        };
        var expectedBounds = placeholder.Bounds;

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        placeholder.SimulateDpiChangedAfterParent();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(placeholder.Bounds, Is.EqualTo(expectedBounds));
            Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(placeholder.CustomColor, Is.EqualTo(Color.MediumPurple));
        }));
    }

    [Test]
    public void StaticPaintHandlesRadiusColorAndEnabledCombinations()
    {
        var cases = new[]
        {
            new PaintCase(0, true, BootstrapVariant.Secondary, Color.Empty),
            new PaintCase(-1, true, BootstrapVariant.Primary, Color.Empty),
            new PaintCase(999, true, BootstrapVariant.Success, Color.Empty),
            new PaintCase(8, false, BootstrapVariant.Danger, Color.Empty),
            new PaintCase(4, true, BootstrapVariant.Warning, Color.CornflowerBlue)
        };

        foreach (var item in cases)
        {
            using var placeholder = new PaintProbePlaceholder
            {
                AutoSize = false,
                Size = new Size(31, 17),
                BorderRadius = item.BorderRadius,
                Enabled = item.Enabled,
                Variant = item.Variant,
                CustomColor = item.CustomColor
            };
            using var bitmap = new Bitmap(placeholder.Width, placeholder.Height);
            using var graphics = Graphics.FromImage(bitmap);

            Assert.DoesNotThrow((Action)(() => placeholder.Render(graphics)));
            Assert.That(bitmap.GetPixel(placeholder.Width / 2, placeholder.Height / 2).A, Is.GreaterThan(0));
        }
    }

    [Test]
    public void ConstructionAndDisposalPairThemeSubscription()
    {
        var baseline = GetThemeSubscriptionCount();
        var placeholder = new BootstrapPlaceholder();

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline + 1));

        placeholder.Dispose();

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline));
    }

    private static BootstrapThemeTypography CreateTypography(BootstrapFontToken body)
    {
        var defaults = BootstrapThemeTypography.Default;
        return new BootstrapThemeTypography(
            body,
            defaults.BodySmall,
            defaults.Label,
            defaults.HeadingSmall,
            defaults.HeadingMedium);
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private sealed class DpiProbePlaceholder : BootstrapPlaceholder
    {
        public void SimulateDpiChangedAfterParent()
        {
            OnDpiChangedAfterParent(EventArgs.Empty);
        }
    }

    private sealed class PaintProbePlaceholder : BootstrapPlaceholder
    {
        public void Render(Graphics graphics)
        {
            OnPaint(new PaintEventArgs(graphics, ClientRectangle));
        }
    }

    private readonly struct PaintCase
    {
        public PaintCase(int borderRadius, bool enabled, BootstrapVariant variant, Color customColor)
        {
            BorderRadius = borderRadius;
            Enabled = enabled;
            Variant = variant;
            CustomColor = customColor;
        }

        public int BorderRadius { get; }
        public bool Enabled { get; }
        public BootstrapVariant Variant { get; }
        public Color CustomColor { get; }
    }
}
