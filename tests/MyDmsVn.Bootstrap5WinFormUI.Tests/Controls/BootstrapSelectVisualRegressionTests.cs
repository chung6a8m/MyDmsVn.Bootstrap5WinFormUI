using System;
using System.Collections.Generic;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
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
}
