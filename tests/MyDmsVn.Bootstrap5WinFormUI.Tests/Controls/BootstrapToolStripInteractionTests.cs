using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapToolStripInteractionTests
{
    [Test]
    public void NativeItemsKeepClickCheckImageTooltipAndDynamicCollectionBehavior()
    {
        using var strip = new BootstrapToolStrip();
        using var image = new Bitmap(8, 8);
        var button = new ToolStripButton("Check", image) { CheckOnClick = true, ToolTipText = "Native tip" };
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        strip.Items.Add(button);

        button.PerformClick();
        strip.Items.Remove(button);
        strip.Items.Add(button);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(button.Checked, Is.True);
            Assert.That(button.Image, Is.SameAs(image));
            Assert.That(button.ToolTipText, Is.EqualTo("Native tip"));
            Assert.That(strip.Items.Cast<ToolStripItem>().Single(), Is.SameAs(button));
        }));
    }

    [Test]
    public void SplitButtonKeepsMainAndDropDownActionsSeparate()
    {
        using var strip = new BootstrapToolStrip();
        var split = new ToolStripSplitButton("Split");
        split.DropDownItems.Add("Child");
        strip.Items.Add(split);
        var mainClicks = 0;
        var openings = 0;
        split.ButtonClick += (_, _) => mainClicks++;
        split.DropDownOpening += (_, _) => openings++;

        split.PerformButtonClick();
        split.ShowDropDown();
        split.HideDropDown();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(mainClicks, Is.EqualTo(1));
            Assert.That(openings, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ConstrainedWidthUsesNativeOverflowOwnership()
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form { Size = new Size(180, 100) };
            using var strip = new BootstrapToolStrip { Dock = DockStyle.Top, CanOverflow = true };
            for (var index = 0; index < 12; index++)
            {
                strip.Items.Add(new ToolStripButton("Item " + index) { Overflow = ToolStripItemOverflow.AsNeeded });
            }

            form.Controls.Add(strip);
            form.Show();
            form.PerformLayout();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(strip.OverflowButton, Is.Not.Null);
                Assert.That(strip.Items.Cast<ToolStripItem>().Any(item => item.Placement == ToolStripItemPlacement.Overflow), Is.True);
                Assert.That(strip.Items.Cast<ToolStripItem>().All(item => item.Owner == strip), Is.True);
            }));
            form.Close();
        });
    }
}
