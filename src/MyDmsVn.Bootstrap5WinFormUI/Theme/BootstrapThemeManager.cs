using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Theme;

/// <summary>
/// Owns the application-level current theme and publishes runtime theme changes.
/// </summary>
public static class BootstrapThemeManager
{
    private static BootstrapTheme _currentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

    /// <summary>
    /// Occurs after <see cref="CurrentTheme"/> changes to a different theme instance.
    /// Subscribers that outlive controls must unsubscribe deterministically.
    /// </summary>
    public static event EventHandler<BootstrapThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Gets or sets the application-level current theme.
    /// The default is a safe Light theme so Designer-created controls do not require startup initialization.
    /// </summary>
    public static BootstrapTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (ReferenceEquals(_currentTheme, value))
            {
                return;
            }

            var oldTheme = _currentTheme;
            _currentTheme = value;
            ThemeChanged?.Invoke(null, new BootstrapThemeChangedEventArgs(oldTheme, value));
        }
    }
}
