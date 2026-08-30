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

    [Test]
    public void AdvancedInputsDemoContainsStage9BootstrapDatePickerScenarios()
    {
        using var form = new AdvancedInputsDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var datePickers = FindControls<BootstrapDatePicker>(form).ToArray();
        var labels = FindControls<Label>(form).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(datePickers.Length, Is.GreaterThanOrEqualTo(11));
            Assert.That(datePickers.Any(picker => picker.Format == DateTimePickerFormat.Long), Is.True);
            Assert.That(datePickers.Any(picker => picker.Format == DateTimePickerFormat.Short), Is.True);
            Assert.That(datePickers.Any(picker => picker.Format == DateTimePickerFormat.Time), Is.True);
            Assert.That(datePickers.Any(picker => picker.Format == DateTimePickerFormat.Custom && picker.CustomFormat == "yyyy-MM-dd"), Is.True);
            Assert.That(datePickers.Any(picker => picker.Format == DateTimePickerFormat.Custom && picker.CustomFormat == "yyyy-MM-dd HH:mm"), Is.True);
            Assert.That(datePickers.Any(picker => picker.ShowCheckBox && !picker.Checked), Is.True);
            Assert.That(datePickers.Any(picker => picker.MinDate == new DateTime(2026, 1, 1) && picker.MaxDate == new DateTime(2026, 12, 31)), Is.True);
            Assert.That(datePickers.Any(picker => picker.ValidationState == BootstrapValidationState.Valid), Is.True);
            Assert.That(datePickers.Any(picker => picker.ValidationState == BootstrapValidationState.Invalid), Is.True);
            Assert.That(datePickers.Any(picker => !picker.Enabled), Is.True);
            Assert.That(datePickers.Any(picker => picker.BorderRadius == 8), Is.True);
            Assert.That(labels.Any(label => label.Text.StartsWith("ValueChanged:", StringComparison.Ordinal)), Is.True);
            Assert.That(labels.Any(label =>
                label.Text.IndexOf("native", StringComparison.OrdinalIgnoreCase) >= 0 &&
                label.Text.IndexOf("calendar", StringComparison.OrdinalIgnoreCase) >= 0 &&
                label.Text.IndexOf("popup", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
        }));
    }

    [Test]
    public void AdvancedInputsDemoContainsCustomCalendarAndPickerSelectionScenarios()
    {
        using var form = new AdvancedInputsDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var calendars = FindControls<BootstrapCalendar>(form).ToArray();
        var pickers = FindControls<BootstrapCalendarPicker>(form).ToArray();
        var labels = FindControls<Label>(form).ToArray();
        var captions = labels.Select(label => label.Text).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(captions.Contains("Custom Calendar — Range"), Is.True);
            Assert.That(captions.Contains("Calendar Picker — Single"), Is.True);
            Assert.That(captions.Contains("Calendar Picker — Range"), Is.True);
            Assert.That(captions.Contains("Calendar Picker — Multiple"), Is.True);
            Assert.That(calendars.Any(calendar =>
                calendar.SelectionMode == BootstrapCalendarSelectionMode.Range &&
                calendar.DisplayMonth == new DateTime(2026, 8, 1) &&
                calendar.RangeStart == new DateTime(2026, 8, 10) &&
                calendar.RangeEnd == new DateTime(2026, 8, 15)), Is.True);
            Assert.That(pickers.Any(picker =>
                picker.SelectionMode == BootstrapCalendarSelectionMode.Single &&
                picker.MinDate == new DateTime(2025, 1, 1) &&
                picker.MaxDate == new DateTime(2030, 12, 31) &&
                picker.DateFormat == "yyyy-MM-dd" &&
                picker.SelectedDate == new DateTime(2026, 8, 12)), Is.True);
            Assert.That(pickers.Any(picker =>
                picker.SelectionMode == BootstrapCalendarSelectionMode.Range &&
                picker.MinDate == new DateTime(2025, 1, 1) &&
                picker.MaxDate == new DateTime(2030, 12, 31) &&
                picker.DateFormat == "yyyy-MM-dd" &&
                picker.RangeStart == new DateTime(2026, 8, 10) &&
                picker.RangeEnd == new DateTime(2026, 8, 15) &&
                picker.ValidationState == BootstrapValidationState.Invalid), Is.True);
            Assert.That(pickers.Any(picker =>
                picker.SelectionMode == BootstrapCalendarSelectionMode.Multiple &&
                picker.MinDate == new DateTime(2025, 1, 1) &&
                picker.MaxDate == new DateTime(2030, 12, 31) &&
                picker.DateFormat == "yyyy-MM-dd" &&
                !picker.Enabled &&
                picker.SelectedDates.SequenceEqual(new[]
                {
                    new DateTime(2026, 8, 8),
                    new DateTime(2026, 8, 12),
                    new DateTime(2026, 8, 18)
                })), Is.True);
            Assert.That(labels.Any(label => label.Text.StartsWith("SelectionChanged:", StringComparison.Ordinal)), Is.True);
        }));
    }

    [Test]
    public void AdvancedInputsDemoUpdatesCalendarSelectionFeedbackForPublicSelectionChanges()
    {
        using var form = new AdvancedInputsDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var calendar = FindControls<BootstrapCalendar>(form)
            .Single(control => control.SelectionMode == BootstrapCalendarSelectionMode.Range);
        var singlePicker = FindControls<BootstrapCalendarPicker>(form)
            .Single(control => control.SelectionMode == BootstrapCalendarSelectionMode.Single);
        var rangePicker = FindControls<BootstrapCalendarPicker>(form)
            .Single(control => control.SelectionMode == BootstrapCalendarSelectionMode.Range);

        calendar.SetRange(new DateTime(2026, 9, 2), new DateTime(2026, 9, 6));
        singlePicker.SelectedDate = new DateTime(2026, 9, 12);
        rangePicker.SetRange(new DateTime(2026, 9, 10), new DateTime(2026, 9, 18));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetScenarioStatus(form, "Custom Calendar — Range").Text,
                Is.EqualTo("SelectionChanged: 2026-09-02 — 2026-09-06"));
            Assert.That(GetScenarioStatus(form, "Calendar Picker — Single").Text,
                Is.EqualTo("SelectionChanged: 2026-09-12"));
            Assert.That(GetScenarioStatus(form, "Calendar Picker — Range").Text,
                Is.EqualTo("SelectionChanged: 2026-09-10 — 2026-09-18"));
        }));
    }

    private static Label GetScenarioStatus(Control root, string caption)
    {
        var captionLabel = FindControls<Label>(root).Single(label => label.Text == caption);
        return captionLabel.Parent!.Controls.OfType<Label>().Single(label => !ReferenceEquals(label, captionLabel));
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
