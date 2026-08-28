using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapToastLayoutLogicTests
{
    [Test]
    public void Stage8PlacementTypeMustExist()
    {
        var type = typeof(BootstrapButton).Assembly.GetType(
            "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapToastPlacement",
            throwOnError: false);

        Assert.That(type, Is.Not.Null, "Stage 8 must introduce BootstrapToastPlacement before layout behavior can pass.");
    }
}
