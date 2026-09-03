using System;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Represents ordered source-member names used for lookup searching.</summary>
public sealed class BootstrapLookupSearchMemberCollection : Collection<string>
{
    internal Action<string>? MemberValidator { get; set; }

    /// <inheritdoc />
    protected override void InsertItem(int index, string item)
    {
        Validate(item, -1);
        base.InsertItem(index, item);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, string item)
    {
        Validate(item, index);
        base.SetItem(index, item);
    }

    private void Validate(string item, int replacedIndex)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        if (string.IsNullOrWhiteSpace(item)) throw new ArgumentException("A search member name cannot be empty or whitespace.", nameof(item));
        for (var i = 0; i < Count; i++)
        {
            if (i != replacedIndex && string.Equals(this[i], item, StringComparison.Ordinal))
                throw new ArgumentException("Duplicate search member names are not allowed.", nameof(item));
        }
        MemberValidator?.Invoke(item);
    }
}
