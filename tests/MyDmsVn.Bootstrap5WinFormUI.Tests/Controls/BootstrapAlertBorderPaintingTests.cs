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
public sealed class BootstrapAlertBorderPaintingTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void SquareAlertPaintsRightAndBottomBorderInsideClientBounds()
    {
        using var host = new Form { ClientSize = new Size(260, 120) };
        using var alert = new BootstrapAlert
        {
            Bounds = new Rectangle(10, 10, 220, 64),
            BorderRadius = 0,
            Variant = BootstrapVariant.Primary,
            Text = string.Empty
        };
        host.Controls.Add(alert);
        host.CreateControl();
        alert.CreateControl();

        using var bitmap = new Bitmap(alert.Width, alert.Height);
        alert.DrawToBitmap(bitmap, alert.ClientRectangle);

        var palette = BootstrapAlertRenderLogic.ResolvePalette(
            BootstrapThemeManager.CurrentTheme.Colors,
            alert.Variant,
            enabled: true);
        var rightEdge = bitmap.GetPixel(alert.Width - 1, alert.Height / 2);
        var bottomEdge = bitmap.GetPixel(alert.Width / 2, alert.Height - 1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                ColorDistanceSquared(rightEdge, palette.Border),
                Is.LessThan(ColorDistanceSquared(rightEdge, palette.Surface)),
                "The right-most client pixel should be dominated by the border stroke rather than the alert surface.");
            Assert.That(
                ColorDistanceSquared(bottomEdge, palette.Border),
                Is.LessThan(ColorDistanceSquared(bottomEdge, palette.Surface)),
                "The bottom-most client pixel should be dominated by the border stroke rather than the alert surface.");
        }));
    }

    private static int ColorDistanceSquared(Color left, Color right)
    {
        var red = left.R - right.R;
        var green = left.G - right.G;
        var blue = left.B - right.B;
        return (red * red) + (green * green) + (blue * blue);
    }
}
