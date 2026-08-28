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
public sealed class AdvancedInputsDemoFormTests
{
    [Test]
    public void AdvancedInputsDemoContainsStage5NumericScenarios()
    {
        using var form = new AdvancedInputsDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var numericBoxes = FindControls<BootstrapNumericBox>(form).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(numericBoxes.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(numericBoxes.Any(box => box.Value == 12m && box.Minimum == 0m && box.Maximum == 100m && box.Increment == 1m), Is.True);
            Assert.That(numericBoxes.Any(box => box.DecimalPlaces == 2 && box.Increment == 0.25m && box.Value == 12.50m), Is.True);
            Assert.That(numericBoxes.Any(box => box.ThousandsSeparator && box.Value == 123456m), Is.True);
            Assert.That(numericBoxes.Any(box => box.Minimum == -100m && box.Maximum == 100m && box.Increment == 10m), Is.True);
            Assert.That(numericBoxes.Any(box => box.ValidationState == BootstrapValidationState.Valid), Is.True);
            Assert.That(numericBoxes.Any(box => box.ValidationState == BootstrapValidationState.Invalid), Is.True);
            Assert.That(numericBoxes.Any(box => box.ReadOnly && box.Enabled), Is.True);
            Assert.That(numericBoxes.Any(box => !box.Enabled), Is.True);
        }));
    }

    [Test]
    public void AdvancedInputsDemoContainsStage6ComboBoxScenarios()
    {
        using var form = new AdvancedInputsDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var comboBoxes = FindControls<BootstrapComboBox>(form).ToArray();
        var labels = FindControls<Label>(form).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBoxes.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(comboBoxes.Any(box => box.DropDownStyle == ComboBoxStyle.DropDownList && box.Items.Count >= 3), Is.True);
            Assert.That(comboBoxes.Any(box =>
                box.DropDownStyle == ComboBoxStyle.DropDown &&
                box.AutoCompleteMode == AutoCompleteMode.SuggestAppend &&
                box.AutoCompleteSource == AutoCompleteSource.ListItems), Is.True);
            Assert.That(comboBoxes.Any(box =>
                box.DataSource is not null &&
                !string.IsNullOrEmpty(box.DisplayMember) &&
                !string.IsNullOrEmpty(box.ValueMember)), Is.True);
            Assert.That(comboBoxes.Any(box => box.LeadingIcon is not null), Is.True);
            Assert.That(comboBoxes.Any(box => box.LeadingIcon is null), Is.True);
            Assert.That(comboBoxes.Any(box => box.Items.Cast<object>().Any(item => (box.GetItemText(item) ?? string.Empty).Length >= 50)), Is.True);
            Assert.That(comboBoxes.Any(box => box.ValidationState == BootstrapValidationState.Valid), Is.True);
            Assert.That(comboBoxes.Any(box => box.ValidationState == BootstrapValidationState.Invalid), Is.True);
            Assert.That(comboBoxes.Any(box => !box.Enabled), Is.True);
            Assert.That(comboBoxes.Any(box => box.BorderRadius == 8), Is.True);
            Assert.That(labels.Any(label =>
                label.Text.IndexOf("WinForms", StringComparison.OrdinalIgnoreCase) >= 0 &&
                label.Text.IndexOf("popup", StringComparison.OrdinalIgnoreCase) >= 0 &&
                label.Text.IndexOf("OS", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
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
