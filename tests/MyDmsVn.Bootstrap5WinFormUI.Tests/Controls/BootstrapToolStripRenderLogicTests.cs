using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapToolStripRenderLogicTests
{
    [TestCase(96)]
    [TestCase(144)]
    [TestCase(192)]
    public void ResolveMetricsScalesEveryCustomMetricFromDpi(int dpi)
    {
        var actual = BootstrapToolStripRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.BorderWidth, Is.EqualTo(DpiScaler.Scale(1f, dpi)));
            Assert.That(actual.ItemHorizontalPadding, Is.EqualTo(DpiScaler.Scale(8, dpi)));
            Assert.That(actual.ItemVerticalPadding, Is.EqualTo(DpiScaler.Scale(4, dpi)));
            Assert.That(actual.ImageSize, Is.EqualTo(DpiScaler.Scale(16, dpi)));
            Assert.That(actual.SeparatorInset, Is.EqualTo(DpiScaler.Scale(8, dpi)));
            Assert.That(actual.ArrowSize, Is.EqualTo(DpiScaler.Scale(4, dpi)));
            Assert.That(actual.GripDotSize, Is.EqualTo(DpiScaler.Scale(2, dpi)));
        }));
    }

    [Test]
    public void ResolvePaletteUsesSurfaceKindAndDeterministicStatePrecedence()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);
        var normalToolbar = BootstrapToolStripRenderLogic.ResolvePalette(
            colors, BootstrapVariant.Success, BootstrapToolStripSurfaceKind.ToolBar,
            enabled: true, selected: false, pressed: false, @checked: false);
        var selected = BootstrapToolStripRenderLogic.ResolvePalette(
            colors, BootstrapVariant.Success, BootstrapToolStripSurfaceKind.DropDown,
            enabled: true, selected: true, pressed: false, @checked: false);
        var checkedAndSelected = BootstrapToolStripRenderLogic.ResolvePalette(
            colors, BootstrapVariant.Success, BootstrapToolStripSurfaceKind.DropDown,
            enabled: true, selected: true, pressed: false, @checked: true);
        var pressedCheckedSelected = BootstrapToolStripRenderLogic.ResolvePalette(
            colors, BootstrapVariant.Success, BootstrapToolStripSurfaceKind.DropDown,
            enabled: true, selected: true, pressed: true, @checked: true);
        var disabledPressed = BootstrapToolStripRenderLogic.ResolvePalette(
            colors, BootstrapVariant.Success, BootstrapToolStripSurfaceKind.DropDown,
            enabled: false, selected: true, pressed: true, @checked: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(normalToolbar.Background, Is.EqualTo(colors.SurfaceSecondary));
            Assert.That(selected.Background, Is.EqualTo(ColorUtil.Blend(colors.Success, colors.Surface, 0.12f)));
            Assert.That(checkedAndSelected.Background, Is.EqualTo(ColorUtil.Blend(colors.Success, colors.Surface, 0.16f)));
            Assert.That(pressedCheckedSelected.Background, Is.EqualTo(ColorUtil.Blend(colors.Success, colors.Surface, 0.22f)));
            Assert.That(disabledPressed.Background, Is.EqualTo(colors.Surface));
            Assert.That(disabledPressed.Foreground, Is.EqualTo(colors.MutedText));
            Assert.That(disabledPressed.Accent, Is.EqualTo(colors.Disabled));
        }));
    }

    [Test]
    public void ResolvePaletteUsesCurrentThemeTokensInLightAndDarkModes()
    {
        var light = BootstrapToolStripRenderLogic.ResolvePalette(
            BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light), BootstrapVariant.Primary,
            BootstrapToolStripSurfaceKind.DropDown, true, false, false, false);
        var dark = BootstrapToolStripRenderLogic.ResolvePalette(
            BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Dark), BootstrapVariant.Primary,
            BootstrapToolStripSurfaceKind.DropDown, true, false, false, false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(light.Background, Is.Not.EqualTo(dark.Background));
            Assert.That(light.Foreground, Is.Not.EqualTo(dark.Foreground));
            Assert.That(light.Accent, Is.Not.EqualTo(dark.Accent));
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ResolveSeparatorLineHonorsNativeOrientation(bool vertical)
    {
        var bounds = new Rectangle(0, 0, 40, 30);
        var line = BootstrapToolStripRenderLogic.ResolveSeparatorLine(bounds, 5, vertical);

        if (vertical)
        {
            Assert.Multiple((Action)(() =>
            {
                Assert.That(line.Start.X, Is.EqualTo(line.End.X));
                Assert.That(line.Start.Y, Is.LessThan(line.End.Y));
            }));
        }
        else
        {
            Assert.Multiple((Action)(() =>
            {
                Assert.That(line.Start.Y, Is.EqualTo(line.End.Y));
                Assert.That(line.Start.X, Is.LessThan(line.End.X));
            }));
        }
    }

    [TestCase(ArrowDirection.Left)]
    [TestCase(ArrowDirection.Right)]
    [TestCase(ArrowDirection.Up)]
    [TestCase(ArrowDirection.Down)]
    public void ResolveArrowPointsKeepsVectorInsideSuppliedBounds(ArrowDirection direction)
    {
        var bounds = new Rectangle(10, 20, 18, 16);
        var points = BootstrapToolStripRenderLogic.ResolveArrowPoints(bounds, direction, 4);

        Assert.That(points, Has.Length.EqualTo(3));
        Assert.That(points.All(point => bounds.Contains(Point.Round(point))), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ResolveSplitButtonGeometryUsesNativeDropDownBounds(bool rightToLeft)
    {
        var buttonBounds = rightToLeft
            ? new Rectangle(18, 0, 62, 28)
            : new Rectangle(0, 0, 62, 28);
        var dropDownBounds = rightToLeft
            ? new Rectangle(0, 0, 18, 28)
            : new Rectangle(62, 0, 18, 28);

        var geometry = BootstrapToolStripRenderLogic.ResolveSplitButtonGeometry(
            buttonBounds, dropDownBounds, rightToLeft, arrowSize: 4);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dropDownBounds.Contains(Point.Round(geometry.ArrowPoints[0])), Is.True);
            Assert.That(dropDownBounds.Contains(Point.Round(geometry.ArrowPoints[1])), Is.True);
            Assert.That(dropDownBounds.Contains(Point.Round(geometry.ArrowPoints[2])), Is.True);
            Assert.That(geometry.DividerStart.X, Is.EqualTo(geometry.DividerEnd.X));
            Assert.That(geometry.DividerStart.X, Is.EqualTo(rightToLeft ? dropDownBounds.Right - 1 : dropDownBounds.Left));
        }));
    }

    [Test]
    public void RenderLogicRejectsInvalidInputs()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapToolStripRenderLogic.ResolveMetrics(null!, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToolStripRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 0)));
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapToolStripRenderLogic.ResolvePalette(null!, BootstrapVariant.Primary, BootstrapToolStripSurfaceKind.ToolBar, true, false, false, false)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToolStripRenderLogic.ResolvePalette(colors, (BootstrapVariant)999, BootstrapToolStripSurfaceKind.ToolBar, true, false, false, false)));
        }));
    }
}
