namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Describes why a lookup selection was committed.</summary>
public enum BootstrapLookupCommitReason
{
    /// <summary>The selection was committed from the keyboard.</summary>
    Keyboard,
    /// <summary>The selection was committed by mouse activation.</summary>
    Mouse,
    /// <summary>The selection was committed through the public API.</summary>
    Programmatic,
    /// <summary>Pending text resolved to an existing exact match.</summary>
    ExactMatch,
    /// <summary>Pending text created and committed a new source item.</summary>
    CommitAndAdd,
    /// <summary>The selection was cleared.</summary>
    Clear
}
