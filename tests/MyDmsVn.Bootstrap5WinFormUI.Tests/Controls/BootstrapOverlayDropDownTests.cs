using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapOverlayDropDownTests
{
    [Test]
    public void OwnsExactlyOneReusableSurfaceHost()
    {
        using var surface = new BootstrapOverlaySurface();
        using var dropDown = new BootstrapOverlayDropDown(surface);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dropDown.Items.OfType<ToolStripControlHost>().Count(), Is.EqualTo(1));
            Assert.That(dropDown.Items.OfType<ToolStripControlHost>().Single().Control, Is.SameAs(surface));
            Assert.That(dropDown.AutoSize, Is.False);
            Assert.That(dropDown.Padding, Is.EqualTo(Padding.Empty));
        }));

        dropDown.AutoClose = false;
        Assert.That(dropDown.AutoClose, Is.False);
    }

    [Test]
    public void DisposalAfterDetachDoesNotDisposeCallerContent()
    {
        var surface = new BootstrapOverlaySurface();
        var dropDown = new BootstrapOverlayDropDown(surface);
        using var content = new Panel();
        var disposed = 0;
        content.Disposed += (_, _) => disposed++;
        surface.AttachContent(content);
        Assert.That(surface.DetachContent(), Is.SameAs(content));

        dropDown.Dispose();

        Assert.That(disposed, Is.Zero);
    }
}
