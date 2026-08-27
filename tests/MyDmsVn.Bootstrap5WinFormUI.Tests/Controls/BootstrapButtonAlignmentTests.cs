using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapButtonAlignmentTests
{
    [Test]
    public void InheritedTextAlignPositionsTheWholeNormalContentGroup()
    {
        using var button = new BootstrapButton
        {
            Size = new Size(240, 40),
            Text = "Navigation",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var method = typeof(BootstrapButton).GetMethod("GetNormalContentLayout", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        var layout = (HorizontalContentLayout)method!.Invoke(button, new object[] { BootstrapThemeManager.CurrentTheme })!;

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = button.DeviceDpi > 0 ? button.DeviceDpi : DpiScaler.DefaultDpi;
        var expectedLeft = DpiScaler.Scale(
            BootstrapButtonRenderLogic.GetLogicalHorizontalPadding(theme.Metrics, button.ButtonSize),
            dpi);

        Assert.That(layout.ContentBounds.Left, Is.EqualTo(expectedLeft));
    }
}
