using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal enum BootstrapSelectResultRowKind
{
    GroupHeader = 0,
    Item = 1,
    CreateValue = 2,
    Loading = 3,
    LoadMoreError = 4,
    Empty = 5,
    Instruction = 6,
    Error = 7
}

internal sealed class BootstrapSelectResultRow
{
    private BootstrapSelectResultRow(
        BootstrapSelectResultRowKind kind,
        BootstrapSelectItem? item,
        string text,
        bool isSelected,
        string? customValueText = null)
    {
        Kind = kind;
        Item = item;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        IsSelected = isSelected;
        CustomValueText = customValueText;
    }

    internal BootstrapSelectResultRowKind Kind { get; }
    internal BootstrapSelectItem? Item { get; }
    internal string Text { get; }
    internal bool IsSelected { get; }
    internal string? CustomValueText { get; }

    internal static BootstrapSelectResultRow GroupHeader(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        return new BootstrapSelectResultRow(BootstrapSelectResultRowKind.GroupHeader, null, text, false);
    }

    internal static BootstrapSelectResultRow ItemRow(BootstrapSelectItem item, bool isSelected)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        return new BootstrapSelectResultRow(BootstrapSelectResultRowKind.Item, item, item.Text, isSelected);
    }

    internal static BootstrapSelectResultRow CreateValue(string searchText)
    {
        if (searchText is null) throw new ArgumentNullException(nameof(searchText));
        return new BootstrapSelectResultRow(BootstrapSelectResultRowKind.CreateValue, null, "Create '" + searchText + "'", false, searchText);
    }

    internal static BootstrapSelectResultRow Message(BootstrapSelectResultRowKind kind, string text)
    {
        if (kind == BootstrapSelectResultRowKind.GroupHeader || kind == BootstrapSelectResultRowKind.Item)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Use the dedicated row factory for item or group content.");
        }
        if (text is null) throw new ArgumentNullException(nameof(text));
        return new BootstrapSelectResultRow(kind, null, text, false);
    }
}
