using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Theme;

/// <summary>
/// Defines unscaled 100%-DPI metrics used by framework controls.
/// </summary>
public sealed class BootstrapThemeMetrics
{
    private static readonly BootstrapThemeMetrics DefaultMetrics = new BootstrapThemeMetrics(
        controlHeightSmall: 28,
        controlHeight: 32,
        controlHeightLarge: 38,
        radiusSmall: 4,
        radius: 6,
        radiusLarge: 8,
        borderWidth: 1,
        focusBorderWidth: 2,
        spacingXS: 4,
        spacingSM: 8,
        spacingMD: 12,
        spacingLG: 16,
        spacingXL: 24);

    /// <summary>
    /// Initializes a new metric token set. Values are expressed at 100% DPI.
    /// </summary>
    public BootstrapThemeMetrics(
        int controlHeightSmall,
        int controlHeight,
        int controlHeightLarge,
        int radiusSmall,
        int radius,
        int radiusLarge,
        int borderWidth,
        int focusBorderWidth,
        int spacingXS,
        int spacingSM,
        int spacingMD,
        int spacingLG,
        int spacingXL)
    {
        ControlHeightSmall = EnsurePositive(controlHeightSmall, nameof(controlHeightSmall));
        ControlHeight = EnsurePositive(controlHeight, nameof(controlHeight));
        ControlHeightLarge = EnsurePositive(controlHeightLarge, nameof(controlHeightLarge));
        RadiusSmall = EnsureNonNegative(radiusSmall, nameof(radiusSmall));
        Radius = EnsureNonNegative(radius, nameof(radius));
        RadiusLarge = EnsureNonNegative(radiusLarge, nameof(radiusLarge));
        BorderWidth = EnsureNonNegative(borderWidth, nameof(borderWidth));
        FocusBorderWidth = EnsureNonNegative(focusBorderWidth, nameof(focusBorderWidth));
        SpacingXS = EnsureNonNegative(spacingXS, nameof(spacingXS));
        SpacingSM = EnsureNonNegative(spacingSM, nameof(spacingSM));
        SpacingMD = EnsureNonNegative(spacingMD, nameof(spacingMD));
        SpacingLG = EnsureNonNegative(spacingLG, nameof(spacingLG));
        SpacingXL = EnsureNonNegative(spacingXL, nameof(spacingXL));
    }

    /// <summary>
    /// Gets the default metric tokens documented by the design system.
    /// </summary>
    public static BootstrapThemeMetrics Default => DefaultMetrics;

    /// <summary>Gets the small control height.</summary>
    public int ControlHeightSmall { get; }

    /// <summary>Gets the default control height.</summary>
    public int ControlHeight { get; }

    /// <summary>Gets the large control height.</summary>
    public int ControlHeightLarge { get; }

    /// <summary>Gets the small corner radius.</summary>
    public int RadiusSmall { get; }

    /// <summary>Gets the default corner radius.</summary>
    public int Radius { get; }

    /// <summary>Gets the large corner radius.</summary>
    public int RadiusLarge { get; }

    /// <summary>Gets the normal border width.</summary>
    public int BorderWidth { get; }

    /// <summary>Gets the focus border width.</summary>
    public int FocusBorderWidth { get; }

    /// <summary>Gets the extra-small spacing token.</summary>
    public int SpacingXS { get; }

    /// <summary>Gets the small spacing token.</summary>
    public int SpacingSM { get; }

    /// <summary>Gets the medium spacing token.</summary>
    public int SpacingMD { get; }

    /// <summary>Gets the large spacing token.</summary>
    public int SpacingLG { get; }

    /// <summary>Gets the extra-large spacing token.</summary>
    public int SpacingXL { get; }

    private static int EnsurePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.");
        }

        return value;
    }

    private static int EnsureNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
        }

        return value;
    }
}
