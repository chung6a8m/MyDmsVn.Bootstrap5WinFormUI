using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectVisualRegressionTests
{
    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(192)]
    public void SelectionLayoutScalesActionMetricsAtSupportedDpi(int dpi)
    {
        var result = BootstrapSelectSelectionLayout.Create(
            new Size(300, Math.Max(32, dpi / 3)),
            BootstrapSelectMode.Single,
            new List<BootstrapSelectItem> { new BootstrapSelectItem(1, "Alpha") },
            allowClear: true,
            rightToLeft: false,
            dpi,
            maximumRows: 3);

        Assert.That(result.ArrowBounds.Width, Is.EqualTo(Math.Max(20, (int)Math.Round(20d * dpi / 96d))));
        Assert.That(result.ClearBounds.Width, Is.EqualTo(result.ArrowBounds.Width));
    }

    [Test]
    public void RightToLeftMirrorsMajorAffordances()
    {
        var selected = new List<BootstrapSelectItem> { new BootstrapSelectItem(1, "Alpha") };
        var ltr = BootstrapSelectSelectionLayout.Create(new Size(300, 40), BootstrapSelectMode.Single, selected, true, false, 96, 3);
        var rtl = BootstrapSelectSelectionLayout.Create(new Size(300, 40), BootstrapSelectMode.Single, selected, true, true, 96, 3);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ltr.ArrowBounds.Left, Is.GreaterThan(ltr.ClearBounds.Left));
            Assert.That(rtl.ArrowBounds.Left, Is.LessThan(rtl.ClearBounds.Left));
            Assert.That(rtl.ContentBounds.Left, Is.GreaterThan(ltr.ContentBounds.Left));
        }));
    }

    [Test]
    public void InvalidRoundedShellPaintsRightAndBottomBorderInsideClientBounds()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

            using var host = new Form { ClientSize = new Size(420, 120) };
            using var select = new BootstrapSelect
            {
                Bounds = new Rectangle(20, 20, 340, 40),
                BorderRadius = 8,
                ValidationState = BootstrapValidationState.Invalid
            };
            host.Controls.Add(select);
            host.CreateControl();
            select.CreateControl();

            using var bitmap = new Bitmap(select.Width, select.Height);
            select.DrawToBitmap(bitmap, select.ClientRectangle);

            var colors = BootstrapThemeManager.CurrentTheme.Colors;
            var rightEdge = bitmap.GetPixel(select.Width - 1, select.Height / 2);
            var bottomEdge = bitmap.GetPixel(select.Width / 2, select.Height - 1);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(
                    ColorDistanceSquared(rightEdge, colors.Danger),
                    Is.LessThan(ColorDistanceSquared(rightEdge, colors.Surface)));
                Assert.That(
                    ColorDistanceSquared(bottomEdge, colors.Danger),
                    Is.LessThan(ColorDistanceSquared(bottomEdge, colors.Surface)));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void FocusedRoundedShellPaintsFocusBorderAtFullConfiguredThickness()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

            using var host = new Form
            {
                ShowInTaskbar = false,
                ClientSize = new Size(420, 120)
            };
            using var select = new BootstrapSelect
            {
                Bounds = new Rectangle(20, 20, 340, 40)
            };
            host.Controls.Add(select);
            host.Show();
            Application.DoEvents();

            Assert.That(select.Focus(), Is.True);
            Application.DoEvents();
            Assert.That(
                select.ContainsFocus || select.Focused,
                Is.True,
                "The bitmap regression must exercise the focused Select paint path.");

            using var bitmap = new Bitmap(select.Width, select.Height);
            select.DrawToBitmap(bitmap, select.ClientRectangle);

            var colors = BootstrapThemeManager.CurrentTheme.Colors;
            var rightEdge = bitmap.GetPixel(select.Width - 1, select.Height / 2);
            var innerRightEdge = bitmap.GetPixel(select.Width - 2, select.Height / 2);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(
                    ColorDistanceSquared(rightEdge, colors.Focus),
                    Is.LessThan(ColorDistanceSquared(rightEdge, colors.Surface)));
                Assert.That(
                    ColorDistanceSquared(innerRightEdge, colors.Focus),
                    Is.LessThan(ColorDistanceSquared(innerRightEdge, colors.Surface)),
                    "The inner edge pixel must be dominated by the configured 2px focus stroke.");
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private static int ColorDistanceSquared(Color left, Color right)
    {
        var red = left.R - right.R;
        var green = left.G - right.G;
        var blue = left.B - right.B;
        return (red * red) + (green * green) + (blue * blue);
    }
}
