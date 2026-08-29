using System;
using System.ComponentModel;
using MyDmsVn.Bootstrap5WinFormUI.Icons;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Describes one caller-owned logical value that can be presented and selected by <see cref="BootstrapSelect"/>.
/// </summary>
public class BootstrapSelectItem
{
    private string _text;

    /// <summary>
    /// Initializes an item with an immutable non-null logical value and display text.
    /// </summary>
    /// <param name="value">The non-null logical identity of the item.</param>
    /// <param name="text">The non-null display text.</param>
    public BootstrapSelectItem(object value, string text)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Gets the immutable non-null logical identity used for comparison, deduplication, and selection reconciliation.
    /// </summary>
    [Category("Data")]
    public object Value { get; }

    /// <summary>
    /// Gets or sets the non-null display text.
    /// </summary>
    [Category("Appearance")]
    public string Text
    {
        get => _text;
        set => _text = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets whether the item is unavailable for new selection.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets optional grouping metadata used to create non-selectable result headers.
    /// </summary>
    [Category("Data")]
    [DefaultValue(null)]
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets an optional source-neutral icon descriptor.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public IconDescriptor? Icon { get; set; }

    /// <summary>
    /// Gets or sets arbitrary caller-owned data associated with the item.
    /// </summary>
    [Category("Data")]
    [DefaultValue(null)]
    public object? Tag { get; set; }
}
