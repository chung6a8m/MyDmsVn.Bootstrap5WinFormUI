namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Defines how a calendar accepts dates.
/// </summary>
public enum BootstrapCalendarSelectionMode
{
    /// <summary>
    /// Allows zero or one selected date.
    /// </summary>
    Single = 0,

    /// <summary>
    /// Allows an incomplete or complete inclusive date range.
    /// </summary>
    Range = 1,

    /// <summary>
    /// Allows a set of independently selected dates.
    /// </summary>
    Multiple = 2
}
