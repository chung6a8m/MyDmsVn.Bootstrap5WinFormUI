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
public sealed class BootstrapNotificationCenterRenderLogicTests
{
    [TestCase(96, 12, 8)]
    [TestCase(120, 15, 10)]
    [TestCase(144, 18, 12)]
    [TestCase(168, 21, 14)]
    [TestCase(192, 24, 16)]
    public void MetricsScaleAcrossSupportedDpiMatrix(int dpi, int expectedPadding, int expectedMarker)
    {
        var metrics = BootstrapNotificationCenterRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.Padding, Is.EqualTo(expectedPadding));
            Assert.That(metrics.UnreadMarkerSize, Is.EqualTo(expectedMarker));
            Assert.That(metrics.ContentSpacing, Is.EqualTo(expectedMarker));
        }));
    }

    [Test]
    public void RowHeightGrowsWithWrappedBodyAndOptionalTitle()
    {
        var metrics = BootstrapNotificationCenterRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96);
        var shortRow = BootstrapNotificationCenterRenderLogic.CalculateRowHeight(
            360, metrics, Size.Empty, new Size(300, 18), new Size(100, 16), hasTitle: false);
        var longRow = BootstrapNotificationCenterRenderLogic.CalculateRowHeight(
            360, metrics, new Size(300, 18), new Size(300, 90), new Size(100, 16), hasTitle: true);

        Assert.That(longRow, Is.GreaterThan(shortRow));
    }

    [Test]
    public void RowLayoutContainsMarkerTitleBodyAndTimestampAtNarrowWidth()
    {
        var metrics = BootstrapNotificationCenterRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96);
        var height = BootstrapNotificationCenterRenderLogic.CalculateRowHeight(
            120, metrics, new Size(80, 18), new Size(80, 54), new Size(70, 16), hasTitle: true);
        var layout = BootstrapNotificationCenterRenderLogic.CalculateRowLayout(
            new Rectangle(5, 7, 120, height),
            metrics,
            new Size(80, 18),
            new Size(80, 54),
            new Size(70, 16),
            hasTitle: true);

        Assert.Multiple((Action)(() =>
        {
            AssertContained(layout.RowBounds, layout.UnreadMarkerBounds);
            AssertContained(layout.RowBounds, layout.TitleBounds);
            AssertContained(layout.RowBounds, layout.BodyBounds);
            AssertContained(layout.RowBounds, layout.TimestampBounds);
            Assert.That(layout.BodyBounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(layout.BodyBounds.Height, Is.GreaterThanOrEqualTo(0));
        }));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void PaletteUsesThemeAndSemanticVariantWithoutHardCodedHistoryColors(BootstrapThemeMode mode)
    {
        var colors = BootstrapThemeColors.CreateDefault(mode);
        var unread = BootstrapNotificationCenterRenderLogic.ResolvePalette(colors, BootstrapVariant.Success, selected: false, isRead: false);
        var read = BootstrapNotificationCenterRenderLogic.ResolvePalette(colors, BootstrapVariant.Success, selected: false, isRead: true);
        var selected = BootstrapNotificationCenterRenderLogic.ResolvePalette(colors, BootstrapVariant.Success, selected: true, isRead: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(unread.Surface, Is.EqualTo(colors.Surface));
            Assert.That(unread.Foreground, Is.EqualTo(colors.Text));
            Assert.That(read.Foreground, Is.EqualTo(colors.MutedText));
            Assert.That(selected.Surface, Is.EqualTo(colors.Active));
            Assert.That(unread.Marker, Is.EqualTo(BootstrapVariantColorResolver.Resolve(colors, BootstrapVariant.Success)));
        }));
    }

    [Test]
    public void InvalidInputsAreRejectedBeforeReturningGeometryOrPalette()
    {
        var metrics = BootstrapNotificationCenterRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96);
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapNotificationCenterRenderLogic.ResolveMetrics(null!, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapNotificationCenterRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 0)));
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapNotificationCenterRenderLogic.ResolvePalette(null!, BootstrapVariant.Primary, false, false)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapNotificationCenterRenderLogic.ResolvePalette(BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light), (BootstrapVariant)999, false, false)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapNotificationCenterRenderLogic.CalculateRowHeight(0, metrics, Size.Empty, Size.Empty, Size.Empty, false)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapNotificationCenterRenderLogic.CalculateRowHeight(100, metrics, new Size(-1, 1), Size.Empty, Size.Empty, true)));
        }));
    }

    [Test]
    public void HistoryListUsesNativeOwnerDrawAndActivatesOnlyEnterOrSpace()
    {
        using var list = new BootstrapNotificationHistoryListBox();
        var item = new BootstrapToastHistoryItem(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Title",
            "Body",
            BootstrapVariant.Info,
            false);
        list.SetHistory(new[] { item });
        list.SelectedIndex = 0;
        var activations = 0;
        BootstrapToastHistoryItem? activated = null;
        list.ItemActivated += (_, e) =>
        {
            activations++;
            activated = e.Item;
        };

        list.ProcessActivationKeyForTests(Keys.Down);
        list.ProcessActivationKeyForTests(Keys.Enter);
        list.ProcessActivationKeyForTests(Keys.Space);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.DrawMode, Is.EqualTo(DrawMode.OwnerDrawVariable));
            Assert.That(list.IntegralHeight, Is.False);
            Assert.That(list.BorderStyle, Is.EqualTo(BorderStyle.None));
            Assert.That(list.Items.Count, Is.EqualTo(1));
            Assert.That(activations, Is.EqualTo(2));
            Assert.That(activated, Is.SameAs(item));
        }));
    }

    private static void AssertContained(Rectangle outer, Rectangle inner)
    {
        if (inner.Width == 0 || inner.Height == 0)
        {
            return;
        }

        Assert.That(inner.Left, Is.GreaterThanOrEqualTo(outer.Left));
        Assert.That(inner.Top, Is.GreaterThanOrEqualTo(outer.Top));
        Assert.That(inner.Right, Is.LessThanOrEqualTo(outer.Right));
        Assert.That(inner.Bottom, Is.LessThanOrEqualTo(outer.Bottom));
    }
}
