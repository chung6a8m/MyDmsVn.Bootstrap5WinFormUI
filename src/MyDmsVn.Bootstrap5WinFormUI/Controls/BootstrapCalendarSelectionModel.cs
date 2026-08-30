using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapCalendarSelectionChange
{
    public BootstrapCalendarSelectionChange(bool changed, bool completed)
    {
        Changed = changed;
        Completed = completed;
    }

    public bool Changed { get; }

    public bool Completed { get; }
}

internal sealed class BootstrapCalendarSelectionModel
{
    internal static readonly DateTime MinimumSupportedDate = DateTimePicker.MinimumDateTime.Date;
    internal static readonly DateTime MaximumSupportedDate = DateTimePicker.MaximumDateTime.Date;

    private static readonly IReadOnlyList<DateTime> EmptyDates = Array.AsReadOnly(new DateTime[0]);
    private SortedSet<DateTime> selectedDates = new SortedSet<DateTime>();

    public BootstrapCalendarSelectionModel(DateTime minDate, DateTime maxDate)
    {
        ValidateBounds(minDate, maxDate, out var normalizedMinDate, out var normalizedMaxDate);
        MinDate = normalizedMinDate;
        MaxDate = normalizedMaxDate;
        Mode = BootstrapCalendarSelectionMode.Single;
        SelectedDates = EmptyDates;
    }

    public BootstrapCalendarSelectionMode Mode { get; private set; }

    public DateTime MinDate { get; private set; }

    public DateTime MaxDate { get; private set; }

    public DateTime? SelectedDate { get; private set; }

    public DateTime? RangeStart { get; private set; }

    public DateTime? RangeEnd { get; private set; }

    public IReadOnlyList<DateTime> SelectedDates { get; private set; }

    public bool SetMode(BootstrapCalendarSelectionMode mode)
    {
        ValidateMode(mode);
        if (Mode == mode)
        {
            return false;
        }

        var changed = HasSelection();
        Mode = mode;
        ClearSelection();
        return changed;
    }

    public bool SetBounds(DateTime minDate, DateTime maxDate)
    {
        ValidateBounds(minDate, maxDate, out var normalizedMinDate, out var normalizedMaxDate);
        var changed = false;

        if (Mode == BootstrapCalendarSelectionMode.Single &&
            SelectedDate.HasValue && !IsWithinBounds(SelectedDate.Value, normalizedMinDate, normalizedMaxDate))
        {
            SelectedDate = null;
            changed = true;
        }
        else if (Mode == BootstrapCalendarSelectionMode.Range &&
            ((RangeStart.HasValue && !IsWithinBounds(RangeStart.Value, normalizedMinDate, normalizedMaxDate)) ||
             (RangeEnd.HasValue && !IsWithinBounds(RangeEnd.Value, normalizedMinDate, normalizedMaxDate))))
        {
            RangeStart = null;
            RangeEnd = null;
            changed = true;
        }
        else if (Mode == BootstrapCalendarSelectionMode.Multiple)
        {
            var reconciledDates = new SortedSet<DateTime>(selectedDates);
            reconciledDates.RemoveWhere(date => !IsWithinBounds(date, normalizedMinDate, normalizedMaxDate));
            if (!SetsEqual(selectedDates, reconciledDates))
            {
                SetMultipleDates(reconciledDates);
                changed = true;
            }
        }

        MinDate = normalizedMinDate;
        MaxDate = normalizedMaxDate;
        return changed;
    }

    public bool SetSelectedDate(DateTime? date)
    {
        EnsureMode(BootstrapCalendarSelectionMode.Single);
        var normalizedDate = NormalizeAndValidateSelection(date);
        if (SelectedDate == normalizedDate)
        {
            return false;
        }

        SelectedDate = normalizedDate;
        return true;
    }

    public bool SetRange(DateTime? start, DateTime? end)
    {
        EnsureMode(BootstrapCalendarSelectionMode.Range);
        if (!start.HasValue && end.HasValue)
        {
            throw new ArgumentException("An end date requires a start date.", nameof(start));
        }

        var normalizedStart = NormalizeAndValidateSelection(start);
        var normalizedEnd = NormalizeAndValidateSelection(end);
        if (normalizedStart.HasValue && normalizedEnd.HasValue && normalizedStart.Value > normalizedEnd.Value)
        {
            var temporary = normalizedStart;
            normalizedStart = normalizedEnd;
            normalizedEnd = temporary;
        }

        if (RangeStart == normalizedStart && RangeEnd == normalizedEnd)
        {
            return false;
        }

        RangeStart = normalizedStart;
        RangeEnd = normalizedEnd;
        return true;
    }

    public bool SetSelectedDates(IEnumerable<DateTime> dates)
    {
        EnsureMode(BootstrapCalendarSelectionMode.Multiple);
        if (dates == null)
        {
            throw new ArgumentNullException(nameof(dates));
        }

        var normalizedDates = new SortedSet<DateTime>();
        foreach (var date in dates)
        {
            normalizedDates.Add(NormalizeAndValidateDate(date));
        }

        if (SetsEqual(selectedDates, normalizedDates))
        {
            return false;
        }

        SetMultipleDates(normalizedDates);
        return true;
    }

    public bool Clear()
    {
        if (!HasSelection())
        {
            return false;
        }

        ClearSelection();
        return true;
    }

    public BootstrapCalendarSelectionChange Activate(DateTime date)
    {
        var normalizedDate = NormalizeAndValidateDate(date);
        if (Mode == BootstrapCalendarSelectionMode.Single)
        {
            var changed = SelectedDate != normalizedDate;
            if (changed)
            {
                SelectedDate = normalizedDate;
            }

            return new BootstrapCalendarSelectionChange(changed, completed: true);
        }

        if (Mode == BootstrapCalendarSelectionMode.Range)
        {
            if (!RangeStart.HasValue || RangeEnd.HasValue)
            {
                var changed = RangeStart != normalizedDate || RangeEnd.HasValue;
                RangeStart = normalizedDate;
                RangeEnd = null;
                return new BootstrapCalendarSelectionChange(changed, completed: false);
            }

            var start = RangeStart.Value;
            RangeStart = start <= normalizedDate ? start : normalizedDate;
            RangeEnd = start <= normalizedDate ? normalizedDate : start;
            return new BootstrapCalendarSelectionChange(changed: true, completed: true);
        }

        var multipleDates = new SortedSet<DateTime>(selectedDates);
        if (!multipleDates.Add(normalizedDate))
        {
            multipleDates.Remove(normalizedDate);
        }

        SetMultipleDates(multipleDates);
        return new BootstrapCalendarSelectionChange(changed: true, completed: false);
    }

    private static void ValidateBounds(DateTime minDate, DateTime maxDate, out DateTime normalizedMinDate, out DateTime normalizedMaxDate)
    {
        normalizedMinDate = minDate.Date;
        normalizedMaxDate = maxDate.Date;

        if (normalizedMinDate < MinimumSupportedDate)
        {
            throw new ArgumentOutOfRangeException(nameof(minDate));
        }

        if (normalizedMaxDate > MaximumSupportedDate)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDate));
        }

        if (normalizedMinDate > normalizedMaxDate)
        {
            throw new ArgumentOutOfRangeException(nameof(minDate));
        }
    }

    private static void ValidateMode(BootstrapCalendarSelectionMode mode)
    {
        if (mode != BootstrapCalendarSelectionMode.Single &&
            mode != BootstrapCalendarSelectionMode.Range &&
            mode != BootstrapCalendarSelectionMode.Multiple)
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private DateTime? NormalizeAndValidateSelection(DateTime? date)
    {
        if (!date.HasValue)
        {
            return null;
        }

        return NormalizeAndValidateDate(date.Value);
    }

    private DateTime NormalizeAndValidateDate(DateTime date)
    {
        var normalizedDate = date.Date;
        if (normalizedDate < MinDate || normalizedDate > MaxDate)
        {
            throw new ArgumentOutOfRangeException(nameof(date));
        }

        return normalizedDate;
    }

    private void EnsureMode(BootstrapCalendarSelectionMode expectedMode)
    {
        if (Mode != expectedMode)
        {
            throw new InvalidOperationException();
        }
    }

    private static bool IsWithinBounds(DateTime date, DateTime minDate, DateTime maxDate)
    {
        return date >= minDate && date <= maxDate;
    }

    private bool HasSelection()
    {
        return SelectedDate.HasValue || RangeStart.HasValue || RangeEnd.HasValue || selectedDates.Count != 0;
    }

    private void ClearSelection()
    {
        SelectedDate = null;
        RangeStart = null;
        RangeEnd = null;
        SetMultipleDates(new SortedSet<DateTime>());
    }

    private void SetMultipleDates(SortedSet<DateTime> dates)
    {
        selectedDates = dates;
        SelectedDates = dates.Count == 0
            ? EmptyDates
            : Array.AsReadOnly(new List<DateTime>(dates).ToArray());
    }

    private static bool SetsEqual(SortedSet<DateTime> left, SortedSet<DateTime> right)
    {
        return left.SetEquals(right);
    }
}
