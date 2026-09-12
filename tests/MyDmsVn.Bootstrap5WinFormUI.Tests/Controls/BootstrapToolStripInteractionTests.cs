using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
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

    [Test]
    public void OpenNativeOverflowIsInvalidatedInPlaceWhenAppearanceChanges()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
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
                strip.OverflowButton.ShowDropDown();
                Application.DoEvents();

                var overflow = strip.OverflowButton.DropDown;
                var invalidations = 0;
                overflow.Invalidated += (_, _) => invalidations++;
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
                strip.Variant = BootstrapVariant.Warning;
                Application.DoEvents();

                Assert.Multiple((Action)(() =>
                {
                    Assert.That(strip.OverflowButton.DropDown, Is.SameAs(overflow));
                    Assert.That(overflow.Visible, Is.True);
                    Assert.That(invalidations, Is.GreaterThan(0));
                }));

                strip.OverflowButton.HideDropDown();
                form.Close();
            });
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [TestCase(ToolStripLayoutStyle.Flow)]
    [TestCase(ToolStripLayoutStyle.Table)]
    public void VerticalFlowAndTableLayoutsRenderTheNativeHorizontalGrip(ToolStripLayoutStyle layoutStyle)
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form { ClientSize = new Size(240, 180) };
            using var strip = new BootstrapToolStrip
            {
                AutoSize = false,
                LayoutStyle = layoutStyle,
                Dock = DockStyle.Left,
                GripStyle = ToolStripGripStyle.Visible,
                Width = 72
            };
            strip.Items.Add("Item");
            form.Controls.Add(strip);
            form.Show();
            form.PerformLayout();

            using var bitmap = new Bitmap(1, 1);
            using var graphics = Graphics.FromImage(bitmap);
            var args = new ToolStripGripRenderEventArgs(graphics, strip);
            var dots = BootstrapToolStripRenderLogic.ResolveGripDots(new Rectangle(0, 0, 30, 10), args.GripDisplayStyle, dotSize: 2);
            var painted = dots.Aggregate(Rectangle.Union);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(strip.Orientation, Is.EqualTo(Orientation.Vertical));
                Assert.That(args.GripDisplayStyle, Is.EqualTo(ToolStripGripDisplayStyle.Horizontal));
                Assert.That(painted.Width, Is.GreaterThan(painted.Height));
            }));

            form.Close();
        });
    }

    [Test]
    public void RealVerticalOverflowRendersARightFacingAffordance()
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form { ClientSize = new Size(240, 120) };
            using var strip = new BootstrapToolStrip { AutoSize = false, Dock = DockStyle.Left, CanOverflow = true, Width = 84 };
            for (var index = 0; index < 12; index++)
            {
                strip.Items.Add(new ToolStripButton("Item " + index) { Overflow = ToolStripItemOverflow.AsNeeded });
            }

            form.Controls.Add(strip);
            form.Show();
            form.PerformLayout();
            var overflowButton = strip.OverflowButton;
            Assert.That(strip.Items.Cast<ToolStripItem>().Any(item => item.Placement == ToolStripItemPlacement.Overflow), Is.True);

            using var bitmap = new Bitmap(overflowButton.Width, overflowButton.Height);
            using var graphics = Graphics.FromImage(bitmap);
            InvokeRendererHook(strip.Renderer, "OnRenderOverflowButtonBackground", new ToolStripItemRenderEventArgs(graphics, overflowButton));
            var arrowSize = BootstrapToolStripRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, strip.DeviceDpi).ArrowSize;
            var rightTip = new Point((overflowButton.Width / 2) + Math.Max(1, arrowSize / 2), overflowButton.Height / 2);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(strip.Orientation, Is.EqualTo(Orientation.Vertical));
                Assert.That(bitmap.GetPixel(rightTip.X, rightTip.Y).ToArgb(), Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.Text.ToArgb()));
            }));

            form.Close();
        });
    }

    [Test]
    public void SplitButtonDividerUsesNativeSplitterBoundsAndItemRightToLeft()
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form { ClientSize = new Size(280, 100) };
            using var strip = new BootstrapToolStrip { Dock = DockStyle.Top, RightToLeft = RightToLeft.No };
            var split = new ToolStripSplitButton("Split") { RightToLeft = RightToLeft.Yes };
            split.DropDownItems.Add("Child");
            strip.Items.Add(split);
            form.Controls.Add(strip);
            form.Show();
            form.PerformLayout();

            using var bitmap = new Bitmap(split.Width, split.Height);
            using var graphics = Graphics.FromImage(bitmap);
            InvokeRendererHook(strip.Renderer, "OnRenderSplitButtonBackground", new ToolStripItemRenderEventArgs(graphics, split));
            var visibleSplitterBounds = Rectangle.Intersect(split.SplitterBounds, new Rectangle(Point.Empty, bitmap.Size));
            var splitterPainted = RectanglePoints(visibleSplitterBounds)
                .Any(point => bitmap.GetPixel(point.X, point.Y).A != 0);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(split.SplitterBounds.IsEmpty, Is.False);
                Assert.That(split.DropDownButtonBounds.Right, Is.LessThanOrEqualTo(split.SplitterBounds.Left));
                Assert.That(splitterPainted, Is.True);
            }));

            form.Close();
        });
    }

    private static void InvokeRendererHook(ToolStripRenderer renderer, string methodName, EventArgs args)
    {
        var method = typeof(BootstrapToolStripRendererBase).GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(renderer, new object[] { args });
    }

    private static System.Collections.Generic.IEnumerable<Point> RectanglePoints(Rectangle rectangle)
    {
        for (var y = rectangle.Top; y < rectangle.Bottom; y++)
        for (var x = rectangle.Left; x < rectangle.Right; x++)
        {
            yield return new Point(x, y);
        }
    }
}
