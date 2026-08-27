using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapTextBoxRenderLogic
{
    public static Color ResolveBorderColor(
        BootstrapThemeColors colors,
        BootstrapValidationState validationState,
        bool containsFocus,
        bool enabled)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        ValidateState(validationState);

        if (!enabled)
        {
            return colors.Disabled;
        }

        switch (validationState)
        {
            case BootstrapValidationState.Valid:
                return colors.Success;
            case BootstrapValidationState.Invalid:
                return colors.Danger;
            case BootstrapValidationState.None:
                return containsFocus ? colors.Focus : colors.Border;
            default:
                throw new ArgumentOutOfRangeException(nameof(validationState), validationState, "Unsupported validation state.");
        }
    }

    public static void ValidateState(BootstrapValidationState validationState)
    {
        if (!Enum.IsDefined(typeof(BootstrapValidationState), validationState))
        {
            throw new ArgumentOutOfRangeException(nameof(validationState), validationState, "Unsupported validation state.");
        }
    }
}
