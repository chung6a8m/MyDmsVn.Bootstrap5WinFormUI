using System;
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
    public void HandleTimeItemHeightChangeRelayoutsFollowingStatusLabel()
    {
        using var form = new Form
        {
            ShowInTaskbar = false,
            ClientSize = new Size(500, 180)
        };
        using var stack = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Location = new Point(12, 12)
        };
        using var comboBox = new BootstrapComboBox
        {
            Width = 360,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = Padding.Empty
        };
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta" });
        comboBox.SelectedIndex = 0;
        using var status = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 0),
            Text = "SelectedIndexChanged: 0 / Alpha"
        };

        stack.Controls.Add(comboBox);
        stack.Controls.Add(status);
        form.Controls.Add(stack);

        form.Show();
        Application.DoEvents();
        form.PerformLayout();
        Application.DoEvents();

        Assert.That(status.Top, Is.GreaterThanOrEqualTo(comboBox.Bottom + status.Margin.Top),
            "Changing native ItemHeight during handle creation must cause the parent layout to reposition controls that follow the ComboBox.");
    }

    [Test]
    public void RoundedShellDoesNotExposeNativeRectangularCornerPixels()
    {
        using var form = new Form
        {
            ShowInTaskbar = false,
            BackColor = Color.Magenta,
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
        form.PerformLayout();
        Application.DoEvents();

        using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, form.ClientRectangle);

        var topLeft = bitmap.GetPixel(comboBox.Left, comboBox.Top);
        var topRight = bitmap.GetPixel(comboBox.Right - 1, comboBox.Top);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(topLeft.ToArgb(), Is.EqualTo(form.BackColor.ToArgb()),
                "The rounded top-left corner should reveal the parent background, not the native rectangular ComboBox border.");
            Assert.That(topRight.ToArgb(), Is.EqualTo(form.BackColor.ToArgb()),
                "The rounded top-right corner should reveal the parent background, not the native rectangular ComboBox border.");
        }));
    }

    [Test]
    public void IntegratedDemoStatusLabelDoesNotOverlapDropDownList()
    {
        using var form = new AdvancedInputsDemoForm { ShowInTaskbar = false };
        form.Show();
        Application.DoEvents();
        form.PerformLayout();
        Application.DoEvents();

        var combo = Descendants(form)
            .OfType<BootstrapComboBox>()
            .Single(control => control.AccessibleName == "DropDownList / native selection combo box");
        var status = Descendants(form)
            .OfType<Label>()
            .Single(control => control.Text.StartsWith("SelectedIndexChanged:", StringComparison.Ordinal));

        Assert.That(status.Top, Is.GreaterThanOrEqualTo(combo.Bottom + status.Margin.Top));
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
