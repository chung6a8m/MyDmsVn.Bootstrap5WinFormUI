using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Rendering;

[TestFixture]
public sealed class DpiScalerTests
{
    [Test]
    public void ScaleAtDefaultDpiReturnsOriginalValue()
    {
        Assert.That(DpiScaler.Scale(32, DpiScaler.DefaultDpi), Is.EqualTo(32));
        Assert.That(DpiScaler.Scale(1.5f, DpiScaler.DefaultDpi), Is.EqualTo(1.5f).Within(0.0001f));
    }

    [TestCase(120, 40)]
    [TestCase(144, 48)]
    [TestCase(168, 56)]
    [TestCase(192, 64)]
    public void ScaleUsesNinetySixDpiBaseline(int dpi, int expected)
    {
        Assert.That(DpiScaler.Scale(32, dpi), Is.EqualTo(expected));
    }

    [Test]
    public void ScaleSupportsCommonGeometryTypes()
    {
        var size = DpiScaler.Scale(new Size(32, 24), 144);
        var padding = DpiScaler.Scale(new Padding(4, 8, 12, 16), 144);
        var rectangle = DpiScaler.Scale(new Rectangle(4, 8, 20, 24), 144);

        Assert.That(size, Is.EqualTo(new Size(48, 36)));
        Assert.That(padding, Is.EqualTo(new Padding(6, 12, 18, 24)));
        Assert.That(rectangle, Is.EqualTo(new Rectangle(6, 12, 30, 36)));
    }

    [TestCase(0)]
    [TestCase(-96)]
    public void ScaleRejectsInvalidDpi(int dpi)
    {
        Action action = () => DpiScaler.Scale(32, dpi);

        Assert.That(action, Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
