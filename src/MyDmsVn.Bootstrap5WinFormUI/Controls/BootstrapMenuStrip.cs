using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-themed presentation while retaining native <see cref="MenuStrip"/> activation, shortcut, merge, and accessibility behavior.
/// </summary>
public class BootstrapMenuStrip : MenuStrip
{
    private readonly BootstrapToolStripAppearanceController _appearance;

    /// <summary>Initializes a designer-safe Bootstrap menu strip.</summary>
    public BootstrapMenuStrip()
    {
        _appearance = new BootstrapToolStripAppearanceController(this);
    }

    /// <summary>Gets or sets the semantic accent used for interactive menu states.</summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic accent used for selected, checked, and pressed menu states.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _appearance.Variant;
        set => _appearance.Variant = value;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _appearance?.Dispose();
        base.Dispose(disposing);
    }
}
