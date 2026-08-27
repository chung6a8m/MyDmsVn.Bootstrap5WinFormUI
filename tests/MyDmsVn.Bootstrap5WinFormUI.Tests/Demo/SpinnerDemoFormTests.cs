using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class SpinnerDemoFormTests
{
    [Test]
    public void SpinnerSectionsRemainWideEnoughToDisplayTheirRows()
    {
        using var form = new SpinnerDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var content = form.Controls
            .OfType<FlowLayoutPanel>()
            .Single(panel => panel.Dock == DockStyle.Fill);
        content.PerformLayout();

        var groups = content.Controls.OfType<GroupBox>().ToArray();

        Assert.That(groups, Has.Length.EqualTo(3));
        Assert.Multiple((Action)(() =>
        {
            foreach (var group in groups)
            {
                Assert.That(
                    group.Width,
                    Is.GreaterThanOrEqualTo(600),
                    $"{group.Text} collapsed to {group.Width}px and clips its spinner rows.");
            }
        }));
    }
}
