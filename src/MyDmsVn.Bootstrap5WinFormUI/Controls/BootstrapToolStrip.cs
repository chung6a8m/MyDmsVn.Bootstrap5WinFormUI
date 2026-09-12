using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-themed presentation while retaining the native <see cref="ToolStrip"/> behavior contract.
/// </summary>
public class BootstrapToolStrip : ToolStrip
{
    private readonly BootstrapToolStripAppearanceController _appearance;

    /// <summary>Initializes a designer-safe Bootstrap tool strip.</summary>
    public BootstrapToolStrip()
    {
        _appearance = new BootstrapToolStripAppearanceController(this);
    }

    /// <summary>Gets or sets the semantic accent used for interactive item states.</summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic accent used for selected, checked, and pressed item states.")]
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
