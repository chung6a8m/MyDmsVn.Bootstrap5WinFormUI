using System;
using System.ComponentModel;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-aware presentation while retaining the native <see cref="TrackBar"/> contract.
/// </summary>
public class BootstrapRange : TrackBar
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _themeSubscribed;

    /// <summary>Initializes a new instance of the <see cref="BootstrapRange"/> class.</summary>
    public BootstrapRange()
    {
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemePresentation();
    }

    /// <summary>Gets or sets the semantic variant used for the range thumb.</summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic variant used for the range thumb.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!IsDisposed && !Disposing)
        {
            ApplyThemePresentation();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        if (!IsDisposed && !Disposing)
        {
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _themeSubscribed)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _themeSubscribed = false;
        }

        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        ApplyThemePresentation();
        Invalidate();
    }

    private void ApplyThemePresentation()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        BackColor = colors.Surface;
        ForeColor = colors.Text;
    }
}
