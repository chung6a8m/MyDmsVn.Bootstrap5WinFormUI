using System;
using System.ComponentModel;
using MyDmsVn.Bootstrap5WinFormUI.Icons;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Describes one caller-owned command or separator in a <see cref="BootstrapDropdown"/>.
/// </summary>
public sealed class BootstrapDropdownItem
{
    private string _text;

    /// <summary>
    /// Initializes a normal command item.
    /// </summary>
    public BootstrapDropdownItem()
        : this(BootstrapDropdownItemKind.Item)
    {
    }

    /// <summary>
    /// Initializes an item with the requested immutable kind.
    /// </summary>
    /// <param name="kind">The command or separator kind.</param>
    public BootstrapDropdownItem(BootstrapDropdownItemKind kind)
    {
        if (!Enum.IsDefined(typeof(BootstrapDropdownItemKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported dropdown item kind.");
        }

        Kind = kind;
        _text = string.Empty;
        Enabled = true;
    }

    /// <summary>
    /// Gets the immutable command/separator kind.
    /// </summary>
    [Category("Behavior")]
    public BootstrapDropdownItemKind Kind { get; }

    /// <summary>
    /// Gets or sets the command caption. Null assignments are normalized to an empty string.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue("")]
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets an optional source-neutral icon descriptor.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public IconDescriptor? Icon { get; set; }

    /// <summary>
    /// Gets or sets whether a command row can be activated.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets checked presentation state. Activation does not toggle this value automatically.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Checked { get; set; }

    /// <summary>
    /// Gets or sets caller-defined data associated with the item.
    /// </summary>
    [Category("Data")]
    [DefaultValue(null)]
    public object? Tag { get; set; }

    /// <summary>
    /// Occurs when an enabled command item is activated by the owned native dropdown.
    /// </summary>
    public event EventHandler? Click;

    internal void RaiseClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
}
