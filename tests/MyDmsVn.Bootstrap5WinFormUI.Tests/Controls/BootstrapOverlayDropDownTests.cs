using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapOverlayDropDownTests
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

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

    [Test]
    public void ShowAtAndMoveToPreserveActualWindowBoundsOutsideWorkingArea()
    {
        using var surface = new BootstrapOverlaySurface();
        using var dropDown = new BootstrapOverlayDropDown(surface);
        var workingArea = Screen.PrimaryScreen!.WorkingArea;
        var shownBounds = new Rectangle(workingArea.Right - 20, workingArea.Top + 40, 120, 60);
        var movedBounds = new Rectangle(workingArea.Left - 80, workingArea.Top + 120, 140, 70);

        try
        {
            dropDown.ShowAt(shownBounds);
            Application.DoEvents();
            Assert.That(GetActualBounds(dropDown.Handle), Is.EqualTo(shownBounds));

            dropDown.MoveTo(movedBounds);
            Application.DoEvents();
            Assert.That(GetActualBounds(dropDown.Handle), Is.EqualTo(movedBounds));
        }
        finally
        {
            dropDown.Close();
        }
    }

    [Test]
    public void ShowAtAndResizeCloneOuterClipWithoutDiscardingRenderedAntiAliasCoverage()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        using var surface = new BootstrapOverlaySurface
        {
            LogicalBorderRadius = 16
        };
        using var dropDown = new BootstrapOverlayDropDown(surface);
        surface.ApplyTheme(theme, 96);
        var workingArea = Screen.PrimaryScreen!.WorkingArea;

        try
        {
            dropDown.ShowAt(new Rectangle(workingArea.Left + 40, workingArea.Top + 40, 140, 80));
            Application.DoEvents();
            AssertWindowClipPreservesRenderedAntiAliasCoverage(surface, dropDown, theme, 16);

            dropDown.MoveTo(new Rectangle(workingArea.Left + 60, workingArea.Top + 60, 160, 90));
            Application.DoEvents();
            AssertWindowClipPreservesRenderedAntiAliasCoverage(surface, dropDown, theme, 16);
        }
        finally
        {
            dropDown.Close();
        }
    }

    private static void AssertWindowClipPreservesRenderedAntiAliasCoverage(
        BootstrapOverlaySurface surface,
        BootstrapOverlayDropDown dropDown,
        BootstrapTheme theme,
        int radius)
    {
        Assert.That(dropDown.Region, Is.Not.Null);
        using var windowClip = dropDown.Region!.Clone();
        surface.Region = null;
        using var rendered = new Bitmap(surface.Width, surface.Height, PixelFormat.Format32bppArgb);
        surface.DrawToBitmap(rendered, surface.ClientRectangle);
        using var clipped = new Bitmap(surface.Width, surface.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(clipped))
        {
            graphics.Clear(Color.Transparent);
            graphics.SetClip(windowClip, System.Drawing.Drawing2D.CombineMode.Replace);
            graphics.DrawImageUnscaled(rendered, Point.Empty);
        }

        Assert.Multiple((Action)(() =>
        {
            AssertCornerCoverage(rendered, clipped, theme, radius, left: true, top: true);
            AssertCornerCoverage(rendered, clipped, theme, radius, left: false, top: true);
            AssertCornerCoverage(rendered, clipped, theme, radius, left: true, top: false);
            AssertCornerCoverage(rendered, clipped, theme, radius, left: false, top: false);
            Assert.That(windowClip.IsVisible(0.5f, 0.5f), Is.False);
        }));
    }

    private static void AssertCornerCoverage(
        Bitmap rendered,
        Bitmap clipped,
        BootstrapTheme theme,
        int radius,
        bool left,
        bool top)
    {
        var outside = Color.Black.ToArgb();
        var surface = theme.Colors.Surface.ToArgb();
        var border = theme.Colors.Border.ToArgb();
        var antiAliasedPixels = 0;
        var lostPixels = 0;
        for (var y = 0; y <= radius; y++)
        {
            for (var x = 0; x <= radius; x++)
            {
                var sampleX = left ? x : rendered.Width - 1 - x;
                var sampleY = top ? y : rendered.Height - 1 - y;
                var before = rendered.GetPixel(sampleX, sampleY).ToArgb();
                if (before == outside || before == surface || before == border)
                {
                    continue;
                }

                antiAliasedPixels++;
                if (clipped.GetPixel(sampleX, sampleY).ToArgb() != before)
                {
                    lostPixels++;
                }
            }
        }

        Assert.That(antiAliasedPixels, Is.GreaterThan(0));
        Assert.That(lostPixels, Is.Zero, $"The dropdown Region discarded AA coverage at {(top ? "top" : "bottom")}-{(left ? "left" : "right")}.");
    }

    private static Rectangle GetActualBounds(IntPtr handle)
    {
        Assert.That(GetWindowRect(handle, out var bounds), Is.True);
        return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRectangle bounds);
}
