using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Rendering;

[TestFixture]
public sealed class CornerRadiusTests
{
    [Test]
    public void UniformRadiusAppliesToEveryCorner()
    {
        var radius = new CornerRadius(6f);

        Assert.That(radius.TopLeft, Is.EqualTo(6f));
        Assert.That(radius.TopRight, Is.EqualTo(6f));
        Assert.That(radius.BottomRight, Is.EqualTo(6f));
        Assert.That(radius.BottomLeft, Is.EqualTo(6f));
    }

    [Test]
    public void ConstructorRejectsNegativeRadius()
    {
        Action action = () => new CornerRadius(4f, -1f, 4f, 4f);

        Assert.That(action, Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void NormalizeToBoundsScalesOversizedCornersProportionally()
    {
        var radius = new CornerRadius(30f);

        var normalized = radius.NormalizeTo(new RectangleF(0f, 0f, 100f, 40f));

        Assert.That(normalized.TopLeft, Is.EqualTo(20f).Within(0.001f));
        Assert.That(normalized.TopRight, Is.EqualTo(20f).Within(0.001f));
        Assert.That(normalized.BottomRight, Is.EqualTo(20f).Within(0.001f));
        Assert.That(normalized.BottomLeft, Is.EqualTo(20f).Within(0.001f));
    }

    [Test]
    public void NormalizeToEmptyBoundsReturnsZeroRadii()
    {
        var normalized = new CornerRadius(8f).NormalizeTo(RectangleF.Empty);

        Assert.That(normalized, Is.EqualTo(CornerRadius.Empty));
    }

    [Test]
    public void RoundedPathUsesNormalizedPerCornerRadii()
    {
        using var path = RoundedPath.Create(
            new RectangleF(0f, 0f, 100f, 40f),
            new CornerRadius(30f, 10f, 30f, 10f));

        var bounds = path.GetBounds();

        Assert.That(path.PointCount, Is.GreaterThan(4));
        Assert.That(bounds.Left, Is.EqualTo(0f).Within(0.01f));
        Assert.That(bounds.Top, Is.EqualTo(0f).Within(0.01f));
        Assert.That(bounds.Right, Is.EqualTo(100f).Within(0.01f));
        Assert.That(bounds.Bottom, Is.EqualTo(40f).Within(0.01f));
    }
}
