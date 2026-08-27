using NUnit.Framework;
using MyDmsVn.Bootstrap5WinFormUI.Animation;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Animation;

[TestFixture]
public sealed class BootstrapEasingTests
{
    [TestCase(-1.0, 0.0)]
    [TestCase(0.0, 0.0)]
    [TestCase(0.5, 0.5)]
    [TestCase(1.0, 1.0)]
    [TestCase(2.0, 1.0)]
    public void LinearClampsToNormalizedRange(double input, double expected)
    {
        Assert.That(BootstrapEasing.Linear(input), Is.EqualTo(expected).Within(0.000001));
    }

    [Test]
    public void QuadraticCurvesHaveExpectedMidpoints()
    {
        Assert.That(BootstrapEasing.EaseIn(0.5), Is.EqualTo(0.25).Within(0.000001));
        Assert.That(BootstrapEasing.EaseOut(0.5), Is.EqualTo(0.75).Within(0.000001));
        Assert.That(BootstrapEasing.EaseInOut(0.25), Is.EqualTo(0.125).Within(0.000001));
        Assert.That(BootstrapEasing.EaseInOut(0.75), Is.EqualTo(0.875).Within(0.000001));
    }

    [Test]
    public void BuiltInCurvesStayNormalizedAndMonotonic()
    {
        var curves = new Func<double, double>[]
        {
            BootstrapEasing.Linear,
            BootstrapEasing.EaseIn,
            BootstrapEasing.EaseOut,
            BootstrapEasing.EaseInOut
        };

        foreach (var curve in curves)
        {
            var previous = curve(-1.0);
            Assert.That(previous, Is.InRange(0.0, 1.0));

            for (var index = 0; index <= 20; index++)
            {
                var current = curve(index / 20.0);
                Assert.That(current, Is.InRange(0.0, 1.0));
                Assert.That(current, Is.GreaterThanOrEqualTo(previous));
                previous = current;
            }

            Assert.That(curve(2.0), Is.EqualTo(1.0).Within(0.000001));
        }
    }
}
