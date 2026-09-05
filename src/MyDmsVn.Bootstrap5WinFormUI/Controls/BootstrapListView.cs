using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-themed presentation while retaining the native <see cref="ListView"/> contract.
/// </summary>
public class BootstrapListView : ListView
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _striped;
    private bool _hoverHighlight = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapListView"/> class.
    /// </summary>
    public BootstrapListView()
    {
        OwnerDraw = true;
        DoubleBuffered = true;
    }

    /// <summary>
    /// Gets or sets the Bootstrap semantic variant used for selected-item presentation.
    /// </summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic variant used for selected-item presentation.")]
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

    /// <summary>
    /// Gets or sets a value indicating whether neutral rows alternate their background in row-oriented views.
    /// </summary>
    [Category("Appearance")]
    [Description("Alternates neutral row backgrounds in Details and List views.")]
    [DefaultValue(false)]
    public bool Striped
    {
        get => _striped;
        set
        {
            if (_striped == value)
            {
                return;
            }

            _striped = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the item under the pointer receives presentation-only highlighting.
    /// </summary>
    [Category("Appearance")]
    [Description("Highlights the item under the pointer without changing native selection behavior.")]
    [DefaultValue(true)]
    public bool HoverHighlight
    {
        get => _hoverHighlight;
        set
        {
            if (_hoverHighlight == value)
            {
                return;
            }

            _hoverHighlight = value;
            Invalidate();
        }
    }
}
