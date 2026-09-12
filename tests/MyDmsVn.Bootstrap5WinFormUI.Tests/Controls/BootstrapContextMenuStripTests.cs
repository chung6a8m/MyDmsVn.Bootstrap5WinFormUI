using System;
using System.ComponentModel;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapContextMenuStripTests
{
    [Test]
    public void PublicContractIsDirectNativeContextMenuWithSharedRenderer()
    {
        using var menu = new BootstrapContextMenuStrip();
        var item = new ToolStripMenuItem("Action");
        menu.Items.Add(item);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapContextMenuStrip).BaseType, Is.EqualTo(typeof(ContextMenuStrip)));
            Assert.That(menu.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(menu.Renderer, Is.TypeOf<BootstrapToolStripRenderer>());
            Assert.That(menu.Items[0], Is.SameAs(item));
        }));
    }

    [Test]
    public void ContainerConstructorUsesNativeComponentLifetime()
    {
        var container = new Container();
        var menu = new BootstrapContextMenuStrip(container);

        container.Dispose();

        Assert.That(menu.IsDisposed, Is.True);
    }
}
