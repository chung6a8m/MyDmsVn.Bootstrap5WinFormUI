namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Specifies how unmatched lookup text is resolved.</summary>
public enum BootstrapLookupUnmatchedTextBehavior
{
    /// <summary>Restores the previously committed selection.</summary>
    RestorePreviousSelection,
    /// <summary>Retains focus and reports a validation error.</summary>
    KeepFocusWithValidationError,
    /// <summary>Requests creation and commits the accepted new item.</summary>
    CommitAndAdd
}
