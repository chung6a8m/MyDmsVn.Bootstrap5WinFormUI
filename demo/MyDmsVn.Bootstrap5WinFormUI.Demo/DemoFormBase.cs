using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>
/// Provides the shared DPI and native body-font policy for Integrated Demo forms.
/// </summary>
public abstract class DemoFormBase : Form
{
    private Font? _demoBodyFont;

    /// <summary>
    /// Initializes an Integrated Demo form with DPI autoscaling and browser-equivalent body typography.
    /// </summary>
    protected DemoFormBase()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        ApplyBodyTypography(BootstrapThemeManager.CurrentTheme);
        BootstrapThemeManager.ThemeChanged += OnDemoThemeChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnDemoThemeChanged;
        }

        base.Dispose(disposing);

        if (disposing)
        {
            _demoBodyFont?.Dispose();
            _demoBodyFont = null;
        }
    }

    private void OnDemoThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyBodyTypography(e.NewTheme);
    }

    private void ApplyBodyTypography(BootstrapTheme theme)
    {
        var token = theme.Typography.Body;
        if (_demoBodyFont is not null && DemoTypography.FontMatchesToken(_demoBodyFont, token))
        {
            return;
        }

        var replacement = DemoTypography.CreateFont(token);
        var previous = _demoBodyFont;
        _demoBodyFont = replacement;
        Font = replacement;
        previous?.Dispose();
    }
}
