using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Theme;

/// <summary>
/// Provides the previous and current application themes for a theme change.
/// </summary>
public sealed class BootstrapThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes event data for a theme change.
    /// </summary>
    public BootstrapThemeChangedEventArgs(BootstrapTheme oldTheme, BootstrapTheme newTheme)
    {
        OldTheme = oldTheme ?? throw new ArgumentNullException(nameof(oldTheme));
        NewTheme = newTheme ?? throw new ArgumentNullException(nameof(newTheme));
    }

    /// <summary>Gets the theme that was active before the change.</summary>
    public BootstrapTheme OldTheme { get; }

    /// <summary>Gets the newly active theme.</summary>
    public BootstrapTheme NewTheme { get; }
}
