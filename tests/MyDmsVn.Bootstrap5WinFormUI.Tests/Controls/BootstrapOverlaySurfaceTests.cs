using System;
using System.Drawing;
using System.Drawing.Imaging;
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
    private static readonly Color OpaqueContentColor = Color.Fuchsia;

    [Test]
    public void AttachDetachPreservesCallerOwnership()
    {
        using var surface = new BootstrapOverlaySurface();
        using var callerRegion = new Region(new Rectangle(0, 0, 80, 30));
        using var content = new PreferredSizeControl(new Size(100, 40));
        content.Region = callerRegion;
        var disposed = 0;
        content.Disposed += (_, _) => disposed++;

        Assert.That(surface.HostedContent, Is.Null);
        surface.AttachContent(content);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(content.Parent, Is.Not.Null);
            Assert.That(content.Parent!.Parent, Is.SameAs(surface));
            Assert.That(content.Region, Is.SameAs(callerRegion));
        }));
        Assert.Throws<InvalidOperationException>((Action)(() => surface.AttachContent(new Panel())));

        var detached = surface.DetachContent();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(detached, Is.SameAs(content));
            Assert.That(content.Parent, Is.Null);
            Assert.That(content.Region, Is.SameAs(callerRegion));
            Assert.That(callerRegion.IsVisible(10, 10), Is.True);
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

    [TestCase(96, 4, 16)]
    [TestCase(120, 5, 20)]
    [TestCase(144, 6, 24)]
    [TestCase(168, 7, 28)]
    [TestCase(192, 8, 32)]
    public void OpaqueZeroPaddingContentCannotPaintOverAnyRoundedInteriorCorner(
        int dpi,
        int expectedBorderWidth,
        int expectedRadius)
    {
        using var surface = new BootstrapOverlaySurface
        {
            Size = new Size(140, 80),
            LogicalContentPadding = Padding.Empty,
            LogicalBorderRadius = 16
        };
        using var content = new Panel { BackColor = OpaqueContentColor };
        surface.AttachContent(content);
        var theme = CreateThickBorderTheme();
        surface.ApplyTheme(theme, dpi);
        surface.CreateControl();
        content.CreateControl();
        surface.PerformLayout();
        Application.DoEvents();

        using var bitmap = ComposeHostedContent(surface, content);

        var host = content.Parent!;
        Assert.That(
            bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2).ToArgb(),
            Is.EqualTo(OpaqueContentColor.ToArgb()),
            "The composed bitmap must contain the opaque hosted content before corner pixels are evaluated.");
        Assert.That(host.Region, Is.Not.Null, "Opaque hosted content requires an inner rounded clip region.");
        var hostRegion = host.Region!;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Bounds, Is.EqualTo(new Rectangle(
                expectedBorderWidth,
                expectedBorderWidth,
                surface.Width - (2 * expectedBorderWidth),
                surface.Height - (2 * expectedBorderWidth))));
            Assert.That(hostRegion.IsVisible(0.5f, 0.5f), Is.False);
            Assert.That(hostRegion.IsVisible(host.Width / 2f, host.Height / 2f), Is.True);
        }));
        Assert.Multiple((Action)(() =>
        {
            AssertCornerExcludesOpaqueContent(host, bitmap, expectedRadius, expectedRadius - expectedBorderWidth, Corner.TopLeft);
            AssertCornerExcludesOpaqueContent(host, bitmap, expectedRadius, expectedRadius - expectedBorderWidth, Corner.TopRight);
            AssertCornerExcludesOpaqueContent(host, bitmap, expectedRadius, expectedRadius - expectedBorderWidth, Corner.BottomLeft);
            AssertCornerExcludesOpaqueContent(host, bitmap, expectedRadius, expectedRadius - expectedBorderWidth, Corner.BottomRight);
        }));
    }

    [TestCase(96, 16)]
    [TestCase(120, 20)]
    [TestCase(144, 24)]
    [TestCase(168, 28)]
    [TestCase(192, 32)]
    public void OuterClipPreservesRenderedAntiAliasCoverageAtEveryCorner(int dpi, int expectedRadius)
    {
        using var surface = new BootstrapOverlaySurface
        {
            Size = new Size(140, 80),
            LogicalContentPadding = Padding.Empty,
            LogicalBorderRadius = 16
        };
        using var content = new Panel { BackColor = OpaqueContentColor };
        surface.AttachContent(content);
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        surface.ApplyTheme(theme, dpi);
        surface.CreateControl();
        content.CreateControl();
        surface.PerformLayout();
        Application.DoEvents();

        Assert.That(surface.Region, Is.Not.Null);
        using var outerClip = surface.Region!.Clone();
        surface.Region = null;
        using var rendered = new Bitmap(surface.Width, surface.Height, PixelFormat.Format32bppArgb);
        surface.DrawToBitmap(rendered, surface.ClientRectangle);
        using var clipped = ApplyClip(rendered, outerClip);

        Assert.Multiple((Action)(() =>
        {
            AssertCornerPreservesAntiAliasCoverage(rendered, clipped, expectedRadius, theme, Corner.TopLeft);
            AssertCornerPreservesAntiAliasCoverage(rendered, clipped, expectedRadius, theme, Corner.TopRight);
            AssertCornerPreservesAntiAliasCoverage(rendered, clipped, expectedRadius, theme, Corner.BottomLeft);
            AssertCornerPreservesAntiAliasCoverage(rendered, clipped, expectedRadius, theme, Corner.BottomRight);
            Assert.That(outerClip.IsVisible(0.5f, 0.5f), Is.False, "The conservative clip must keep the rounded window silhouette.");
        }));
    }

    private static BootstrapTheme CreateThickBorderTheme()
    {
        var defaults = BootstrapThemeMetrics.Default;
        var metrics = new BootstrapThemeMetrics(
            defaults.ControlHeightSmall,
            defaults.ControlHeight,
            defaults.ControlHeightLarge,
            defaults.RadiusSmall,
            defaults.Radius,
            defaults.RadiusLarge,
            borderWidth: 4,
            defaults.FocusBorderWidth,
            defaults.SpacingXS,
            defaults.SpacingSM,
            defaults.SpacingMD,
            defaults.SpacingLG,
            defaults.SpacingXL);
        return new BootstrapTheme(
            BootstrapThemeMode.Light,
            BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light),
            metrics,
            BootstrapThemeTypography.Default);
    }

    private static Bitmap ComposeHostedContent(BootstrapOverlaySurface surface, Control content)
    {
        var bitmap = new Bitmap(surface.Width, surface.Height);
        using var contentBitmap = new Bitmap(content.Width, content.Height);
        content.DrawToBitmap(contentBitmap, content.ClientRectangle);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);

        if (surface.Region is not null)
        {
            using var outerClip = surface.Region.Clone();
            graphics.SetClip(outerClip, System.Drawing.Drawing2D.CombineMode.Replace);
        }

        var host = content.Parent!;
        if (!ReferenceEquals(host, surface) && host.Region is not null)
        {
            using var innerClip = host.Region.Clone();
            innerClip.Translate(host.Left, host.Top);
            graphics.SetClip(innerClip, System.Drawing.Drawing2D.CombineMode.Intersect);
        }

        graphics.DrawImageUnscaled(
            contentBitmap,
            host.Left + content.Left,
            host.Top + content.Top);
        return bitmap;
    }

    private static Bitmap ApplyClip(Bitmap source, Region clip)
    {
        var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SetClip(clip, System.Drawing.Drawing2D.CombineMode.Replace);
        graphics.DrawImageUnscaled(source, Point.Empty);
        return bitmap;
    }

    private static void AssertCornerPreservesAntiAliasCoverage(
        Bitmap rendered,
        Bitmap clipped,
        int radius,
        BootstrapTheme theme,
        Corner corner)
    {
        var antiAliasedPixels = 0;
        var lostPixels = 0;
        var outside = Color.Black.ToArgb();
        var surface = theme.Colors.Surface.ToArgb();
        var border = theme.Colors.Border.ToArgb();
        var content = OpaqueContentColor.ToArgb();
        for (var y = 0; y <= radius; y++)
        {
            for (var x = 0; x <= radius; x++)
            {
                var sampleX = corner == Corner.TopLeft || corner == Corner.BottomLeft
                    ? x
                    : rendered.Width - 1 - x;
                var sampleY = corner == Corner.TopLeft || corner == Corner.TopRight
                    ? y
                    : rendered.Height - 1 - y;
                var before = rendered.GetPixel(sampleX, sampleY);
                var beforeArgb = before.ToArgb();
                if (beforeArgb == outside
                    || beforeArgb == surface
                    || beforeArgb == border
                    || beforeArgb == content)
                {
                    continue;
                }

                antiAliasedPixels++;
                if (clipped.GetPixel(sampleX, sampleY).ToArgb() != beforeArgb)
                {
                    lostPixels++;
                }
            }
        }

        Assert.That(antiAliasedPixels, Is.GreaterThan(0), $"The real surface render must contain anti-aliased coverage at {corner}.");
        Assert.That(lostPixels, Is.Zero, $"The outer Region discarded anti-aliased coverage at {corner}.");
    }

    private static void AssertCornerExcludesOpaqueContent(
        Control host,
        Bitmap bitmap,
        int outerRadius,
        int innerRadius,
        Corner corner)
    {
        var sampled = 0;
        var opaquePixels = 0;
        for (var y = 0; y < outerRadius; y++)
        {
            for (var x = 0; x < outerRadius; x++)
            {
                var dx = innerRadius - (x + 0.5d);
                var dy = innerRadius - (y + 0.5d);
                var distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance <= innerRadius + 1d || distance >= outerRadius - 1d)
                {
                    continue;
                }

                var localX = corner == Corner.TopLeft || corner == Corner.BottomLeft
                    ? x
                    : host.Width - 1 - x;
                var localY = corner == Corner.TopLeft || corner == Corner.TopRight
                    ? y
                    : host.Height - 1 - y;
                var sampleX = host.Left + localX;
                var sampleY = host.Top + localY;
                sampled++;
                if (bitmap.GetPixel(sampleX, sampleY).ToArgb() == OpaqueContentColor.ToArgb())
                {
                    opaquePixels++;
                }
            }
        }

        Assert.That(sampled, Is.GreaterThan(0));
        Assert.That(opaquePixels, Is.Zero, $"Opaque content covered the {corner} rounded interior corner.");
    }

    private enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
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
