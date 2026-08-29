namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Identifies whether a dropdown model represents a command, separator, or hosted control.
/// </summary>
public enum BootstrapDropdownItemKind
{
    /// <summary>
    /// Represents a normal command row.
    /// </summary>
    Item = 0,

    /// <summary>
    /// Represents a non-activatable separator row.
    /// </summary>
    Separator = 1,

    /// <summary>
    /// Represents a row whose control is created for the current native menu snapshot.
    /// </summary>
    HostedControl = 2
}
