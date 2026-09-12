using System;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapMenuStripTests
{
    [Test]
    public void PublicContractUsesNativeMenuItemsAndSharedRenderer()
    {
        using var menu = new BootstrapMenuStrip();
        var item = new ToolStripMenuItem("&File");
        menu.Items.Add(item);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapMenuStrip).BaseType, Is.EqualTo(typeof(MenuStrip)));
            Assert.That(menu.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(menu.Renderer, Is.TypeOf<BootstrapToolStripRenderer>());
            Assert.That(menu.Items[0], Is.SameAs(item));
        }));
    }

    [Test]
    public void NativeMergeAndRevertPreserveItemIdentityAndOrder()
    {
        using var target = new BootstrapMenuStrip { AllowMerge = true };
        using var source = new BootstrapMenuStrip { AllowMerge = true };
        var targetItem = new ToolStripMenuItem("Target");
        var sourceItem = new ToolStripMenuItem("Source") { MergeAction = MergeAction.Insert, MergeIndex = 0 };
        target.Items.Add(targetItem);
        source.Items.Add(sourceItem);

        Assert.That(ToolStripManager.Merge(source, target), Is.True);
        Assert.That(target.Items.Cast<ToolStripItem>().First(), Is.SameAs(sourceItem));
        Assert.That(ToolStripManager.RevertMerge(target, source), Is.True);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(target.Items.Cast<ToolStripItem>().Single(), Is.SameAs(targetItem));
            Assert.That(source.Items.Cast<ToolStripItem>().Single(), Is.SameAs(sourceItem));
        }));
    }
}
