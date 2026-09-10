using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class IconDemoLayoutTests
{
    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(192)]
    public void PreviewCardKeepsMeasuredTypographyInsideItsBounds(int dpi)
    {
        var helperType = typeof(MainForm).Assembly.GetType(
            "MyDmsVn.Bootstrap5WinFormUI.Demo.IconPreviewItemLayout",
            throwOnError: true)!;
        var calculate = helperType.GetMethod("Calculate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var width = Scale(240, dpi);
        var height = Scale(132, dpi);
        using var bitmap = new Bitmap(width, height);
        bitmap.SetResolution(dpi, dpi);
        using var graphics = Graphics.FromImage(bitmap);
        using var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var sourceFont = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        var cardBounds = new Rectangle(0, 0, width, height);

        var layout = calculate.Invoke(
            null,
            new object[] { graphics, cardBounds, dpi, titleFont, sourceFont })!;
        var iconBounds = ReadRectangle(layout, "IconBounds");
        var titleBounds = ReadRectangle(layout, "TitleBounds");
        var sourceBounds = ReadRectangle(layout, "SourceBounds");
        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        var titleHeight = TextRenderer.MeasureText(graphics, "Ag", titleFont, cardBounds.Size, flags).Height;
        var sourceHeight = TextRenderer.MeasureText(graphics, "Ag", sourceFont, cardBounds.Size, flags).Height;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(cardBounds.Contains(iconBounds), Is.True);
            Assert.That(cardBounds.Contains(titleBounds), Is.True);
            Assert.That(cardBounds.Contains(sourceBounds), Is.True);
            Assert.That(titleBounds.Top, Is.GreaterThanOrEqualTo(iconBounds.Bottom));
            Assert.That(sourceBounds.Top, Is.GreaterThanOrEqualTo(titleBounds.Bottom));
            Assert.That(titleBounds.Height, Is.GreaterThanOrEqualTo(titleHeight));
            Assert.That(sourceBounds.Height, Is.GreaterThanOrEqualTo(sourceHeight));
        }));
    }

    private static Rectangle ReadRectangle(object instance, string propertyName)
    {
        return (Rectangle)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;
    }

    private static int Scale(int logicalPixels, int dpi)
    {
        return (int)Math.Round(logicalPixels * dpi / 96d, MidpointRounding.AwayFromZero);
    }
}
