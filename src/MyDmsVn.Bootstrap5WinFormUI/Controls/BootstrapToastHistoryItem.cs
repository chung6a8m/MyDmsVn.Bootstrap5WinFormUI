using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Represents an immutable semantic snapshot retained in Toast notification history.</summary>
public sealed class BootstrapToastHistoryItem
{
    internal BootstrapToastHistoryItem(
        Guid id,
        DateTimeOffset createdAtUtc,
        string title,
        string text,
        BootstrapVariant variant,
        bool isRead)
    {
        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The notification timestamp must use the UTC offset.", nameof(createdAtUtc));
        }

        BootstrapFeedbackRenderLogic.ValidateVariant(variant);
        Id = id;
        CreatedAtUtc = createdAtUtc;
        Title = title ?? string.Empty;
        Text = text ?? string.Empty;
        Variant = variant;
        IsRead = isRead;
    }

    /// <summary>Gets the notification identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Gets the notification title.</summary>
    public string Title { get; }

    /// <summary>Gets the notification body text.</summary>
    public string Text { get; }

    /// <summary>Gets the semantic Bootstrap-inspired color variant.</summary>
    public BootstrapVariant Variant { get; }

    /// <summary>Gets whether this immutable snapshot is marked as read.</summary>
    public bool IsRead { get; }
}
