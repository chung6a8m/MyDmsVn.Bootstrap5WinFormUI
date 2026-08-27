using System.ComponentModel;
using MyDmsVn.Bootstrap5WinFormUI.Icons;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Describes one navigation entry rendered by <see cref="BootstrapSidebar"/>.
/// </summary>
[DefaultProperty(nameof(Text))]
public sealed class BootstrapSidebarItem
{
    private readonly BindingList<BootstrapSidebarItem> _items = new BindingList<BootstrapSidebarItem>();

    /// <summary>
    /// Gets or sets the item text.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue("")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional source-neutral navigation icon.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public IconDescriptor? Icon { get; set; }

    /// <summary>
    /// Gets or sets optional badge text.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue("")]
    public string BadgeText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the item may be activated.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether nested items are expanded.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool Expanded { get; set; }

    /// <summary>
    /// Gets whether this item is currently selected by its owning sidebar.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected { get; internal set; }

    /// <summary>
    /// Gets or sets application-defined data associated with this item.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? Tag { get; set; }

    /// <summary>
    /// Gets the nested navigation items.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BindingList<BootstrapSidebarItem> Items => _items;
}
