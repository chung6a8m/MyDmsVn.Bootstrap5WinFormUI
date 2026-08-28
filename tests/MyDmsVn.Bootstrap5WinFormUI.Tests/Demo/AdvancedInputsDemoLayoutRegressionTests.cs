using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class AdvancedInputsDemoLayoutRegressionTests
{
    [Test]
    public void ComboBoxOwnershipNoteIsLaidOutBelowScenarioGrid()
    {
        using var form = new AdvancedInputsDemoForm
        {
            ShowInTaskbar = false
        };

        form.Show();
        Application.DoEvents();
        form.PerformLayout();
        Application.DoEvents();

        var comboSection = Descendants(form)
            .OfType<GroupBox>()
            .Single(control => string.Equals(control.Text, "ComboBox scenarios", StringComparison.Ordinal));
        var grid = Descendants(comboSection)
            .OfType<TableLayoutPanel>()
            .Single(control => control.ColumnCount == 2);
        var note = Descendants(comboSection)
            .OfType<Label>()
            .Single(control => control.Text.StartsWith("Native ownership note:", StringComparison.Ordinal));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.Parent, Is.SameAs(note.Parent),
                "The note and scenario grid should participate in the same deterministic stack layout.");
            Assert.That(grid.Bounds.IntersectsWith(note.Bounds), Is.False,
                "The explanatory note must not overlay the scenario grid.");
            Assert.That(note.Top, Is.GreaterThanOrEqualTo(grid.Bottom),
                "The explanatory note should be placed after the scenario grid in visual order.");
        }));
    }

    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;

            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
