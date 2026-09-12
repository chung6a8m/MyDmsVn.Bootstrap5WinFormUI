using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapStatusStripTests
{
    [Test]
    public void PublicContractIsThinNativeStatusStrip()
    {
        using var strip = new BootstrapStatusStrip();
        var label = new ToolStripStatusLabel("Ready");
        var progress = new ToolStripProgressBar();
        strip.Items.AddRange(new ToolStripItem[] { label, progress });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapStatusStrip).BaseType, Is.EqualTo(typeof(StatusStrip)));
            Assert.That(strip.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(strip.Renderer, Is.TypeOf<BootstrapToolStripRenderer>());
            Assert.That(strip.Items[0], Is.SameAs(label));
            Assert.That(strip.Items[1], Is.SameAs(progress));
        }));
    }

    [TestCase(ToolStripStatusLabelBorderSides.None, 0)]
    [TestCase(ToolStripStatusLabelBorderSides.Left, 1)]
    [TestCase(ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top, 2)]
    [TestCase(ToolStripStatusLabelBorderSides.All, 4)]
    public void StatusBorderGeometryContainsOnlyRequestedNativeSides(ToolStripStatusLabelBorderSides sides, int count)
    {
        var lines = BootstrapToolStripRenderLogic.ResolveStatusBorderLines(new Rectangle(0, 0, 30, 20), sides);
        Assert.That(lines, Has.Count.EqualTo(count));
    }

    [TestCase(96)]
    [TestCase(144)]
    [TestCase(192)]
    public void SizingGripGeometryUsesDpiScaledDots(int dpi)
    {
        var dotSize = BootstrapToolStripRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi).GripDotSize;
        var dots = BootstrapToolStripRenderLogic.ResolveSizingGripDots(new Size(120, 24), dotSize, rightToLeft: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dots, Has.Count.EqualTo(6));
            Assert.That(dots.All(dot => dot.Width == dotSize && dot.Height == dotSize), Is.True);
            Assert.That(dots.All(dot => dot.Right <= 120 && dot.Bottom <= 24), Is.True);
        }));
    }

    [Test]
    public void StatusLabelRenderingPreservesRepresentativeBorder3DStyles()
    {
        var styles = new[] { Border3DStyle.Flat, Border3DStyle.Raised, Border3DStyle.Sunken, Border3DStyle.Etched };
        var signatures = styles.Select(RenderStatusBorder).ToArray();

        Assert.That(signatures.Distinct().Count(), Is.EqualTo(styles.Length));
    }

    private static string RenderStatusBorder(Border3DStyle style)
    {
        using var strip = new BootstrapStatusStrip();
        var label = new ToolStripStatusLabel("Border")
        {
            AutoSize = false,
            Size = new Size(48, 24),
            BorderSides = ToolStripStatusLabelBorderSides.All,
            BorderStyle = style
        };
        strip.Items.Add(label);
        using var bitmap = new Bitmap(label.Width, label.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var method = typeof(BootstrapToolStripRendererBase).GetMethod(
                "OnRenderToolStripStatusLabelBackground",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method!.Invoke(strip.Renderer, new object[] { new ToolStripItemRenderEventArgs(graphics, label) });
        }

        return string.Join(",", Enumerable.Range(0, bitmap.Height)
            .SelectMany(y => Enumerable.Range(0, bitmap.Width).Select(x => bitmap.GetPixel(x, y).ToArgb())));
    }
}
