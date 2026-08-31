using System;
using MyDmsVn.Bootstrap5WinFormUI.Icons;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Defines the content and transient behavior of a Toast created by the application-level Toast service.</summary>
public sealed class BootstrapToastOptions
{
    private string _title = string.Empty;
    private string _text = string.Empty;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private int _autoHideDelay = 5000;
    private int _animationDuration = 200;

    /// <summary>Gets or sets the optional single-line notification title.</summary>
    public string Title
    {
        get => _title;
        set => _title = value ?? string.Empty;
    }

    /// <summary>Gets or sets the notification body text.</summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    /// <summary>Gets or sets the semantic Bootstrap-inspired color variant.</summary>
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapFeedbackRenderLogic.ValidateVariant(value);
            _variant = value;
        }
    }

    /// <summary>Gets or sets an optional source-neutral icon for the live Toast.</summary>
    public IconDescriptor? Icon { get; set; }

    /// <summary>Gets or sets whether the live Toast exposes its close affordance.</summary>
    public bool Dismissible { get; set; } = true;

    /// <summary>Gets or sets whether the live Toast dismisses after its auto-hide delay.</summary>
    public bool AutoHide { get; set; } = true;

    /// <summary>Gets or sets the auto-hide delay in milliseconds.</summary>
    public int AutoHideDelay
    {
        get => _autoHideDelay;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Auto-hide delay must be greater than zero.");
            }

            _autoHideDelay = value;
        }
    }

    /// <summary>Gets or sets the enter and exit animation duration in milliseconds.</summary>
    public int AnimationDuration
    {
        get => _animationDuration;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Animation duration must be greater than zero.");
            }

            _animationDuration = value;
        }
    }

    /// <summary>Gets or sets whether this notification is retained in service history.</summary>
    public bool IncludeInHistory { get; set; } = true;
}
