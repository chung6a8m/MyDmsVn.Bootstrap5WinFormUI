using System;
using System.Drawing;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToastReviewRegressionTests
{
    [Test]
    public void DismissedHandlerMayDisposeContainerWithoutCreatingAnOrphanedExitAnimation()
    {
        var harness = new BootstrapToastAnimationHarness();
        var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = new BootstrapToast
        {
            Width = 240,
            Text = "Reentrant dismissal",
            AutoHide = false,
            AnimationDuration = 200
        };

        container.ShowToast(toast);
        harness.Records[0].Advance(200);
        toast.Dismissed += (_, _) => container.Dispose();

        Assert.DoesNotThrow((Action)toast.Dismiss);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(container.IsDisposed, Is.True);
            Assert.That(toast.IsDisposed, Is.True);
            Assert.That(harness.Records.Count, Is.EqualTo(1), "no exit animation should be created after the callback disposed the owner");
        }));
    }
}
