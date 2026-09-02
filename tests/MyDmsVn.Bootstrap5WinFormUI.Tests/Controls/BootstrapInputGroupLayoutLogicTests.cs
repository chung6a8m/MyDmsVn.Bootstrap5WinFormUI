using System;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapInputGroupLayoutLogicTests
{
    [Test]
    public void SurplusIsSharedEquallyByStretchChildren()
    {
        var items = new[]
        {
            new BootstrapInputGroupLayoutItem(30, 20, false),
            new BootstrapInputGroupLayoutItem(80, 40, true),
            new BootstrapInputGroupLayoutItem(70, 40, true)
        };

        var result = BootstrapInputGroupLayoutLogic.Calculate(items, 199, 32, 1, false);

        Assert.That(result.Bounds.Select(value => value.Width), Is.EqualTo(new[] { 30, 86, 85 }));
        AssertContained(result, 199);
    }

    [Test]
    public void FixedCompressionPreservesStretchMinimum()
    {
        var items = new[]
        {
            new BootstrapInputGroupLayoutItem(60, 20, false),
            new BootstrapInputGroupLayoutItem(100, 50, true),
            new BootstrapInputGroupLayoutItem(40, 20, false)
        };

        var result = BootstrapInputGroupLayoutLogic.Calculate(items, 118, 32, 1, false);

        Assert.That(result.Bounds[1].Width, Is.EqualTo(50));
        Assert.That(result.Bounds.Sum(value => value.Width), Is.EqualTo(120));
        AssertContained(result, 118);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(39)]
    public void EmergencyCompressionNeverEmitsNegativeOrOutOfClientBounds(int clientWidth)
    {
        var items = new[]
        {
            new BootstrapInputGroupLayoutItem(80, 30, false),
            new BootstrapInputGroupLayoutItem(120, 70, true),
            new BootstrapInputGroupLayoutItem(50, 20, false)
        };

        var result = BootstrapInputGroupLayoutLogic.Calculate(items, clientWidth, 24, 1, false);

        AssertContained(result, clientWidth);
    }

    [Test]
    public void RtlMirrorsVisualBoundsWithoutChangingResultOrder()
    {
        var items = new[]
        {
            new BootstrapInputGroupLayoutItem(20, 20, false),
            new BootstrapInputGroupLayoutItem(40, 20, true)
        };

        var ltr = BootstrapInputGroupLayoutLogic.Calculate(items, 80, 30, 1, false);
        var rtl = BootstrapInputGroupLayoutLogic.Calculate(items, 80, 30, 1, true);

        Assert.That(rtl.Bounds[0].Right, Is.EqualTo(80));
        Assert.That(rtl.Bounds[1].Left, Is.EqualTo(0));
        Assert.That(rtl.Bounds.Select(value => value.Width), Is.EqualTo(ltr.Bounds.Select(value => value.Width)));
    }

    [Test]
    public void PreferredWidthUsesNaturalWidthsMinusSeams()
    {
        var result = BootstrapInputGroupLayoutLogic.Calculate(
            new[] { new BootstrapInputGroupLayoutItem(30, 10, false), new BootstrapInputGroupLayoutItem(80, 40, true) },
            200,
            36,
            2,
            false);

        Assert.That(result.PreferredWidth, Is.EqualTo(108));
        Assert.That(result.PreferredHeight, Is.EqualTo(36));
    }

    private static void AssertContained(BootstrapInputGroupLayoutResult result, int clientWidth)
    {
        Assert.That(result.Bounds.All(value => value.X >= 0 && value.Width >= 0 && value.Right <= Math.Max(0, clientWidth)), Is.True);
    }
}
