namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Specifies lookup behavior after Enter commits a selection.</summary>
public enum BootstrapLookupEnterKeyBehavior
{
    /// <summary>Commits and remains in the current control or cell.</summary>
    CommitSelection,
    /// <summary>Commits and delegates forward navigation to the owner.</summary>
    CommitSelectionAndMoveNext
}
