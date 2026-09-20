using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides data for a breadcrumb ancestor activation.</summary>
public sealed class BootstrapBreadcrumbItemClickedEventArgs : EventArgs
{
    internal BootstrapBreadcrumbItemClickedEventArgs(
        BootstrapBreadcrumbItem item,
        int index)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
    }

    /// <summary>Gets the activated caller-owned item.</summary>
    public BootstrapBreadcrumbItem Item { get; }

    /// <summary>Gets the item's logical index in the breadcrumb collection at activation time.</summary>
    public int Index { get; }
}
