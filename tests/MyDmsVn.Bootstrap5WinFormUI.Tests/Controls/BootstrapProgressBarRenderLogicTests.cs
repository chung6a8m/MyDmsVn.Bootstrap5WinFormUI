using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapProgressBarRenderLogicTests
{
    [Test]
    public void InterpolateValueSupportsFullIntegerRangeWithoutOverflow()
    {
        Assert.That(
            BootstrapProgressBarRenderLogic.InterpolateValue(int.MinValue, int.MaxValue, 1.0),
            Is.EqualTo(int.MaxValue));
    }
}
