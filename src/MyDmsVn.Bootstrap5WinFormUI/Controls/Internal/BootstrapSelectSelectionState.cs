using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectSelectionState
{
    private readonly List<BootstrapSelectItem> _selectedItems = new List<BootstrapSelectItem>();
    private readonly ReadOnlyCollection<BootstrapSelectItem> _selectedItemsView;
    private readonly IEqualityComparer<object> _valueComparer;

    internal BootstrapSelectSelectionState(BootstrapSelectMode mode, IEqualityComparer<object> valueComparer)
    {
        ValidateMode(mode);
        Mode = mode;
        _valueComparer = valueComparer ?? throw new ArgumentNullException(nameof(valueComparer));
        _selectedItemsView = _selectedItems.AsReadOnly();
    }

    internal BootstrapSelectMode Mode { get; private set; }
    internal IEqualityComparer<object> ValueComparer => _valueComparer;
    internal IReadOnlyList<BootstrapSelectItem> SelectedItems => _selectedItemsView;

    internal BootstrapSelectSelectionMutation PreviewSelect(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        ValidateReason(reason);
        if (item.Disabled || FindIndex(item.Value) >= 0)
        {
            return CreateMutation(false, reason, Mode, Array.Empty<BootstrapSelectItem>(), Array.Empty<BootstrapSelectItem>(), Snapshot());
        }

        var finalItems = Snapshot();
        var removed = new List<BootstrapSelectItem>();
        if (Mode == BootstrapSelectMode.Single && finalItems.Count > 0)
        {
            removed.AddRange(finalItems);
            finalItems.Clear();
        }

        finalItems.Add(item);
        return CreateMutation(true, reason, Mode, new[] { item }, removed, finalItems);
    }

    internal BootstrapSelectSelectionMutation TrySelect(BootstrapSelectItem item, BootstrapSelectChangeReason reason)
    {
        var mutation = PreviewSelect(item, reason);
        Apply(mutation);
        return mutation;
    }

    internal BootstrapSelectSelectionMutation PreviewRemove(object value, BootstrapSelectChangeReason reason)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        ValidateReason(reason);
        var index = FindIndex(value);
        if (index < 0)
        {
            return CreateMutation(false, reason, Mode, Array.Empty<BootstrapSelectItem>(), Array.Empty<BootstrapSelectItem>(), Snapshot());
        }

        var finalItems = Snapshot();
        var removed = finalItems[index];
        finalItems.RemoveAt(index);
        return CreateMutation(true, reason, Mode, Array.Empty<BootstrapSelectItem>(), new[] { removed }, finalItems);
    }

    internal BootstrapSelectSelectionMutation TryRemove(object value, BootstrapSelectChangeReason reason)
    {
        var mutation = PreviewRemove(value, reason);
        Apply(mutation);
        return mutation;
    }

    internal BootstrapSelectSelectionMutation PreviewClear(BootstrapSelectChangeReason reason)
    {
        ValidateReason(reason);
        if (_selectedItems.Count == 0)
        {
            return CreateMutation(false, reason, Mode, Array.Empty<BootstrapSelectItem>(), Array.Empty<BootstrapSelectItem>(), Snapshot());
        }

        return CreateMutation(true, reason, Mode, Array.Empty<BootstrapSelectItem>(), Snapshot(), new List<BootstrapSelectItem>());
    }

    internal BootstrapSelectSelectionMutation TryClear(BootstrapSelectChangeReason reason)
    {
        var mutation = PreviewClear(reason);
        Apply(mutation);
        return mutation;
    }

    internal BootstrapSelectSelectionMutation PreviewModeChange(BootstrapSelectMode mode)
    {
        ValidateMode(mode);
        if (mode == Mode)
        {
            return CreateMutation(false, BootstrapSelectChangeReason.ModeChange, mode, Array.Empty<BootstrapSelectItem>(), Array.Empty<BootstrapSelectItem>(), Snapshot());
        }

        var finalItems = Snapshot();
        var removed = new List<BootstrapSelectItem>();
        if (mode == BootstrapSelectMode.Single && finalItems.Count > 1)
        {
            for (var i = 1; i < finalItems.Count; i++)
            {
                removed.Add(finalItems[i]);
            }

            finalItems.RemoveRange(1, finalItems.Count - 1);
        }

        return CreateMutation(true, BootstrapSelectChangeReason.ModeChange, mode, Array.Empty<BootstrapSelectItem>(), removed, finalItems);
    }

    internal bool RefreshSelectedItem(BootstrapSelectItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var index = FindIndex(item.Value);
        if (index < 0)
        {
            return false;
        }

        if (ReferenceEquals(_selectedItems[index], item))
        {
            return false;
        }

        _selectedItems[index] = item;
        return true;
    }

    internal void Apply(BootstrapSelectSelectionMutation mutation)
    {
        if (mutation is null)
        {
            throw new ArgumentNullException(nameof(mutation));
        }

        if (!ReferenceEquals(mutation.Owner, this))
        {
            throw new InvalidOperationException("The selection mutation belongs to a different BootstrapSelect selection state.");
        }

        if (!mutation.Changed)
        {
            return;
        }

        _selectedItems.Clear();
        for (var i = 0; i < mutation.FinalItems.Count; i++)
        {
            _selectedItems.Add(mutation.FinalItems[i]);
        }

        Mode = mutation.TargetMode;
    }

    private int FindIndex(object value)
    {
        for (var i = 0; i < _selectedItems.Count; i++)
        {
            if (_valueComparer.Equals(_selectedItems[i].Value, value))
            {
                return i;
            }
        }

        return -1;
    }

    private List<BootstrapSelectItem> Snapshot()
    {
        return new List<BootstrapSelectItem>(_selectedItems);
    }

    private BootstrapSelectSelectionMutation CreateMutation(
        bool changed,
        BootstrapSelectChangeReason reason,
        BootstrapSelectMode targetMode,
        IEnumerable<BootstrapSelectItem> added,
        IEnumerable<BootstrapSelectItem> removed,
        IList<BootstrapSelectItem> finalItems)
    {
        return new BootstrapSelectSelectionMutation(
            this,
            changed,
            reason,
            targetMode,
            new List<BootstrapSelectItem>(added),
            new List<BootstrapSelectItem>(removed),
            finalItems);
    }

    private static void ValidateMode(BootstrapSelectMode mode)
    {
        if (!Enum.IsDefined(typeof(BootstrapSelectMode), mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported BootstrapSelect selection mode.");
        }
    }

    private static void ValidateReason(BootstrapSelectChangeReason reason)
    {
        BootstrapSelectEventArgsValidation.ValidateReason(reason);
    }
}
