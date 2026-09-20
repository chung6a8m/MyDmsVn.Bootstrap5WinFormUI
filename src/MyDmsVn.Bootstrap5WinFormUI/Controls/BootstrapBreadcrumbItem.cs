using System;
using System.ComponentModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Represents one caller-owned logical location in a <c>BootstrapBreadcrumb</c> trail.
/// </summary>
public sealed class BootstrapBreadcrumbItem
{
    private string _text = "Item";

    /// <summary>Initializes an item with the default text <c>Item</c>.</summary>
    public BootstrapBreadcrumbItem()
    {
    }

    /// <summary>Initializes an item with the specified non-empty display text.</summary>
    /// <param name="text">The complete visual and accessible item text.</param>
    public BootstrapBreadcrumbItem(string text)
    {
        Text = text;
    }

    /// <summary>Gets or sets the complete visual and accessible item text.</summary>
    [Category("Appearance")]
    [DefaultValue("Item")]
    public string Text
    {
        get => _text;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Breadcrumb item text must contain at least one non-whitespace character.",
                    nameof(value));
            }

            if (string.Equals(_text, value, StringComparison.Ordinal))
            {
                return;
            }

            _text = value;
            TextChangedForOwner?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Gets or sets arbitrary caller-owned application data associated with the item.</summary>
    [Category("Data")]
    [DefaultValue(null)]
    public object? Tag { get; set; }

    internal event EventHandler? TextChangedForOwner;
}
