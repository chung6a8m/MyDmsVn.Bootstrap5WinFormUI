using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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

    private static Rectangle GetActualBounds(IntPtr handle)
    {
        Assert.That(GetWindowRect(handle, out var bounds), Is.True);
        return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRectangle bounds);
}
