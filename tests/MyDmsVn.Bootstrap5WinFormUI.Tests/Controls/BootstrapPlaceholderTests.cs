using System;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
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

    [Test]
    public void NoneOwnsNoLoopAndAnimationChangesPreserveOtherPublicState()
    {
        using var placeholder = new LifecycleProbePlaceholder
        {
            AutoSize = false,
            Size = new Size(222, 33),
            PlaceholderSize = BootstrapPlaceholderSize.Small,
            Variant = BootstrapVariant.Success,
            CustomColor = Color.MediumPurple,
            BorderRadius = 7,
            AnimationDuration = TimeSpan.FromSeconds(5)
        };
        placeholder.EnsureHandle();

        Assert.That(GetAnimationLoop(placeholder), Is.Null);

        placeholder.Animation = BootstrapPlaceholderAnimation.Glow;
        placeholder.Animation = BootstrapPlaceholderAnimation.Wave;

        Assert.Multiple((Action)delegate
        {
            Assert.That(placeholder.Size, Is.EqualTo(new Size(222, 33)));
            Assert.That(placeholder.PlaceholderSize, Is.EqualTo(BootstrapPlaceholderSize.Small));
            Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(placeholder.CustomColor, Is.EqualTo(Color.MediumPurple));
            Assert.That(placeholder.BorderRadius, Is.EqualTo(7));
            Assert.That(placeholder.AnimationDuration, Is.EqualTo(TimeSpan.FromSeconds(5)));
        });

        placeholder.AnimationDuration = TimeSpan.FromSeconds(7);
        Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.Wave));

        placeholder.Animation = BootstrapPlaceholderAnimation.None;
        Assert.That(GetAnimationLoop(placeholder), Is.Null);
        Assert.DoesNotThrow((Action)placeholder.RecreateHandleForTest);
        Assert.That(GetAnimationLoop(placeholder), Is.Null);
    }

    [Test]
    public void HandleRecreationRetainsLoopAndResumesCapturedProgress()
    {
        using var placeholder = new LifecycleProbePlaceholder
        {
            AnimationDuration = TimeSpan.FromSeconds(5),
            Animation = BootstrapPlaceholderAnimation.Glow
        };
        placeholder.EnsureHandle();
        var loop = GetAnimationLoop(placeholder);
        Assert.That(loop, Is.Not.Null);
        Assert.That(PumpUntil(() => loop!.Progress > 0.01, TimeSpan.FromSeconds(2)), Is.True);
        var progressBeforeRecreation = loop!.Progress;

        placeholder.RecreateHandleForTest();
        var loopAfterRecreation = GetAnimationLoop(placeholder);

        Assert.Multiple((Action)delegate
        {
            Assert.That(loopAfterRecreation, Is.SameAs(loop));
            Assert.That(loopAfterRecreation!.Progress, Is.GreaterThan(0.0));
        });
        Assert.That(PumpUntil(() => loopAfterRecreation!.Progress > progressBeforeRecreation, TimeSpan.FromSeconds(2)), Is.True);
    }

    [Test]
    public void ReducedMotionKeepsAnimationSelectionAndStableGeometryAcrossThemeChanges()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
        using var placeholder = new LifecycleProbePlaceholder
        {
            Animation = BootstrapPlaceholderAnimation.Wave
        };
        placeholder.EnsureHandle();
        var expectedSize = placeholder.Size;

        Assert.DoesNotThrow((Action)(() => placeholder.PaintSeveralFrames(3)));
        Assert.Multiple((Action)delegate
        {
            Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.Wave));
            Assert.That(placeholder.Size, Is.EqualTo(expectedSize));
        });

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark, reducedMotion: false);
        Assert.DoesNotThrow((Action)(() => placeholder.PaintSeveralFrames(3)));
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark, reducedMotion: true);
        Assert.DoesNotThrow((Action)(() => placeholder.PaintSeveralFrames(3)));
        Assert.Multiple((Action)delegate
        {
            Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.Wave));
            Assert.That(placeholder.Size, Is.EqualTo(expectedSize));
        });
    }

    [Test]
    public void WavePaintHandlesExtremeGeometryThemeAndColorCombinations()
    {
        var sizes = new[]
        {
            new Size(1, 1),
            new Size(2, 2),
            new Size(40, 18),
            new Size(400, 8),
            new Size(8, 400)
        };

        foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode, reducedMotion: true);
            foreach (var size in sizes)
            {
                foreach (var radius in new[] { 0, -1, 999 })
                {
                    using var placeholder = new LifecycleProbePlaceholder
                    {
                        AutoSize = false,
                        Size = size,
                        BorderRadius = radius,
                        Animation = BootstrapPlaceholderAnimation.Wave,
                        Enabled = radius != -1,
                        CustomColor = radius == 999 ? Color.CornflowerBlue : Color.Empty
                    };
                    placeholder.EnsureHandle();
                    Assert.DoesNotThrow((Action)(() => placeholder.PaintSeveralFrames(1)));
                }
            }
        }
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

    private static BootstrapLoopAnimation? GetAnimationLoop(BootstrapPlaceholder placeholder)
    {
        var field = typeof(BootstrapPlaceholder).GetField("_animationLoop", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (BootstrapLoopAnimation?)field!.GetValue(placeholder);
    }

    private static bool PumpUntil(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            Application.DoEvents();
            if (condition())
            {
                return true;
            }

            Thread.Sleep(10);
        }

        Application.DoEvents();
        return condition();
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

    private sealed class LifecycleProbePlaceholder : BootstrapPlaceholder
    {
        public void EnsureHandle()
        {
            _ = Handle;
        }

        public void RecreateHandleForTest()
        {
            RecreateHandle();
        }

        public void PaintSeveralFrames(int count)
        {
            using var bitmap = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            using var graphics = Graphics.FromImage(bitmap);
            for (var index = 0; index < count; index++)
            {
                OnPaint(new PaintEventArgs(graphics, ClientRectangle));
            }
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
