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
public sealed class BootstrapBadgeTests
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
    public void DefaultsMatchStage1ContractAndConstructionIsDesignerSafe()
    {
        Assert.DoesNotThrow((Action)(() =>
        {
            using var badge = new BootstrapBadge();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(badge.Text, Is.EqualTo(string.Empty));
                Assert.That(badge.AutoSize, Is.True);
                Assert.That(badge.TabStop, Is.False);
                Assert.That(badge.Variant, Is.EqualTo(BootstrapVariant.Primary));
                Assert.That(badge.CustomColor, Is.EqualTo(Color.Empty));
                Assert.That(badge.Pill, Is.False);
                Assert.That(badge.BorderRadius, Is.EqualTo(-1));
                Assert.That(badge.AccessibleRole, Is.EqualTo(AccessibleRole.StaticText));
            }));
        }));
    }

    [Test]
    public void TextNormalizesNullAndPreferredWidthTracksContent()
    {
        using var badge = new BootstrapBadge();
        badge.Text = "Long status badge";
        var populatedWidth = badge.GetPreferredSize(Size.Empty).Width;

        badge.Text = null!;
        var emptyWidth = badge.GetPreferredSize(Size.Empty).Width;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(badge.Text, Is.EqualTo(string.Empty));
            Assert.That(populatedWidth, Is.GreaterThan(emptyWidth));
        }));
    }

    [Test]
    public void BorderRadiusRejectsValuesBelowThemeSentinel()
    {
        using var badge = new BootstrapBadge();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => badge.BorderRadius = -2));
        Assert.DoesNotThrow((Action)(() => badge.BorderRadius = -1));
        Assert.DoesNotThrow((Action)(() => badge.BorderRadius = 0));
    }

    [Test]
    public void VariantRejectsUndefinedValues()
    {
        using var badge = new BootstrapBadge();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => badge.Variant = (BootstrapVariant)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => badge.Variant = (BootstrapVariant)99));
    }

    [Test]
    public void CustomColorAcceptsEmptyOrOpaqueAndRejectsAlphaColors()
    {
        using var badge = new BootstrapBadge();
        var opaque = Color.FromArgb(255, 111, 66, 193);

        Assert.DoesNotThrow((Action)(() => badge.CustomColor = Color.Empty));
        Assert.DoesNotThrow((Action)(() => badge.CustomColor = opaque));
        Assert.That(badge.CustomColor, Is.EqualTo(opaque));
        Assert.Throws<ArgumentException>((Action)(() => badge.CustomColor = Color.Transparent));
        Assert.Throws<ArgumentException>((Action)(() => badge.CustomColor = Color.FromArgb(128, 111, 66, 193)));
    }

    [Test]
    public void BadgeUsesDoubleBufferedNonSelectableCustomPainting()
    {
        using var badge = new StyleProbeBadge();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(badge.HasStyle(ControlStyles.UserPaint), Is.True);
            Assert.That(badge.HasStyle(ControlStyles.OptimizedDoubleBuffer), Is.True);
            Assert.That(badge.HasStyle(ControlStyles.ResizeRedraw), Is.True);
            Assert.That(badge.HasStyle(ControlStyles.Selectable), Is.False);
        }));
    }

    [Test]
    public void RuntimeThemeChangesKeepThemeFontUsableAndDisposalDetachesSubscription()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var badge = new BootstrapBadge();

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        Assert.DoesNotThrow((Action)(() => badge.GetPreferredSize(Size.Empty)));

        badge.Dispose();

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
    }

    [Test]
    public void CallerOwnedFontRemainsCallerOwnedAcrossThemeSwitchAndDisposal()
    {
        using var callerFont = new Font("Segoe UI", 10f, FontStyle.Italic);
        var badge = new BootstrapBadge
        {
            Font = callerFont
        };

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Assert.That(badge.Font, Is.SameAs(callerFont));

        badge.Dispose();

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.DoesNotThrow((Action)(() => graphics.MeasureString("x", callerFont)));
    }

    [Test]
    public void ThemeOwnedFontIsReleasedWhenBadgeIsDisposed()
    {
        var badge = new BootstrapBadge();
        var ownedFont = badge.Font;
        badge.Dispose();

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.Catch((Action)(() => graphics.MeasureString("x", ownedFont)));
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private sealed class StyleProbeBadge : BootstrapBadge
    {
        public bool HasStyle(ControlStyles style)
        {
            return GetStyle(style);
        }
    }
}
