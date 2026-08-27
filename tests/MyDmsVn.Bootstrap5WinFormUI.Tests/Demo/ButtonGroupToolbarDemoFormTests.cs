using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class ButtonGroupToolbarDemoFormTests
{
    [Test]
    public void Phase7DemoExposesGroupsAndBothToolbarOrientations()
    {
        using var form = new ButtonGroupToolbarDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var content = form.Controls
            .OfType<FlowLayoutPanel>()
            .Single(panel => panel.Dock == DockStyle.Fill);
        content.PerformLayout();

        var sections = content.Controls.OfType<GroupBox>().ToArray();
        var groups = FindControls<BootstrapButtonGroup>(content).ToArray();
        var toolbars = FindControls<BootstrapButtonToolbar>(content).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(sections, Has.Length.EqualTo(5));
            Assert.That(sections.All(section => section.Width >= 850), Is.True);
            Assert.That(groups.Length, Is.GreaterThanOrEqualTo(7));
            Assert.That(toolbars.Any(toolbar =>
                toolbar.Orientation == Orientation.Horizontal &&
                toolbar.Alignment == BootstrapToolbarAlignment.SpaceBetween), Is.True);
            Assert.That(toolbars.Any(toolbar => toolbar.Orientation == Orientation.Vertical), Is.True);
        }));
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }
}
