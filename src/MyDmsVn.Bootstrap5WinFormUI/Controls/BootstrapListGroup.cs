using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Arranges a short collection of composed list-group items.</summary>
[DefaultEvent(nameof(ItemClick))]
public class BootstrapListGroup : Panel
{
    private Orientation _orientation = Orientation.Vertical;
    private bool _flush;
    private int _borderRadius = -1;

    /// <summary>Initializes a vertical, auto-sized list group.</summary>
    public BootstrapListGroup()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.List;
        AccessibleDescription = "Bootstrap-inspired list group.";
        Size = new Size(320, 48);
    }

    /// <summary>Gets or sets the direction in which items are arranged.</summary>
    [Category("Layout")]
    [DefaultValue(Orientation.Vertical)]
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (value != Orientation.Vertical && value != Orientation.Horizontal)
            {
                throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(Orientation));
            }

            _orientation = value;
        }
    }

    /// <summary>Gets or sets whether vertical items use edge-to-edge flush presentation.</summary>
    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Flush
    {
        get => _flush;
        set => _flush = value;
    }

    /// <summary>Gets or sets the outer logical corner radius, or -1 for the theme radius.</summary>
    [Category("Appearance")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or non-negative.");
            }

            _borderRadius = value;
        }
    }

    /// <summary>Occurs when an actionable child item is activated.</summary>
    public event EventHandler<BootstrapListGroupItemEventArgs>? ItemClick;

    internal void RaiseItemClick(BootstrapListGroupItem item)
    {
        ItemClick?.Invoke(this, new BootstrapListGroupItemEventArgs(item));
    }
}
