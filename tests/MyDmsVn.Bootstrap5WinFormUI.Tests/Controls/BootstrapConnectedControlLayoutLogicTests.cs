using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapConnectedControlLayoutLogicTests
{
    [TestCase(96, 1)]
    [TestCase(120, 1)]
    [TestCase(144, 2)]
    [TestCase(168, 2)]
    [TestCase(192, 2)]
    public void SeamOverlapScalesThemeBorderWidth(int dpi, int expected)
    {
        Assert.That(ResolveSeamOverlap(BootstrapThemeMetrics.Default, dpi), Is.EqualTo(expected));
    }

    [Test]
    public void HorizontalCornersKeepOnlyConnectedOuterEdgesRounded()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(ResolveCornerRadius(Orientation.Horizontal, 0, 3, 10f),
                Is.EqualTo(new CornerRadius(10f, 0f, 0f, 10f)));
            Assert.That(ResolveCornerRadius(Orientation.Horizontal, 1, 3, 10f),
                Is.EqualTo(CornerRadius.Empty));
            Assert.That(ResolveCornerRadius(Orientation.Horizontal, 2, 3, 10f),
                Is.EqualTo(new CornerRadius(0f, 10f, 10f, 0f)));
        }));
    }

    [Test]
    public void VerticalAndSingleCornersUseExpectedOuterShapes()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(ResolveCornerRadius(Orientation.Vertical, 0, 3, 8f),
                Is.EqualTo(new CornerRadius(8f, 8f, 0f, 0f)));
            Assert.That(ResolveCornerRadius(Orientation.Vertical, 1, 3, 8f),
                Is.EqualTo(CornerRadius.Empty));
            Assert.That(ResolveCornerRadius(Orientation.Vertical, 2, 3, 8f),
                Is.EqualTo(new CornerRadius(0f, 0f, 8f, 8f)));
            Assert.That(ResolveCornerRadius(Orientation.Horizontal, 0, 1, 6f),
                Is.EqualTo(new CornerRadius(6f)));
        }));
    }

    private static int ResolveSeamOverlap(BootstrapThemeMetrics metrics, int dpi)
    {
        return (int)Invoke("ResolveSeamOverlap", metrics, dpi);
    }

    private static CornerRadius ResolveCornerRadius(
        Orientation orientation,
        int index,
        int count,
        float radius)
    {
        return (CornerRadius)Invoke("ResolveCornerRadius", orientation, index, count, radius);
    }

    private static object Invoke(string methodName, params object[] arguments)
    {
        var type = typeof(BootstrapButtonGroup).Assembly.GetType(
            "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapConnectedControlLayoutLogic");
        Assert.That(type, Is.Not.Null, "The shared connected-control layout helper must exist.");
        var method = type!.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .SingleOrDefault(candidate => candidate.Name == methodName);
        Assert.That(method, Is.Not.Null, $"Missing internal layout method {methodName}.");
        return method!.Invoke(null, arguments)!;
    }
}
