using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-themed presentation while retaining native <see cref="StatusStrip"/> layout, Spring sizing, and sizing-grip behavior.
/// </summary>
public class BootstrapStatusStrip : StatusStrip
{
    private readonly BootstrapToolStripAppearanceController _appearance;

    /// <summary>Initializes a designer-safe Bootstrap status strip.</summary>
    public BootstrapStatusStrip()
    {
        _appearance = new BootstrapToolStripAppearanceController(this);
    }

    /// <summary>Gets or sets the semantic accent used for interactive status-item states.</summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic accent used for selected, checked, and pressed status-item states.")]
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
