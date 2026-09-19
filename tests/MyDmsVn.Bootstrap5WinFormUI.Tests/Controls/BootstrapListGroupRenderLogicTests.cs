using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapListGroupRenderLogicTests
{
    [TestCase(false, true, true, true, true, 4)]
    [TestCase(true, true, true, true, true, 3)]
    [TestCase(true, false, true, true, true, 2)]
    [TestCase(true, false, true, false, true, 1)]
    [TestCase(true, false, false, true, true, 0)]
    public void VisualStateUsesDocumentedPrecedence(
        bool enabled,
        bool active,
        bool actionable,
        bool pressed,
        bool hovered,
        int expected)
    {
        Assert.That(
            (int)BootstrapListGroupRenderLogic.ResolveState(enabled, active, actionable, pressed, hovered),
            Is.EqualTo(expected));
    }

    [Test]
    public void NeutralAndEveryContextualVariantProduceReadableThemeDerivedPalettes()
    {
        foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
        {
            var colors = BootstrapThemeColors.CreateDefault(mode);
            var neutral = BootstrapListGroupRenderLogic.ResolvePalette(colors, null, BootstrapListGroupVisualState.Neutral);
            Assert.That(neutral.Surface, Is.EqualTo(colors.Surface));

            foreach (var variant in ((BootstrapVariant[])Enum.GetValues(typeof(BootstrapVariant))))
            {
                var palette = BootstrapListGroupRenderLogic.ResolvePalette(colors, variant, BootstrapListGroupVisualState.Neutral);
                Assert.That(palette.Surface, Is.Not.EqualTo(Color.Empty), variant.ToString());
                Assert.That(ColorUtil.GetContrastRatio(palette.Foreground, palette.Surface), Is.GreaterThanOrEqualTo(4.5d), variant.ToString());
            }

            var active = BootstrapListGroupRenderLogic.ResolvePalette(colors, BootstrapVariant.Danger, BootstrapListGroupVisualState.Active);
            var disabled = BootstrapListGroupRenderLogic.ResolvePalette(colors, BootstrapVariant.Danger, BootstrapListGroupVisualState.Disabled);
            Assert.That(active.Surface, Is.EqualTo(colors.Primary));
            Assert.That(disabled.Foreground, Is.EqualTo(colors.MutedText));
        }
    }

    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(192)]
    public void MetricsScaleThroughSharedDpiInfrastructure(int dpi)
    {
        var metrics = BootstrapThemeMetrics.Default;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapListGroupRenderLogic.ResolveRadius(metrics, -1, dpi), Is.EqualTo(DpiScaler.Scale(metrics.Radius, dpi)));
            Assert.That(BootstrapListGroupRenderLogic.ResolveRadius(metrics, 3, dpi), Is.EqualTo(DpiScaler.Scale(3, dpi)));
            Assert.That(BootstrapListGroupRenderLogic.GetSeamOverlap(metrics, dpi), Is.EqualTo(DpiScaler.Scale(metrics.BorderWidth, dpi)));
            Assert.That(BootstrapListGroupRenderLogic.GetContentPadding(metrics, dpi), Is.EqualTo(DpiScaler.Scale(new Padding(metrics.SpacingMD, metrics.SpacingSM, metrics.SpacingMD, metrics.SpacingSM), dpi)));
        }));
    }

    [Test]
    public void CornerRolesFollowOrientationAndFlushRules()
    {
        const int radius = 6;
        Assert.That(BootstrapListGroupRenderLogic.GetCornerRadius(Orientation.Vertical, 0, 1, false, radius), Is.EqualTo(new CornerRadius(radius)));
        Assert.That(BootstrapListGroupRenderLogic.GetCornerRadius(Orientation.Vertical, 0, 3, false, radius), Is.EqualTo(new CornerRadius(radius, radius, 0, 0)));
        Assert.That(BootstrapListGroupRenderLogic.GetCornerRadius(Orientation.Vertical, 1, 3, false, radius), Is.EqualTo(CornerRadius.Empty));
        Assert.That(BootstrapListGroupRenderLogic.GetCornerRadius(Orientation.Vertical, 2, 3, false, radius), Is.EqualTo(new CornerRadius(0, 0, radius, radius)));
        Assert.That(BootstrapListGroupRenderLogic.GetCornerRadius(Orientation.Vertical, 0, 1, true, radius), Is.EqualTo(CornerRadius.Empty));

        Assert.That(BootstrapListGroupRenderLogic.GetCornerRadius(Orientation.Horizontal, 0, 3, false, radius), Is.EqualTo(new CornerRadius(radius, 0, 0, radius)));
        Assert.That(BootstrapListGroupRenderLogic.GetCornerRadius(Orientation.Horizontal, 2, 3, false, radius), Is.EqualTo(new CornerRadius(0, radius, radius, 0)));
        Assert.That(BootstrapListGroupRenderLogic.GetCornerRadius(Orientation.Horizontal, 0, 1, true, radius), Is.EqualTo(new CornerRadius(radius)));
    }

    [Test]
    public void TinyBoundsNormalizeCornerRadiusAndPreferredSizeNeverGoesNegative()
    {
        var corners = BootstrapListGroupRenderLogic.NormalizeCorners(new CornerRadius(10), new Size(1, 1));
        Assert.That(new[] { corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft }.All(value => value <= 0.5f), Is.True);

        Assert.That(
            BootstrapListGroupRenderLogic.GetPreferredSize(new Size(-1, -2), new Size(-3, -4), new Padding(4)),
            Is.EqualTo(new Size(8, 8)));
    }
}
