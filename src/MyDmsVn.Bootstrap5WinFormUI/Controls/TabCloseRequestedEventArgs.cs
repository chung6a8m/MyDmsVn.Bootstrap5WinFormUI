using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>The input route that requested a tab close. The owner decides whether to remove the page.</summary>
public enum TabCloseReason
{
    /// <summary>A pointer hit the close affordance.</summary>
    Pointer,
    /// <summary>The keyboard shortcut requested a close.</summary>
    Keyboard,
    /// <summary>An accessible close command requested a close.</summary>
    Accessibility,
    /// <summary>The owner explicitly requested a close.</summary>
    Programmatic
}

/// <summary>A request against the original native tab page, without an implicit close.</summary>
public sealed class TabCloseRequestedEventArgs : EventArgs
{
    /// <summary>Creates a request for the original native page.</summary>
    public TabCloseRequestedEventArgs(TabPage page, TabCloseReason reason)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Reason = reason;
    }

    /// <summary>The page whose close was requested.</summary>
    public TabPage Page { get; }

    /// <summary>The input route that originated the request.</summary>
    public TabCloseReason Reason { get; }
}
