using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Displays Bootstrap-inspired determinate or indeterminate progress.
/// </summary>
public class BootstrapProgressBar : Control
{
    /// <summary>Gets or sets the minimum progress value.</summary>
    public int Minimum { get; set; }

    /// <summary>Gets or sets the maximum progress value.</summary>
    public int Maximum { get; set; } = 100;

    /// <summary>Gets or sets the current progress value.</summary>
    public int Value { get; set; }

    /// <summary>Gets the current progress percentage.</summary>
    public int Percentage => 0;

    /// <summary>Gets or sets the semantic fill variant.</summary>
    public BootstrapVariant Variant { get; set; } = BootstrapVariant.Primary;

    /// <summary>Gets or sets an optional fill color override.</summary>
    public Color CustomColor { get; set; } = Color.Empty;

    /// <summary>Gets or sets the uniform logical corner radius, or -1 to use the theme default.</summary>
    public int BorderRadius { get; set; } = -1;

    /// <summary>Gets or sets whether progress text is rendered.</summary>
    public bool ShowText { get; set; }

    /// <summary>Gets or sets the composite format used for progress text.</summary>
    public string TextFormat { get; set; } = "{0}%";

    /// <summary>Gets or sets whether the filled region uses stripes.</summary>
    public bool Striped { get; set; }

    /// <summary>Gets or sets whether stripes animate while striped rendering is enabled.</summary>
    public bool Animated { get; set; }

    /// <summary>Gets or sets the animation duration used by progress transitions and looped visuals.</summary>
    public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(600);

    /// <summary>Gets or sets whether the control renders indeterminate activity.</summary>
    public bool Indeterminate { get; set; }

    /// <summary>Animates the displayed progress value to the supplied target.</summary>
    public void AnimateTo(int value)
    {
    }
}
