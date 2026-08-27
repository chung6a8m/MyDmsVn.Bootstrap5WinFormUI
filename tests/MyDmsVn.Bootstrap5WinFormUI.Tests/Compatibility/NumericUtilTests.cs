using System;
using MyDmsVn.Bootstrap5WinFormUI.Compatibility;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Compatibility;

[TestFixture]
public sealed class NumericUtilTests
{
    [TestCase(-5, 0, 10, 0)]
    [TestCase(5, 0, 10, 5)]
    [TestCase(15, 0, 10, 10)]
    public void ClampIntReturnsValueWithinBounds(int value, int minimum, int maximum, int expected)
    {
        Assert.That(NumericUtil.Clamp(value, minimum, maximum), Is.EqualTo(expected));
    }

    [TestCase(-0.5d, 0d, 1d, 0d)]
    [TestCase(0.25d, 0d, 1d, 0.25d)]
    [TestCase(1.5d, 0d, 1d, 1d)]
    public void ClampDoubleReturnsValueWithinBounds(double value, double minimum, double maximum, double expected)
    {
        Assert.That(NumericUtil.Clamp(value, minimum, maximum), Is.EqualTo(expected));
    }

    [Test]
    public void ClampThrowsWhenMinimumExceedsMaximum()
    {
        Action action = () => NumericUtil.Clamp(5, 10, 0);

        Assert.That(action, Throws.TypeOf<ArgumentException>());
    }
}
