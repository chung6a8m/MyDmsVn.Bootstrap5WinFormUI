using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapComboBoxVisualRegressionTests
{
    [Test]
    public void RoundedShellClipsNativeWindowCorners()
    {
        using var form = new Form
        {
            ShowInTaskbar = false,
            ClientSize = new Size(440, 140)
        };
        using var comboBox = new BootstrapComboBox
        {
            Location = new Point(30, 30),
            Width = 360,
            BorderRadius = 8,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        comboBox.Items.AddRange(new object[] { "With leading icon", "Alternative" });
        comboBox.SelectedIndex = 0;
        form.Controls.Add(comboBox);

        form.Show();
        Application.DoEvents();

        Assert.That(comboBox.Region, Is.Not.Null,
            "A rounded native ComboBox shell needs an HWND region so the original rectangular native corners cannot show through.");
        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.Region!.IsVisible(0, 0), Is.False);
            Assert.That(comboBox.Region.IsVisible(comboBox.Width - 1, 0), Is.False);
            Assert.That(comboBox.Region.IsVisible(comboBox.Width / 2, comboBox.Height / 2), Is.True);
        }));
    }

    [Test]
    public void ZeroRadiusLeavesNativeWindowRectangular()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var comboBox = new BootstrapComboBox
        {
            BorderRadius = 0,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        form.Controls.Add(comboBox);

        form.Show();
        Application.DoEvents();

        Assert.That(comboBox.Region, Is.Null);
    }

    [Test]
    public void IntegratedDemoStatusLabelDoesNotOverlapDropDownList()
    {
        using var form = new AdvancedInputsDemoForm { ShowInTaskbar = false };
        form.Show();
        Application.DoEvents();
        form.PerformLayout();
        Application.DoEvents();

        var combo = FindDemoComboBox(form);
        var status = FindDemoStatus(form);

        Assert.That(
            status.Top,
            Is.GreaterThanOrEqualTo(combo.Bottom + status.Margin.Top),
            $"Status begins at y={status.Top}, but the ComboBox ends at y={combo.Bottom} and requires {status.Margin.Top}px top margin.");
    }

    private static BootstrapComboBox FindDemoComboBox(Control root)
    {
        return Descendants(root)
            .OfType<BootstrapComboBox>()
            .Single(control => control.AccessibleName == "DropDownList / native selection combo box");
    }

    private static Label FindDemoStatus(Control root)
    {
        return Descendants(root)
            .OfType<Label>()
            .Single(control => control.Text.StartsWith("SelectedIndexChanged:", StringComparison.Ordinal));
    }

    private static IEnumerable<Control> Descendants(Control root)
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
