using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Arranges a short collection of composed list-group items.</summary>
[DefaultEvent(nameof(ItemClick))]
public class BootstrapListGroup : Panel
{
    private readonly HashSet<BootstrapListGroupItem> _subscribedItems = new HashSet<BootstrapListGroupItem>();
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

    /// <summary>Gets a read-only snapshot of direct items in current WinForms child-index order.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<BootstrapListGroupItem> Items => GetItemsSnapshot();

    /// <summary>Creates, adds, and returns an item containing the supplied text.</summary>
    public BootstrapListGroupItem AddItem(string text)
    {
        var item = new BootstrapListGroupItem { Text = text ?? string.Empty };
        Controls.Add(item);
        return item;
    }

    /// <summary>Adds an existing item through normal WinForms child ownership.</summary>
    public void AddItem(BootstrapListGroupItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (!ReferenceEquals(item.Parent, this))
        {
            Controls.Add(item);
        }
    }

    /// <summary>Removes an item without disposing it.</summary>
    public bool RemoveItem(BootstrapListGroupItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (!ReferenceEquals(item.Parent, this))
        {
            return false;
        }

        Controls.Remove(item);
        return true;
    }

    /// <summary>Removes all direct list-group items without disposing them.</summary>
    public void ClearItems()
    {
        foreach (var item in GetItemsSnapshot())
        {
            Controls.Remove(item);
        }
    }

    /// <inheritdoc />
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is BootstrapListGroupItem item && _subscribedItems.Add(item))
        {
            item.Click += OnItemClick;
        }
    }

    /// <inheritdoc />
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is BootstrapListGroupItem item && _subscribedItems.Remove(item))
        {
            item.Click -= OnItemClick;
        }

        base.OnControlRemoved(e);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var item in _subscribedItems)
            {
                item.Click -= OnItemClick;
            }

            _subscribedItems.Clear();
        }

        base.Dispose(disposing);
    }

    internal void RaiseItemClick(BootstrapListGroupItem item)
    {
        ItemClick?.Invoke(this, new BootstrapListGroupItemEventArgs(item));
    }

    internal List<BootstrapListGroupItem> GetItemsSnapshot()
    {
        var result = new List<BootstrapListGroupItem>();
        foreach (Control control in Controls)
        {
            if (control is BootstrapListGroupItem item)
            {
                result.Add(item);
            }
        }

        return result;
    }

    private void OnItemClick(object? sender, EventArgs e)
    {
        if (sender is BootstrapListGroupItem item && item.Actionable && item.Enabled)
        {
            RaiseItemClick(item);
        }
    }
}
