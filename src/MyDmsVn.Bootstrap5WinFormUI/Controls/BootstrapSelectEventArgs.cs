using System;
using System.ComponentModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides data for a completed item-level <see cref="BootstrapSelect"/> selection change.
/// </summary>
public sealed class BootstrapSelectItemEventArgs : EventArgs
{
    internal BootstrapSelectItemEventArgs(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        BootstrapSelectEventArgsValidation.ValidateReason(reason);
        Reason = reason;
    }

    /// <summary>
    /// Gets the caller-owned item affected by the change.
    /// </summary>
    public BootstrapSelectItem Item { get; }

    /// <summary>
    /// Gets the origin of the change.
    /// </summary>
    public BootstrapSelectChangeReason Reason { get; }
}

/// <summary>
/// Provides cancellable data for an item-level <see cref="BootstrapSelect"/> selection change.
/// </summary>
public sealed class BootstrapSelectItemCancelEventArgs : CancelEventArgs
{
    internal BootstrapSelectItemCancelEventArgs(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        BootstrapSelectEventArgsValidation.ValidateReason(reason);
        Reason = reason;
    }

    /// <summary>
    /// Gets the caller-owned item affected by the pending change.
    /// </summary>
    public BootstrapSelectItem Item { get; }

    /// <summary>
    /// Gets the origin of the pending change.
    /// </summary>
    public BootstrapSelectChangeReason Reason { get; }
}

internal static class BootstrapSelectEventArgsValidation
{
    internal static void ValidateReason(BootstrapSelectChangeReason reason)
    {
        if (!Enum.IsDefined(typeof(BootstrapSelectChangeReason), reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unsupported BootstrapSelect change reason.");
        }
    }
}
