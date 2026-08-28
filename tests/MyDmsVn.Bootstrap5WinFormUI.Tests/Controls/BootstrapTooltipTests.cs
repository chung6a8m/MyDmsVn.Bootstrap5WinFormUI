using System;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapTooltipTests
{
    [Test]
    public void Stage3TooltipTypeExists()
    {
        var type = typeof(BootstrapButton).Assembly.GetType(
            "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapTooltip",
            throwOnError: false);

        Assert.That(type, Is.Not.Null, "Stage 3 requires the BootstrapTooltip extender component.");
    }
}
