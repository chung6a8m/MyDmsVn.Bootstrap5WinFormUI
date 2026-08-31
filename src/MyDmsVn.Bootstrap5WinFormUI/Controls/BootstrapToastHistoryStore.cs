using System;
using System.Collections.Generic;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapToastHistoryStore
{
    private readonly List<BootstrapToastHistoryItem> _items = new List<BootstrapToastHistoryItem>();
    private int _capacity;

    public BootstrapToastHistoryStore(int capacity)
    {
        ValidateCapacity(capacity);
        _capacity = capacity;
    }

    public int Capacity
    {
        get => _capacity;
        set
        {
            ValidateCapacity(value);
            if (_capacity == value)
            {
                return;
            }

            _capacity = value;
            TrimToCapacity();
        }
    }

    public int UnreadCount
    {
        get
        {
            var count = 0;
            foreach (var item in _items)
            {
                if (!item.IsRead)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int Count => _items.Count;

    public bool Add(BootstrapToastHistoryItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (FindIndex(item.Id) >= 0)
        {
            return false;
        }

        _items.Add(item);
        TrimToCapacity();
        return true;
    }

    public bool Remove(Guid id)
    {
        var index = FindIndex(id);
        if (index < 0)
        {
            return false;
        }

        _items.RemoveAt(index);
        return true;
    }

    public bool MarkAsRead(Guid id)
    {
        var index = FindIndex(id);
        if (index < 0 || _items[index].IsRead)
        {
            return false;
        }

        _items[index] = CopyWithReadState(_items[index], true);
        return true;
    }

    public bool MarkAllAsRead()
    {
        var changed = false;
        for (var index = 0; index < _items.Count; index++)
        {
            if (_items[index].IsRead)
            {
                continue;
            }

            _items[index] = CopyWithReadState(_items[index], true);
            changed = true;
        }

        return changed;
    }

    public bool Clear()
    {
        if (_items.Count == 0)
        {
            return false;
        }

        _items.Clear();
        return true;
    }

    public IReadOnlyList<BootstrapToastHistoryItem> SnapshotNewestFirst()
    {
        var snapshot = new BootstrapToastHistoryItem[_items.Count];
        for (var index = 0; index < _items.Count; index++)
        {
            snapshot[index] = _items[_items.Count - index - 1];
        }

        return snapshot;
    }

    private static BootstrapToastHistoryItem CopyWithReadState(BootstrapToastHistoryItem item, bool isRead)
    {
        return new BootstrapToastHistoryItem(
            item.Id,
            item.CreatedAtUtc,
            item.Title,
            item.Text,
            item.Variant,
            isRead);
    }

    private int FindIndex(Guid id)
    {
        for (var index = 0; index < _items.Count; index++)
        {
            if (_items[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }

    private void TrimToCapacity()
    {
        var removeCount = _items.Count - _capacity;
        if (removeCount > 0)
        {
            _items.RemoveRange(0, removeCount);
        }
    }

    private static void ValidateCapacity(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "History capacity must be greater than zero.");
        }
    }
}
