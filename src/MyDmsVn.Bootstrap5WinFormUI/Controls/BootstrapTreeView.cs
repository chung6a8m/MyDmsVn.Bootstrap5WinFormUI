using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-themed presentation while retaining the native <see cref="TreeView"/> contract.
/// </summary>
public class BootstrapTreeView : TreeView
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapTreeView"/> class.
    /// </summary>
    public BootstrapTreeView()
    {
        BorderStyle = BorderStyle.None;
        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        HideSelection = false;
    }

    /// <summary>
    /// Gets or sets the Bootstrap semantic variant used for selected-node presentation.
    /// </summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic variant used for selected-node presentation.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            Invalidate();
        }
    }
}
