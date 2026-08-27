using System;
using System.ComponentModel;
using MyDmsVn.Bootstrap5WinFormUI.Icons;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Describes one navigation entry rendered by <see cref="BootstrapSidebar"/>.
/// </summary>
[DefaultProperty(nameof(Text))]
public sealed class BootstrapSidebarItem : INotifyPropertyChanged
{
    private readonly BindingList<BootstrapSidebarItem> _items = new BindingList<BootstrapSidebarItem>();
    private string _text = string.Empty;
    private IconDescriptor? _icon;
    private string _badgeText = string.Empty;
    private bool _enabled = true;
    private bool _expanded;
    private bool _selected;
    private object? _tag;

    /// <summary>
    /// Gets or sets the item text.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue("")]
    public string Text
    {
        get => _text;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_text, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _text = normalized;
            OnPropertyChanged(nameof(Text));
        }
    }

    /// <summary>
    /// Gets or sets an optional source-neutral navigation icon.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public IconDescriptor? Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value))
            {
                return;
            }

            _icon = value;
            OnPropertyChanged(nameof(Icon));
        }
    }

    /// <summary>
    /// Gets or sets optional badge text.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue("")]
    public string BadgeText
    {
        get => _badgeText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_badgeText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _badgeText = normalized;
            OnPropertyChanged(nameof(BadgeText));
        }
    }

    /// <summary>
    /// Gets or sets whether the item may be activated.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            OnPropertyChanged(nameof(Enabled));
        }
    }

    /// <summary>
    /// Gets or sets whether nested items are expanded.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value)
            {
                return;
            }

            _expanded = value;
            OnPropertyChanged(nameof(Expanded));
        }
    }

    /// <summary>
    /// Gets whether this item is currently selected by its owning sidebar.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        internal set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            OnPropertyChanged(nameof(Selected));
        }
    }

    /// <summary>
    /// Gets or sets application-defined data associated with this item.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? Tag
    {
        get => _tag;
        set
        {
            if (ReferenceEquals(_tag, value))
            {
                return;
            }

            _tag = value;
            OnPropertyChanged(nameof(Tag));
        }
    }

    /// <summary>
    /// Gets the nested navigation items.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BindingList<BootstrapSidebarItem> Items => _items;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
