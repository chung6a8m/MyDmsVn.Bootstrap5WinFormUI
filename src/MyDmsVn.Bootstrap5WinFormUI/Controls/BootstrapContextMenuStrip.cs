using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-themed presentation while retaining native <see cref="ContextMenuStrip"/> placement, source, and lifecycle behavior.
/// </summary>
public class BootstrapContextMenuStrip : ContextMenuStrip
{
    private readonly BootstrapToolStripAppearanceController _appearance;

    /// <summary>Initializes a designer-safe Bootstrap context menu.</summary>
    public BootstrapContextMenuStrip()
    {
        _appearance = new BootstrapToolStripAppearanceController(this);
    }

    /// <summary>Initializes a Bootstrap context menu owned by the specified component container.</summary>
    /// <param name="container">The component container that owns this menu.</param>
    public BootstrapContextMenuStrip(IContainer container)
        : base(container)
    {
        _appearance = new BootstrapToolStripAppearanceController(this);
    }

    /// <summary>Gets or sets the semantic accent used for interactive menu states.</summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic accent used for selected, checked, and pressed context-menu states.")]
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
