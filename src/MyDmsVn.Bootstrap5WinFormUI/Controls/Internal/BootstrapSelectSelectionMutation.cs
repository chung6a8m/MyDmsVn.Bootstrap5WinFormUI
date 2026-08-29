using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectSelectionMutation
{
    internal BootstrapSelectSelectionMutation(
        BootstrapSelectSelectionState owner,
        bool changed,
        BootstrapSelectChangeReason reason,
        BootstrapSelectMode targetMode,
        IList<BootstrapSelectItem> addedItems,
        IList<BootstrapSelectItem> removedItems,
        IList<BootstrapSelectItem> finalItems)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Changed = changed;
        Reason = reason;
        TargetMode = targetMode;
        AddedItems = new ReadOnlyCollection<BootstrapSelectItem>(addedItems ?? throw new ArgumentNullException(nameof(addedItems)));
        RemovedItems = new ReadOnlyCollection<BootstrapSelectItem>(removedItems ?? throw new ArgumentNullException(nameof(removedItems)));
        FinalItems = new ReadOnlyCollection<BootstrapSelectItem>(finalItems ?? throw new ArgumentNullException(nameof(finalItems)));
    }

    internal BootstrapSelectSelectionState Owner { get; }
    internal bool Changed { get; }
    internal BootstrapSelectChangeReason Reason { get; }
    internal BootstrapSelectMode TargetMode { get; }
    internal IReadOnlyList<BootstrapSelectItem> AddedItems { get; }
    internal IReadOnlyList<BootstrapSelectItem> RemovedItems { get; }
    internal IReadOnlyList<BootstrapSelectItem> FinalItems { get; }
}
