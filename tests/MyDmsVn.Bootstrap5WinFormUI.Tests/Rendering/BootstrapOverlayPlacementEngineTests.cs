using System;
using System.Collections.Generic;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Rendering;

[TestFixture]
public sealed class BootstrapOverlayPlacementEngineTests
{
    private static IEnumerable<TestCaseData> ExplicitPlacements()
    {
        yield return Case(BootstrapOverlayPlacement.Top, 110, 50);
        yield return Case(BootstrapOverlayPlacement.TopStart, 100, 50);
        yield return Case(BootstrapOverlayPlacement.TopEnd, 120, 50);
        yield return Case(BootstrapOverlayPlacement.Bottom, 110, 150);
        yield return Case(BootstrapOverlayPlacement.BottomStart, 100, 150);
        yield return Case(BootstrapOverlayPlacement.BottomEnd, 120, 150);
        yield return Case(BootstrapOverlayPlacement.Left, 30, 100);
        yield return Case(BootstrapOverlayPlacement.LeftStart, 30, 100);
        yield return Case(BootstrapOverlayPlacement.LeftEnd, 30, 100);
        yield return Case(BootstrapOverlayPlacement.Right, 170, 100);
        yield return Case(BootstrapOverlayPlacement.RightStart, 170, 100);
        yield return Case(BootstrapOverlayPlacement.RightEnd, 170, 100);
    }

    [TestCaseSource(nameof(ExplicitPlacements))]
    public void ExplicitPlacementUsesDocumentedBaseGeometry(BootstrapOverlayPlacement placement, int x, int y)
    {
        var result = Compute(
            new Rectangle(100, 100, 70, 50),
            new Size(50, 40),
            new Rectangle(0, 0, 500, 500),
            placement,
            BootstrapOverlayCollisionBehavior.None,
            10);

        Assert.That(result.Bounds, Is.EqualTo(new Rectangle(x, y, 50, 40)));
        Assert.That(result.Placement, Is.EqualTo(placement));
    }

    [Test]
    public void RtlSwapsHorizontalStartAndEndOnly()
    {
        var topStart = Compute(new Rectangle(100, 100, 70, 50), new Size(50, 40), new Rectangle(0, 0, 500, 500), BootstrapOverlayPlacement.TopStart, BootstrapOverlayCollisionBehavior.None, 10, rightToLeft: true);
        var leftStart = Compute(new Rectangle(100, 100, 70, 50), new Size(50, 40), new Rectangle(0, 0, 500, 500), BootstrapOverlayPlacement.LeftStart, BootstrapOverlayCollisionBehavior.None, 10, rightToLeft: true);

        Assert.That(topStart.Bounds.X, Is.EqualTo(120));
        Assert.That(leftStart.Bounds.Y, Is.EqualTo(100));
    }

    [Test]
    public void FlipUsesExactOppositeAndShiftChangesOnlyCrossAxis()
    {
        var result = Compute(
            new Rectangle(460, 5, 30, 30),
            new Size(100, 50),
            new Rectangle(0, 0, 500, 500),
            BootstrapOverlayPlacement.TopEnd,
            BootstrapOverlayCollisionBehavior.FlipAndShift,
            6,
            boundaryPadding: 8);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.Placement, Is.EqualTo(BootstrapOverlayPlacement.BottomEnd));
            Assert.That(result.Bounds.Y, Is.EqualTo(41));
            Assert.That(result.Bounds.X, Is.EqualTo(392));
            Assert.That(result.Flipped, Is.True);
            Assert.That(result.Shifted, Is.True);
            Assert.That(result.Overflow.Total, Is.Zero);
        }));
    }

    [Test]
    public void AutoUsesDeterministicTieOrder()
    {
        var result = Compute(
            new Rectangle(450, 450, 100, 100),
            new Size(100, 100),
            new Rectangle(0, 0, 1000, 1000),
            BootstrapOverlayPlacement.Auto,
            BootstrapOverlayCollisionBehavior.None,
            0);

        Assert.That(result.Placement, Is.EqualTo(BootstrapOverlayPlacement.Bottom));
    }

    [Test]
    public void NegativeDesktopCoordinatesAndOversizedPopupDoNotWrap()
    {
        var result = Compute(
            new Rectangle(-100, 500, 80, 30),
            new Size(2500, 1200),
            new Rectangle(-1920, 0, 1920, 1080),
            BootstrapOverlayPlacement.Top,
            BootstrapOverlayCollisionBehavior.Shift,
            8,
            boundaryPadding: 16);

        Assert.That(result.Bounds.X, Is.EqualTo(-1904));
        Assert.That(result.Bounds.Y, Is.EqualTo(-708));
        Assert.That(result.Overflow.Total, Is.GreaterThan(0));
    }

    [Test]
    public void ExtremeCoordinatesSaturateInsteadOfWrapping()
    {
        var result = Compute(
            new Rectangle(int.MaxValue - 10, int.MinValue + 10, 100, 100),
            new Size(200, 200),
            new Rectangle(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue),
            BootstrapOverlayPlacement.Right,
            BootstrapOverlayCollisionBehavior.None,
            int.MaxValue);

        Assert.That(result.Bounds.X, Is.EqualTo(int.MaxValue));
        Assert.That(result.Bounds.Y, Is.LessThan(0));
    }

    [Test]
    public void InvalidInputsAreRejected()
    {
        var valid = new BootstrapOverlayPlacementRequest(
            Rectangle.Empty,
            Size.Empty,
            new Rectangle(0, 0, 100, 100),
            BootstrapOverlayPlacement.Top,
            BootstrapOverlayCollisionBehavior.None,
            0,
            0,
            false);

        Assert.DoesNotThrow((Action)(() => BootstrapOverlayPlacementEngine.Compute(valid)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapOverlayPlacementEngine.Compute(new BootstrapOverlayPlacementRequest(Rectangle.Empty, Size.Empty, Rectangle.Empty, (BootstrapOverlayPlacement)99, BootstrapOverlayCollisionBehavior.None, 0, 0, false))));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapOverlayPlacementEngine.Compute(new BootstrapOverlayPlacementRequest(Rectangle.Empty, Size.Empty, Rectangle.Empty, BootstrapOverlayPlacement.Top, (BootstrapOverlayCollisionBehavior)99, 0, 0, false))));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapOverlayPlacementEngine.Compute(new BootstrapOverlayPlacementRequest(Rectangle.Empty, Size.Empty, Rectangle.Empty, BootstrapOverlayPlacement.Top, BootstrapOverlayCollisionBehavior.None, -1, 0, false))));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapOverlayPlacementEngine.Compute(new BootstrapOverlayPlacementRequest(Rectangle.Empty, Size.Empty, Rectangle.Empty, BootstrapOverlayPlacement.Top, BootstrapOverlayCollisionBehavior.None, 0, -1, false))));
    }

    private static TestCaseData Case(BootstrapOverlayPlacement placement, int x, int y)
    {
        return new TestCaseData(placement, x, y).SetName($"Explicit_{placement}");
    }

    private static BootstrapOverlayPlacementResult Compute(
        Rectangle anchor,
        Size floating,
        Rectangle boundary,
        BootstrapOverlayPlacement placement,
        BootstrapOverlayCollisionBehavior collision,
        int offset,
        int boundaryPadding = 0,
        bool rightToLeft = false)
    {
        return BootstrapOverlayPlacementEngine.Compute(new BootstrapOverlayPlacementRequest(
            anchor,
            floating,
            boundary,
            placement,
            collision,
            offset,
            boundaryPadding,
            rightToLeft));
    }
}
