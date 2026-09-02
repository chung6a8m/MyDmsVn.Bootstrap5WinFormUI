namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupCommitResult
{
    private BootstrapLookupCommitResult(bool navigationAllowed, bool committed)
    {
        NavigationAllowed = navigationAllowed;
        Committed = committed;
    }

    internal bool NavigationAllowed { get; }
    internal bool Committed { get; }

    internal static BootstrapLookupCommitResult Success(bool committed = true) => new BootstrapLookupCommitResult(true, committed);
    internal static BootstrapLookupCommitResult Blocked() => new BootstrapLookupCommitResult(false, false);
}
