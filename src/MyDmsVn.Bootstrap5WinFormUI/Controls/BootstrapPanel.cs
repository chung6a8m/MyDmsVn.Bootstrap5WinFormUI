using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>A native Panel with a theme-owned semantic surface and public application-owned children.</summary>
[DefaultProperty(nameof(SurfaceRole))]
public class BootstrapPanel : Panel
{
    private BootstrapSurfaceRole _surfaceRole;
    private bool _settingTheme;
    private bool _backColorOverridden;
    private bool _foreColorOverridden;
    private bool _fontOverridden;
    private bool _themeSubscribed;
    private Font? _themeFont;
    private BootstrapFontToken? _themeFontToken;

    /// <summary>Constructs a designer-safe semantic panel.</summary>
    public BootstrapPanel()
    {
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyTheme();
    }

    /// <summary>Gets or explicitly overrides the semantic background color.</summary>
    public new Color BackColor
    {
        get => base.BackColor;
        set
        {
            if (!_settingTheme) _backColorOverridden = true;
            base.BackColor = value;
        }
    }

    /// <summary>Gets or explicitly overrides the themed foreground color.</summary>
    public new Color ForeColor
    {
        get => base.ForeColor;
        set
        {
            if (!_settingTheme) _foreColorOverridden = true;
            base.ForeColor = value;
        }
    }

    /// <summary>Gets or explicitly overrides the themed font. Caller fonts remain caller-owned.</summary>
    public new Font Font
    {
        get => base.Font;
        set
        {
            if (!_settingTheme)
            {
                _fontOverridden = true;
                DisposeThemeFont();
            }
            base.Font = value;
        }
    }

    /// <summary>Gets or sets the theme surface role.</summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapSurfaceRole.Body)]
    public BootstrapSurfaceRole SurfaceRole
    {
        get => _surfaceRole;
        set
        {
            if (!Enum.IsDefined(typeof(BootstrapSurfaceRole), value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_surfaceRole == value) return;
            _surfaceRole = value;
            ApplyTheme();
        }
    }

    /// <inheritdoc />
    protected override void OnBackColorChanged(EventArgs e)
    {
        if (!_settingTheme) _backColorOverridden = true;
        base.OnBackColorChanged(e);
    }

    /// <inheritdoc />
    protected override void OnForeColorChanged(EventArgs e)
    {
        if (!_settingTheme) _foreColorOverridden = true;
        base.OnForeColorChanged(e);
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        if (!_settingTheme)
        {
            _fontOverridden = true;
            DisposeThemeFont();
        }
        base.OnFontChanged(e);
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }
            DisposeThemeFont();
        }
        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (!IsDisposed) ApplyTheme();
    }

    private void ApplyTheme()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        _settingTheme = true;
        try
        {
            if (!_backColorOverridden)
                BackColor = _surfaceRole == BootstrapSurfaceRole.Surface ? theme.Colors.Surface :
                    _surfaceRole == BootstrapSurfaceRole.Alternate ? theme.Colors.SurfaceSecondary : theme.Colors.Body;
            if (!_foreColorOverridden) ForeColor = theme.Colors.Text;
            if (!_fontOverridden) ApplyThemeFont(theme.Typography.Body);
        }
        finally
        {
            _settingTheme = false;
        }
    }

    private void ApplyThemeFont(BootstrapFontToken token)
    {
        if (_themeFontToken != null && _themeFont != null &&
            string.Equals(_themeFontToken.FontFamilyName, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(_themeFontToken.SizeInPoints - token.SizeInPoints) < 0.01f && _themeFontToken.Style == token.Style) return;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var prior = _themeFont;
        _themeFont = next;
        _themeFontToken = token;
        Font = next;
        prior?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        _themeFontToken = null;
        font?.Dispose();
    }
}
