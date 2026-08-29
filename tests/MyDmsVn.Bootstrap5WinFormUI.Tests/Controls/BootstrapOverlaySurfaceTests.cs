using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapOverlaySurfaceTests
{
    [Test]
    public void AttachDetachPreservesCallerOwnership()
    {
        using var surface = new BootstrapOverlaySurface();
        using var content = new PreferredSizeControl(new Size(100, 40));
        var disposed = 0;
        content.Disposed += (_, _) => disposed++;

        Assert.That(surface.HostedContent, Is.Null);
        surface.AttachContent(content);
        Assert.That(content.Parent, Is.SameAs(surface));
        Assert.Throws<InvalidOperationException>((Action)(() => surface.AttachContent(new Panel())));

        var detached = surface.DetachContent();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(detached, Is.SameAs(content));
            Assert.That(content.Parent, Is.Null);
            Assert.That(disposed, Is.Zero);
        }));
    }

    [Test]
    public void AttachRejectsDisposedAndAlreadyParentedContent()
    {
        using var surface = new BootstrapOverlaySurface();
        var disposed = new Panel();
        disposed.Dispose();
        using var parent = new Panel();
        using var parented = new Panel();
        parent.Controls.Add(parented);

        Assert.Throws<ArgumentException>((Action)(() => surface.AttachContent(disposed)));
        Assert.Throws<InvalidOperationException>((Action)(() => surface.AttachContent(parented)));
    }

    [TestCase(96, 126, 58)]
    [TestCase(120, 132, 62)]
    [TestCase(144, 140, 68)]
    [TestCase(168, 146, 72)]
    [TestCase(192, 152, 76)]
    public void PreferredSizeScalesPaddingAndBorder(int dpi, int expectedWidth, int expectedHeight)
    {
        using var surface = new BootstrapOverlaySurface
        {
            LogicalContentPadding = new Padding(12, 8, 12, 8)
        };
        using var content = new PreferredSizeControl(new Size(100, 40));
        surface.AttachContent(content);
        surface.ApplyTheme(BootstrapTheme.CreateDefault(BootstrapThemeMode.Light), dpi);

        Assert.That(surface.GetPreferredSize(Size.Empty), Is.EqualTo(new Size(expectedWidth, expectedHeight)));
    }

    private sealed class PreferredSizeControl : Control
    {
        private readonly Size _preferredSize;

        public PreferredSizeControl(Size preferredSize)
        {
            _preferredSize = preferredSize;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            return _preferredSize;
        }
    }
}
