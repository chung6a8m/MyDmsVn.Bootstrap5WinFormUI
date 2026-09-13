using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapModalLayoutLogicTests
{
    [TestCase(BootstrapModalSize.Small, 96, 300)]
    [TestCase(BootstrapModalSize.Default, 96, 500)]
    [TestCase(BootstrapModalSize.Large, 96, 800)]
    [TestCase(BootstrapModalSize.ExtraLarge, 96, 1140)]
    [TestCase(BootstrapModalSize.Default, 120, 625)]
    [TestCase(BootstrapModalSize.Default, 144, 750)]
    [TestCase(BootstrapModalSize.Default, 168, 875)]
    [TestCase(BootstrapModalSize.Default, 192, 1000)]
    public void ResolvePreferredWidthScalesPresetLogicalWidths(BootstrapModalSize mode, int dpi, int expected)
    {
        Assert.That(BootstrapModalLayoutLogic.ResolvePreferredWidth(mode, 733, dpi), Is.EqualTo(expected));
    }

    [Test]
    public void CustomPreservesRequestedSizeUntilWorkingAreaSafetyClamp()
    {
        var resolved = BootstrapModalLayoutLogic.ResolveDialogSize(
            BootstrapModalSize.Custom,
            new Size(733, 477),
            contentHeight: 900,
            Size.Empty,
            Size.Empty,
            new Size(700, 450),
            dpi: 192);

        Assert.That(resolved, Is.EqualTo(new Size(700, 450)));
    }

    [Test]
    public void PresetWidthBeatsRequestedWidthAndImpossibleMinimumCannotEscapeWorkingArea()
    {
        var resolved = BootstrapModalLayoutLogic.ResolveDialogSize(
            BootstrapModalSize.Small,
            new Size(900, 200),
            contentHeight: 380,
            new Size(1000, 1000),
            Size.Empty,
            new Size(280, 240),
            dpi: 96);

        Assert.That(resolved, Is.EqualTo(new Size(280, 240)));
    }

    [Test]
    public void CenterAndClampKeepsEveryEdgeInsideWorkingArea()
    {
        var working = new Rectangle(-1600, 40, 1200, 800);
        var owner = new Rectangle(-1900, -100, 500, 400);

        var result = BootstrapModalLayoutLogic.CenterAndClamp(new Size(900, 700), owner, working);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.Left, Is.GreaterThanOrEqualTo(working.Left));
            Assert.That(result.Top, Is.GreaterThanOrEqualTo(working.Top));
            Assert.That(result.Right, Is.LessThanOrEqualTo(working.Right));
            Assert.That(result.Bottom, Is.LessThanOrEqualTo(working.Bottom));
        }));
    }
}
