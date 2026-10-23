using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>A native Label with theme-owned semantic text color.</summary>
[DefaultProperty(nameof(TextRole))]
public class BootstrapLabel : Label
{
    private BootstrapTextRole _textRole;
    private bool _settingTheme;
    private bool _foreColorOverridden;
    private bool _fontOverridden;
    private bool _themeSubscribed;
    private Font? _themeFont;
    private BootstrapFontToken? _themeFontToken;

    /// <summary>Constructs a designer-safe semantic label.</summary>
    public BootstrapLabel()
    {
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyTheme();
    }

    /// <summary>Gets or explicitly overrides the semantic foreground color.</summary>
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

    /// <summary>Gets or sets the semantic foreground role.</summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapTextRole.Body)]
    public BootstrapTextRole TextRole
    {
        get => _textRole;
        set
        {
            if (!Enum.IsDefined(typeof(BootstrapTextRole), value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_textRole == value) return;
            _textRole = value;
            ApplyTheme();
        }
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
            if (!_foreColorOverridden)
                ForeColor = _textRole == BootstrapTextRole.Muted ? theme.Colors.MutedText :
                    _textRole == BootstrapTextRole.Success ? theme.Colors.Success :
                    _textRole == BootstrapTextRole.Warning ? theme.Colors.Warning :
                    _textRole == BootstrapTextRole.Danger ? theme.Colors.Danger : theme.Colors.Text;
            if (!_fontOverridden) ApplyThemeFont(theme.Typography.Label);
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
