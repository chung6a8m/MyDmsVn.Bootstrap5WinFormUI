using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapTreeViewLayoutTests
{
    [Test]
    public void Ltr96_AllSlotsRemainOrderedAndNonOverlapping()
    {
        var layout = BootstrapTreeViewLayout.Calculate(CreateInput(
            dpi: 96,
            nativeLabelBounds: new Rectangle(112, 0, 120, 24),
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.ExpanderBounds.Right, Is.LessThanOrEqualTo(layout.StateImageBounds.Left));
            Assert.That(layout.StateImageBounds.Right, Is.LessThanOrEqualTo(layout.NodeImageBounds.Left));
            Assert.That(layout.NodeImageBounds.Right, Is.LessThanOrEqualTo(layout.TextBounds.Left));
            AssertContained(layout.RowBounds, layout.ExpanderBounds);
            AssertContained(layout.RowBounds, layout.StateImageBounds);
            AssertContained(layout.RowBounds, layout.NodeImageBounds);
            AssertContained(layout.RowBounds, layout.TextBounds);
        }));
    }

    [TestCase(120)]
    [TestCase(144)]
    [TestCase(168)]
    [TestCase(192)]
    public void HighDpi_FrameworkOwnedGlyphsAndGapsScaleAndStayVisible(int dpi)
    {
        var scale = dpi / 96.0;
        var rowHeight = DpiScaler.Scale(24, dpi);
        var labelLeft = DpiScaler.Scale(112, dpi);
        var clientWidth = DpiScaler.Scale(320, dpi);
        var imageSize = DpiScaler.Scale(new Size(16, 16), dpi);
        var stateSlot = DpiScaler.Scale(19, dpi);
        var layout = BootstrapTreeViewLayout.Calculate(CreateInput(
            dpi: dpi,
            clientBounds: new Rectangle(0, 0, clientWidth, rowHeight),
            drawBounds: new Rectangle(0, 0, clientWidth, rowHeight),
            nativeLabelBounds: new Rectangle(labelLeft, 0, DpiScaler.Scale(120, dpi), rowHeight),
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: stateSlot,
            hasNodeImage: true,
            nodeImageSize: imageSize));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.ExpanderBounds.Width, Is.GreaterThanOrEqualTo((int)Math.Floor(9 * scale)));
            Assert.That(layout.ExpanderBounds.Right, Is.LessThanOrEqualTo(layout.StateImageBounds.Left));
            Assert.That(layout.StateImageBounds.Right, Is.LessThanOrEqualTo(layout.NodeImageBounds.Left));
            Assert.That(layout.NodeImageBounds.Right, Is.LessThanOrEqualTo(layout.TextBounds.Left));
            AssertContained(layout.RowBounds, layout.ExpanderBounds);
            AssertContained(layout.RowBounds, layout.StateImageBounds);
            AssertContained(layout.RowBounds, layout.NodeImageBounds);
            AssertContained(layout.RowBounds, layout.TextBounds);
        }));
    }

    [Test]
    public void DpiScaling_IsMonotonicAcrossSupportedMatrix()
    {
        var previousExpanderWidth = 0;
        var previousGap = 0;

        foreach (var dpi in new[] { 96, 120, 144, 168, 192 })
        {
            var rowHeight = DpiScaler.Scale(24, dpi);
            var layout = BootstrapTreeViewLayout.Calculate(CreateInput(
                dpi: dpi,
                clientBounds: new Rectangle(0, 0, DpiScaler.Scale(360, dpi), rowHeight),
                drawBounds: new Rectangle(0, 0, DpiScaler.Scale(360, dpi), rowHeight),
                nativeLabelBounds: new Rectangle(DpiScaler.Scale(128, dpi), 0, DpiScaler.Scale(120, dpi), rowHeight),
                hasExpander: true,
                hasStateImage: true,
                nativeStateImageSlotWidth: DpiScaler.Scale(19, dpi),
                hasNodeImage: true,
                nodeImageSize: DpiScaler.Scale(new Size(16, 16), dpi)));
            var gap = layout.TextBounds.Left - layout.NodeImageBounds.Right;

            Assert.That(layout.ExpanderBounds.Width, Is.GreaterThanOrEqualTo(previousExpanderWidth), $"expander width at {dpi} DPI");
            Assert.That(gap, Is.GreaterThanOrEqualTo(previousGap), $"image/text gap at {dpi} DPI");
            previousExpanderWidth = layout.ExpanderBounds.Width;
            previousGap = gap;
        }
    }

    [Test]
    public void NarrowClient_ClipsEveryRectangleWithoutNegativeSizes()
    {
        var layout = BootstrapTreeViewLayout.Calculate(CreateInput(
            clientBounds: new Rectangle(0, 0, 26, 24),
            drawBounds: new Rectangle(0, 0, 26, 24),
            nativeLabelBounds: new Rectangle(22, 0, 120, 24),
            nodeLevel: 4,
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16)));

        foreach (var bounds in GetAllBounds(layout))
        {
            Assert.That(bounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(bounds.Height, Is.GreaterThanOrEqualTo(0));
            AssertContained(layout.RowBounds, bounds);
        }
    }

    [Test]
    public void HorizontallyShiftedNativeLabel_ClipsInsteadOfReconstructingHierarchy()
    {
        var layout = BootstrapTreeViewLayout.Calculate(CreateInput(
            clientBounds: new Rectangle(0, 0, 120, 24),
            drawBounds: new Rectangle(-80, 0, 240, 24),
            nativeLabelBounds: new Rectangle(-12, 0, 92, 24),
            nodeLevel: 7,
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.TextBounds.Left, Is.EqualTo(0));
            Assert.That(layout.TextBounds.Right, Is.EqualTo(80));
            Assert.That(layout.ExpanderBounds.IsEmpty, Is.True);
            Assert.That(layout.StateImageBounds.IsEmpty, Is.True);
            Assert.That(layout.NodeImageBounds.IsEmpty, Is.True);
        }));
    }

    [Test]
    public void Rtl_UsesNativeMirroredLabelCoordinatesWithoutSecondMirroring()
    {
        var layout = BootstrapTreeViewLayout.Calculate(CreateInput(
            rightToLeft: true,
            nativeLabelBounds: new Rectangle(112, 0, 120, 24),
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.ExpanderBounds.Right, Is.LessThanOrEqualTo(layout.StateImageBounds.Left));
            Assert.That(layout.StateImageBounds.Right, Is.LessThanOrEqualTo(layout.NodeImageBounds.Left));
            Assert.That(layout.NodeImageBounds.Right, Is.LessThanOrEqualTo(layout.TextBounds.Left));
            AssertContained(layout.RowBounds, layout.ExpanderBounds);
            AssertContained(layout.RowBounds, layout.StateImageBounds);
            AssertContained(layout.RowBounds, layout.NodeImageBounds);
            AssertContained(layout.RowBounds, layout.TextBounds);
        }));
    }

    [Test]
    public void FullRowSelection_ChangesSelectionAndFocusOnlyNotNativeAnchoredSlots()
    {
        var label = new Rectangle(112, 0, 120, 24);
        var normal = BootstrapTreeViewLayout.Calculate(CreateInput(
            nativeLabelBounds: label,
            effectiveFullRowSelection: false,
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16)));
        var fullRow = BootstrapTreeViewLayout.Calculate(CreateInput(
            nativeLabelBounds: label,
            effectiveFullRowSelection: true,
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(normal.SelectionBounds, Is.EqualTo(normal.TextBounds));
            Assert.That(normal.FocusBounds, Is.EqualTo(normal.TextBounds));
            Assert.That(fullRow.SelectionBounds, Is.EqualTo(fullRow.RowBounds));
            Assert.That(fullRow.FocusBounds, Is.EqualTo(fullRow.RowBounds));
            Assert.That(fullRow.ExpanderBounds, Is.EqualTo(normal.ExpanderBounds));
            Assert.That(fullRow.StateImageBounds, Is.EqualTo(normal.StateImageBounds));
            Assert.That(fullRow.NodeImageBounds, Is.EqualTo(normal.NodeImageBounds));
            Assert.That(fullRow.TextBounds, Is.EqualTo(normal.TextBounds));
        }));
    }

    [Test]
    public void DrawEventHorizontalBounds_DoNotMoveNativeAnchoredGeometry()
    {
        var baseInput = CreateInput(
            drawBounds: new Rectangle(0, 0, 320, 24),
            nativeLabelBounds: new Rectangle(112, 0, 120, 24),
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16));
        var clippedDrawInput = CreateInput(
            drawBounds: new Rectangle(104, 0, 128, 24),
            nativeLabelBounds: new Rectangle(112, 0, 120, 24),
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16));

        var fullRowDraw = BootstrapTreeViewLayout.Calculate(baseInput);
        var labelOnlyDraw = BootstrapTreeViewLayout.Calculate(clippedDrawInput);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(labelOnlyDraw.RowBounds, Is.EqualTo(fullRowDraw.RowBounds));
            Assert.That(labelOnlyDraw.ExpanderBounds, Is.EqualTo(fullRowDraw.ExpanderBounds));
            Assert.That(labelOnlyDraw.StateImageBounds, Is.EqualTo(fullRowDraw.StateImageBounds));
            Assert.That(labelOnlyDraw.NodeImageBounds, Is.EqualTo(fullRowDraw.NodeImageBounds));
            Assert.That(labelOnlyDraw.TextBounds, Is.EqualTo(fullRowDraw.TextBounds));
        }));
    }

    [Test]
    public void StateImageGeometry_UsesExplicitNativeSlotNotNormalImageSize()
    {
        var smallNodeImage = BootstrapTreeViewLayout.Calculate(CreateInput(
            nativeLabelBounds: new Rectangle(112, 0, 120, 24),
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16)));
        var oversizedNormalImage = BootstrapTreeViewLayout.Calculate(CreateInput(
            nativeLabelBounds: new Rectangle(144, 0, 120, 24),
            hasStateImage: true,
            nativeStateImageSlotWidth: 19,
            hasNodeImage: true,
            nodeImageSize: new Size(48, 48)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(smallNodeImage.StateImageBounds.Width, Is.LessThanOrEqualTo(19));
            Assert.That(oversizedNormalImage.StateImageBounds.Width, Is.EqualTo(smallNodeImage.StateImageBounds.Width));
            Assert.That(oversizedNormalImage.StateImageBounds.Height, Is.EqualTo(smallNodeImage.StateImageBounds.Height));
        }));
    }

    private static BootstrapTreeViewLayoutInput CreateInput(
        int dpi = 96,
        Rectangle? clientBounds = null,
        Rectangle? drawBounds = null,
        Rectangle? nativeLabelBounds = null,
        int nodeLevel = 1,
        bool rightToLeft = false,
        bool effectiveFullRowSelection = false,
        bool hasExpander = false,
        bool hasStateImage = false,
        int nativeStateImageSlotWidth = 0,
        bool hasNodeImage = false,
        Size? nodeImageSize = null)
    {
        return new BootstrapTreeViewLayoutInput(
            clientBounds ?? new Rectangle(0, 0, 320, 24),
            drawBounds ?? new Rectangle(0, 0, 320, 24),
            nativeLabelBounds ?? new Rectangle(80, 0, 120, 24),
            nodeLevel,
            dpi,
            rightToLeft,
            effectiveFullRowSelection,
            hasExpander,
            hasStateImage,
            nativeStateImageSlotWidth,
            hasNodeImage,
            nodeImageSize ?? Size.Empty);
    }

    private static Rectangle[] GetAllBounds(BootstrapTreeViewNodeLayout layout)
    {
        return new[]
        {
            layout.RowBounds,
            layout.SelectionBounds,
            layout.ExpanderBounds,
            layout.StateImageBounds,
            layout.NodeImageBounds,
            layout.TextBounds,
            layout.FocusBounds,
        };
    }

    private static void AssertContained(Rectangle container, Rectangle candidate)
    {
        if (candidate.IsEmpty)
        {
            return;
        }

        Assert.That(container.Contains(candidate), Is.True, $"{candidate} must be contained in {container}.");
    }
}
