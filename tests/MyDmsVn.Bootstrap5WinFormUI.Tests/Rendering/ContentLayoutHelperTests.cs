using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Rendering;

[TestFixture]
public sealed class ContentLayoutHelperTests
{
    [Test]
    public void ArrangeHorizontalCentersLeadingAndTrailingContentAsOneGroup()
    {
        var layout = ContentLayoutHelper.ArrangeHorizontal(
            new Rectangle(0, 0, 200, 40),
            new Padding(10),
            new Size(16, 16),
            new Size(60, 20),
            8,
            ContentAlignment.MiddleCenter);

        Assert.That(layout.LeadingBounds, Is.EqualTo(new Rectangle(58, 12, 16, 16)));
        Assert.That(layout.TrailingBounds, Is.EqualTo(new Rectangle(82, 10, 60, 20)));
        Assert.That(layout.ContentBounds, Is.EqualTo(new Rectangle(58, 10, 84, 20)));
    }

    [Test]
    public void ArrangeHorizontalIgnoresSpacingWhenSecondItemIsEmpty()
    {
        var layout = ContentLayoutHelper.ArrangeHorizontal(
            new Rectangle(0, 0, 200, 40),
            new Padding(10),
            new Size(16, 16),
            Size.Empty,
            8,
            ContentAlignment.MiddleCenter);

        Assert.That(layout.LeadingBounds, Is.EqualTo(new Rectangle(92, 12, 16, 16)));
        Assert.That(layout.TrailingBounds, Is.EqualTo(Rectangle.Empty));
        Assert.That(layout.ContentBounds, Is.EqualTo(layout.LeadingBounds));
    }

    [Test]
    public void ArrangeHorizontalHonorsTopLeftAlignment()
    {
        var layout = ContentLayoutHelper.ArrangeHorizontal(
            new Rectangle(0, 0, 100, 40),
            new Padding(5),
            new Size(10, 8),
            new Size(20, 12),
            4,
            ContentAlignment.TopLeft);

        Assert.That(layout.LeadingBounds, Is.EqualTo(new Rectangle(5, 5, 10, 8)));
        Assert.That(layout.TrailingBounds, Is.EqualTo(new Rectangle(19, 5, 20, 12)));
    }

    [Test]
    public void ArrangeHorizontalRejectsNegativeSpacing()
    {
        Action action = () => ContentLayoutHelper.ArrangeHorizontal(
            new Rectangle(0, 0, 100, 40),
            Padding.Empty,
            new Size(10, 10),
            new Size(10, 10),
            -1,
            ContentAlignment.MiddleCenter);

        Assert.That(action, Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
