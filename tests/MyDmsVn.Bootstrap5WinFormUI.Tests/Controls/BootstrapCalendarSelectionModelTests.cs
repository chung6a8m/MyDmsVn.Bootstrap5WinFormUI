using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapCalendarSelectionModelTests
{
    [Test]
    public void ConstructionDefaultsToEmptySingleSelectionAndNormalizesBounds()
    {
        var minDate = new DateTime(2026, 1, 2, 13, 15, 0);
        var maxDate = new DateTime(2026, 12, 30, 23, 59, 59);

        var model = new BootstrapCalendarSelectionModel(minDate, maxDate);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(model.Mode, Is.EqualTo(BootstrapCalendarSelectionMode.Single));
            Assert.That(model.MinDate, Is.EqualTo(minDate.Date));
            Assert.That(model.MaxDate, Is.EqualTo(maxDate.Date));
            Assert.That(model.SelectedDate, Is.Null);
            Assert.That(model.RangeStart, Is.Null);
            Assert.That(model.RangeEnd, Is.Null);
            Assert.That(model.SelectedDates, Is.Empty);
        }));
    }

    [Test]
    public void SupportedDateConstantsMatchDateTimePickerDomain()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapCalendarSelectionModel.MinimumSupportedDate, Is.EqualTo(DateTimePicker.MinimumDateTime.Date));
            Assert.That(BootstrapCalendarSelectionModel.MaximumSupportedDate, Is.EqualTo(DateTimePicker.MaximumDateTime.Date));
        }));
    }

    [Test]
    public void ConstructionRejectsInvalidBoundsAndUndefinedMode()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new BootstrapCalendarSelectionModel(
            new DateTime(2026, 2, 1),
            new DateTime(2026, 1, 31))));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new BootstrapCalendarSelectionModel(
            BootstrapCalendarSelectionModel.MinimumSupportedDate.AddDays(-1),
            BootstrapCalendarSelectionModel.MinimumSupportedDate)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new BootstrapCalendarSelectionModel(
            BootstrapCalendarSelectionModel.MaximumSupportedDate,
            BootstrapCalendarSelectionModel.MaximumSupportedDate.AddDays(1))));

        var model = new BootstrapCalendarSelectionModel(
            BootstrapCalendarSelectionModel.MinimumSupportedDate,
            BootstrapCalendarSelectionModel.MaximumSupportedDate);

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => model.SetMode((BootstrapCalendarSelectionMode)999)));
    }

    [Test]
    public void SingleActivationConfirmsSameDateAndReplacesDifferentDate()
    {
        var model = CreateModel();
        var first = model.Activate(new DateTime(2026, 4, 10, 13, 0, 0));
        var confirmation = model.Activate(new DateTime(2026, 4, 10, 23, 0, 0));
        var replacement = model.Activate(new DateTime(2026, 4, 12));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Changed, Is.True);
            Assert.That(first.Completed, Is.True);
            Assert.That(confirmation.Changed, Is.False);
            Assert.That(confirmation.Completed, Is.True);
            Assert.That(replacement.Changed, Is.True);
            Assert.That(replacement.Completed, Is.True);
            Assert.That(model.SelectedDate, Is.EqualTo(new DateTime(2026, 4, 12)));
        }));
    }

    [Test]
    public void SingleSelectionCanBeClearedAndRejectsOutOfRangeInputAtomically()
    {
        var model = CreateModel();
        model.SetSelectedDate(new DateTime(2026, 4, 10, 12, 0, 0));

        Assert.That(model.SetSelectedDate(null), Is.True);
        Assert.That(model.SelectedDate, Is.Null);
        Assert.That(model.SetSelectedDate(null), Is.False);

        model.SetSelectedDate(new DateTime(2026, 4, 10));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => model.SetSelectedDate(new DateTime(2025, 12, 31))));
        Assert.That(model.SelectedDate, Is.EqualTo(new DateTime(2026, 4, 10)));
    }

    [Test]
    public void RangeActivationSortsEndpointsAndRestartsAfterCompletion()
    {
        var model = CreateRangeModel();
        var first = model.Activate(new DateTime(2026, 5, 10, 12, 0, 0));
        var second = model.Activate(new DateTime(2026, 5, 3));
        var third = model.Activate(new DateTime(2026, 5, 18));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Changed, Is.True);
            Assert.That(first.Completed, Is.False);
            Assert.That(second.Changed, Is.True);
            Assert.That(second.Completed, Is.True);
            Assert.That(third.Changed, Is.True);
            Assert.That(third.Completed, Is.False);
            Assert.That(model.RangeStart, Is.EqualTo(new DateTime(2026, 5, 18)));
            Assert.That(model.RangeEnd, Is.Null);
        }));
    }

    [Test]
    public void RangeSetAndClearFollowIncompleteAndAtomicValidationRules()
    {
        var model = CreateRangeModel();

        Assert.That(model.SetRange(new DateTime(2026, 6, 10, 10, 0, 0), null), Is.True);
        Assert.That(model.RangeStart, Is.EqualTo(new DateTime(2026, 6, 10)));
        Assert.That(model.RangeEnd, Is.Null);
        Assert.That(model.SetRange(null, null), Is.True);
        Assert.That(model.Clear(), Is.False);

        model.SetRange(new DateTime(2026, 6, 10), new DateTime(2026, 6, 20));
        Assert.Throws<ArgumentException>((Action)(() => model.SetRange(null, new DateTime(2026, 6, 20))));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => model.SetRange(new DateTime(2025, 12, 31), null)));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(model.RangeStart, Is.EqualTo(new DateTime(2026, 6, 10)));
            Assert.That(model.RangeEnd, Is.EqualTo(new DateTime(2026, 6, 20)));
        }));
    }

    [Test]
    public void MultipleTogglesDatesAndExposesSortedImmutableReplacementSnapshot()
    {
        var model = CreateMultipleModel();
        var first = model.Activate(new DateTime(2026, 3, 10, 12, 0, 0));
        var second = model.Activate(new DateTime(2026, 3, 2));
        var removal = model.Activate(new DateTime(2026, 3, 10));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Changed, Is.True);
            Assert.That(first.Completed, Is.False);
            Assert.That(second.Completed, Is.False);
            Assert.That(removal.Completed, Is.False);
            Assert.That(model.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 3, 2) }));
        }));

        Assert.That(model.SetSelectedDates(new[]
        {
            new DateTime(2026, 7, 2, 20, 0, 0),
            new DateTime(2026, 7, 1),
            new DateTime(2026, 7, 2)
        }), Is.True);
        Assert.That(model.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 7, 1), new DateTime(2026, 7, 2) }));
        Assert.That(model.SelectedDates, Is.Not.InstanceOf<DateTime[]>());
    }

    [Test]
    public void MultipleReplacementRejectsNullAndInvalidMembersAtomically()
    {
        var model = CreateMultipleModel();
        model.SetSelectedDates(new[] { new DateTime(2026, 7, 1) });

        Assert.Throws<ArgumentNullException>((Action)(() => model.SetSelectedDates(null!)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => model.SetSelectedDates(new[]
        {
            new DateTime(2026, 7, 2),
            new DateTime(2027, 1, 1)
        })));
        Assert.That(model.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 7, 1) }));
    }

    [Test]
    public void BoundsAndModeTransitionsReconcileOnlyEffectiveSelectionChanges()
    {
        var single = CreateModel();
        single.SetSelectedDate(new DateTime(2026, 4, 10));
        Assert.That(single.SetBounds(new DateTime(2026, 5, 1), new DateTime(2026, 12, 31)), Is.True);
        Assert.That(single.SelectedDate, Is.Null);

        var range = CreateRangeModel();
        range.SetRange(new DateTime(2026, 4, 10), new DateTime(2026, 5, 10));
        Assert.That(range.SetBounds(new DateTime(2026, 5, 1), new DateTime(2026, 12, 31)), Is.True);
        Assert.That(range.RangeStart, Is.Null);
        Assert.That(range.RangeEnd, Is.Null);

        var multiple = CreateMultipleModel();
        multiple.SetSelectedDates(new[] { new DateTime(2026, 4, 10), new DateTime(2026, 5, 10) });
        Assert.That(multiple.SetBounds(new DateTime(2026, 5, 1), new DateTime(2026, 12, 31)), Is.True);
        Assert.That(multiple.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 5, 10) }));
        Assert.That(multiple.SetBounds(new DateTime(2026, 5, 1), new DateTime(2026, 12, 31)), Is.False);

        Assert.That(multiple.SetMode(BootstrapCalendarSelectionMode.Multiple), Is.False);
        Assert.That(multiple.SetMode(BootstrapCalendarSelectionMode.Single), Is.True);
        Assert.That(multiple.SelectedDates, Is.Empty);
        Assert.That(multiple.SetMode(BootstrapCalendarSelectionMode.Range), Is.False);
    }

    [Test]
    public void BoundsAcceptSupportedExtremesAndRejectInvalidAssignmentsAtomically()
    {
        var model = CreateModel();
        model.SetSelectedDate(new DateTime(2026, 4, 10));

        Assert.That(model.SetBounds(
            BootstrapCalendarSelectionModel.MinimumSupportedDate,
            BootstrapCalendarSelectionModel.MaximumSupportedDate), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => model.SetBounds(
            BootstrapCalendarSelectionModel.MinimumSupportedDate.AddDays(-1),
            BootstrapCalendarSelectionModel.MaximumSupportedDate)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(model.MinDate, Is.EqualTo(BootstrapCalendarSelectionModel.MinimumSupportedDate));
            Assert.That(model.MaxDate, Is.EqualTo(BootstrapCalendarSelectionModel.MaximumSupportedDate));
            Assert.That(model.SelectedDate, Is.EqualTo(new DateTime(2026, 4, 10)));
        }));
    }

    private static BootstrapCalendarSelectionModel CreateModel()
    {
        return new BootstrapCalendarSelectionModel(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
    }

    private static BootstrapCalendarSelectionModel CreateRangeModel()
    {
        var model = CreateModel();
        model.SetMode(BootstrapCalendarSelectionMode.Range);
        return model;
    }

    private static BootstrapCalendarSelectionModel CreateMultipleModel()
    {
        var model = CreateModel();
        model.SetMode(BootstrapCalendarSelectionMode.Multiple);
        return model;
    }
}
