using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapToastServiceLayoutLogicTests
{
    [Test]
    public void InsetWorkingArea_UsesExplicitDpiAndPreservesNegativeOrigin()
    {
        var result = BootstrapToastServiceLayoutLogic.InsetWorkingArea(
            new Rectangle(-1920, 0, 1920, 1040),
            new Padding(16),
            dpi: 96);

        Assert.That(result, Is.EqualTo(new Rectangle(-1904, 16, 1888, 1008)));
    }

    [TestCase(96, 320)]
    [TestCase(120, 400)]
    [TestCase(144, 480)]
    [TestCase(168, 560)]
    [TestCase(192, 640)]
    public void ResolveToastWidth_ScalesWithTargetScreenDpi(int dpi, int expected)
    {
        Assert.That(BootstrapToastServiceLayoutLogic.ResolveToastWidth(320, 1000, dpi), Is.EqualTo(expected));
        Assert.That(BootstrapToastServiceLayoutLogic.ResolveToastWidth(320, expected - 1, dpi), Is.EqualTo(expected - 1));
    }

    [Test]
    public void OversizedMarginsLeaveAtLeastOneAvailablePixel()
    {
        var result = BootstrapToastServiceLayoutLogic.InsetWorkingArea(
            new Rectangle(-10, -20, 20, 10),
            new Padding(100),
            192);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.Width, Is.EqualTo(1));
            Assert.That(result.Height, Is.EqualTo(1));
            Assert.That(new Rectangle(-10, -20, 20, 10).Contains(result), Is.True);
        }));
    }

    [TestCase(BootstrapToastPlacement.TopLeft, -1000, 50)]
    [TestCase(BootstrapToastPlacement.TopRight, -420, 50)]
    [TestCase(BootstrapToastPlacement.BottomLeft, -1000, 510)]
    [TestCase(BootstrapToastPlacement.BottomRight, -420, 510)]
    public void NotificationCenterBoundsAnchorToAllFourCorners(
        BootstrapToastPlacement placement,
        int expectedX,
        int expectedY)
    {
        var bounds = BootstrapToastServiceLayoutLogic.CalculateNotificationCenterBounds(
            new Rectangle(-1000, 50, 1000, 800),
            new Size(420, 340),
            placement);

        Assert.That(bounds, Is.EqualTo(new Rectangle(expectedX, expectedY, 420, 340)));
    }

    [Test]
    public void NotificationCenterSizeUsesExplicitDpiAndClampsToAvailablePixels()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                BootstrapToastServiceLayoutLogic.ResolveNotificationCenterSize(new Size(420, 560), new Size(1000, 1000), 144),
                Is.EqualTo(new Size(630, 840)));
            Assert.That(
                BootstrapToastServiceLayoutLogic.ResolveNotificationCenterSize(new Size(420, 560), new Size(500, 700), 144),
                Is.EqualTo(new Size(500, 700)));
        }));
    }

    [Test]
    public void ScreenInfoAndGeometryRejectInvalidInputs()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentException>((Action)(() => new BootstrapToastScreenInfo("", new Rectangle(0, 0, 10, 10), 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new BootstrapToastScreenInfo("DISPLAY", Rectangle.Empty, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new BootstrapToastScreenInfo("DISPLAY", new Rectangle(0, 0, 10, 10), 0)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastServiceLayoutLogic.InsetWorkingArea(Rectangle.Empty, Padding.Empty, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastServiceLayoutLogic.InsetWorkingArea(new Rectangle(0, 0, 10, 10), new Padding(-1), 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastServiceLayoutLogic.ResolveToastWidth(0, 10, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastServiceLayoutLogic.ResolveNotificationCenterSize(Size.Empty, new Size(1, 1), 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastServiceLayoutLogic.CalculateNotificationCenterBounds(new Rectangle(0, 0, 10, 10), new Size(1, 1), (BootstrapToastPlacement)999)));
        }));
    }
}
