using System;
using System.Globalization;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapCalendarDateFormatter
{
    internal static string Format(DateTime value, string format, CultureInfo culture)
    {
        if (format == null) throw new ArgumentNullException(nameof(format));
        if (culture == null) throw new ArgumentNullException(nameof(culture));
        return value.ToString(format, ResolveCulture(value, culture));
    }

    internal static void ValidateFormat(string format, CultureInfo culture)
    {
        Format(BootstrapCalendarSelectionModel.MinimumSupportedDate, format, culture);
    }

    private static CultureInfo ResolveCulture(DateTime value, CultureInfo culture)
    {
        if (CanRepresent(culture.DateTimeFormat.Calendar, value)) return culture;

        var fallbackCalendar = FindRepresentableGregorianCalendar(culture.OptionalCalendars, value) ??
            FindRepresentableCalendar(culture.OptionalCalendars, value);
        if (fallbackCalendar == null) return CultureInfo.InvariantCulture;

        var clone = (CultureInfo)culture.Clone();
        clone.DateTimeFormat.Calendar = fallbackCalendar;
        return clone;
    }

    private static Calendar? FindRepresentableGregorianCalendar(Calendar[] calendars, DateTime value)
    {
        foreach (var calendar in calendars)
        {
            if (calendar is GregorianCalendar && CanRepresent(calendar, value)) return calendar;
        }

        return null;
    }

    private static Calendar? FindRepresentableCalendar(Calendar[] calendars, DateTime value)
    {
        foreach (var calendar in calendars)
        {
            if (CanRepresent(calendar, value)) return calendar;
        }

        return null;
    }

    private static bool CanRepresent(Calendar calendar, DateTime value)
    {
        return calendar.MinSupportedDateTime <= value && value <= calendar.MaxSupportedDateTime;
    }
}
