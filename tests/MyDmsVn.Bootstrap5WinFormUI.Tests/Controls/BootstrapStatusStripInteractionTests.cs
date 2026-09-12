using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapStatusStripInteractionTests
{
    [Test]
    public void SpringLabelUsesNativeRemainingWidthAfterResize()
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form { ClientSize = new Size(500, 180) };
            using var strip = new BootstrapStatusStrip();
            var left = new ToolStripStatusLabel("Left") { AutoSize = true };
            var spring = new ToolStripStatusLabel("Spring") { Spring = true };
            var right = new ToolStripStatusLabel("Right") { AutoSize = true };
            strip.Items.AddRange(new ToolStripItem[] { left, spring, right });
            form.Controls.Add(strip);
            form.Show();
            form.PerformLayout();
            var wide = spring.Width;

            form.ClientSize = new Size(320, 180);
            form.PerformLayout();
            var narrow = spring.Width;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(wide, Is.GreaterThan(narrow));
                Assert.That(spring.Spring, Is.True);
                Assert.That(left.Owner, Is.SameAs(strip));
                Assert.That(right.Owner, Is.SameAs(strip));
            }));
            form.Close();
        });
    }

    [Test]
    public void InteractiveItemsAndHostedProgressBarRemainNative()
    {
        using var strip = new BootstrapStatusStrip { ShowItemToolTips = true, SizingGrip = true };
        var dropdown = new ToolStripDropDownButton("Menu") { ToolTipText = "Native tooltip" };
        dropdown.DropDownItems.Add("Child");
        var split = new ToolStripSplitButton("Split");
        split.DropDownItems.Add("Child");
        var progress = new ToolStripProgressBar { Minimum = 0, Maximum = 100, Value = 40, Style = ProgressBarStyle.Continuous };
        strip.Items.AddRange(new ToolStripItem[] { dropdown, split, progress });
        var buttonClicks = 0;
        split.ButtonClick += (_, _) => buttonClicks++;

        split.PerformButtonClick();
        progress.Value = 75;
        progress.Style = ProgressBarStyle.Marquee;
        strip.Width = 240;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(buttonClicks, Is.EqualTo(1));
            Assert.That(dropdown.DropDownItems.Count, Is.EqualTo(1));
            Assert.That(dropdown.ToolTipText, Is.EqualTo("Native tooltip"));
            Assert.That(progress.Value, Is.EqualTo(75));
            Assert.That(progress.Style, Is.EqualTo(ProgressBarStyle.Marquee));
            Assert.That(strip.SizingGrip, Is.True);
        }));
    }
}
