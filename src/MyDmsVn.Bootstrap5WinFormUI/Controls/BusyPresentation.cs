using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Describes one local busy presentation. Cancellation is a request to the owner.</summary>
public sealed class BusyPresentation
{
    /// <summary>Creates a message and optional cancellation request.</summary>
    public BusyPresentation(string message, Action? cancelRequested = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        CancelRequested = cancelRequested;
    }

    /// <summary>Gets the message shown over the content region.</summary>
    public string Message { get; }

    /// <summary>Gets the optional callback invoked by the visible Cancel button.</summary>
    public Action? CancelRequested { get; }
}
