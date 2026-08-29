using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectLayoutTests
{
    [Test]
    public void SingleLayoutReservesDistinctArrowAndClearHitTargets()
    {
        var layout = BootstrapSelectSelectionLayout.Create(
            new Size(240, 32),
            BootstrapSelectMode.Single,
            new[] { new BootstrapSelectItem(1, "Alpha") },
            allowClear: true,
            rightToLeft: false,
            dpi: 96,
            maximumRows: 3);

        Assert.Multiple((System.Action)(() =>
        {
            Assert.That(layout.ArrowBounds.Width, Is.GreaterThanOrEqualTo(20));
            Assert.That(layout.ClearBounds.Width, Is.GreaterThanOrEqualTo(20));
            Assert.That(layout.ArrowBounds.IntersectsWith(layout.ClearBounds), Is.False);
            Assert.That(layout.ContentBounds.Right, Is.LessThanOrEqualTo(layout.ClearBounds.Left));
        }));
    }

    [Test]
    public void MultipleLayoutWrapsAndStopsAtConfiguredRowLimit()
    {
        var items = new[]
        {
            new BootstrapSelectItem(1, "Alpha Customer"),
            new BootstrapSelectItem(2, "Beta Customer"),
            new BootstrapSelectItem(3, "Gamma Customer"),
            new BootstrapSelectItem(4, "Delta Customer"),
            new BootstrapSelectItem(5, "Epsilon Customer")
        };

        var layout = BootstrapSelectSelectionLayout.Create(
            new Size(180, 120),
            BootstrapSelectMode.Multiple,
            items,
            allowClear: true,
            rightToLeft: false,
            dpi: 96,
            maximumRows: 2);

        Assert.That(layout.RowCount, Is.EqualTo(2));
        Assert.That(layout.Chips.Count, Is.GreaterThan(0));
        Assert.That(layout.HasOverflow, Is.True);
        Assert.That(layout.PreferredHeight, Is.LessThanOrEqualTo(64));
    }

    [Test]
    public void LongChipIsClampedToAvailableContentWidth()
    {
        var layout = BootstrapSelectSelectionLayout.Create(
            new Size(180, 64),
            BootstrapSelectMode.Multiple,
            new[] { new BootstrapSelectItem(1, new string('X', 200)) },
            allowClear: false,
            rightToLeft: false,
            dpi: 96,
            maximumRows: 3);

        Assert.That(layout.Chips[0].Bounds.Width, Is.LessThanOrEqualTo(layout.ContentBounds.Width));
    }

    [Test]
    public void RtlMirrorsActionTargets()
    {
        var ltr = BootstrapSelectSelectionLayout.Create(new Size(240, 32), BootstrapSelectMode.Single, System.Array.Empty<BootstrapSelectItem>(), true, false, 96, 3);
        var rtl = BootstrapSelectSelectionLayout.Create(new Size(240, 32), BootstrapSelectMode.Single, System.Array.Empty<BootstrapSelectItem>(), true, true, 96, 3);

        Assert.That(ltr.ArrowBounds.Left, Is.GreaterThan(ltr.ContentBounds.Left));
        Assert.That(rtl.ArrowBounds.Right, Is.LessThanOrEqualTo(rtl.ContentBounds.Left));
    }
}
