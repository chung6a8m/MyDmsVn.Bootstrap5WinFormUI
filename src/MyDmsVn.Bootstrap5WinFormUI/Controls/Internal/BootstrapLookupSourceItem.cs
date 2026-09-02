namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupSourceItem
{
    internal BootstrapLookupSourceItem(object item, object? value, string displayText, int sourceIndex)
    {
        Item = item;
        Value = value;
        DisplayText = displayText;
        SourceIndex = sourceIndex;
    }

    internal object Item { get; }
    internal object? Value { get; }
    internal string DisplayText { get; }
    internal int SourceIndex { get; }
}
