using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal static class BootstrapOverlayActivationDomain
{
    internal static bool IsOwnerWindow(IntPtr window, Form? ownerForm)
    {
        if (window == IntPtr.Zero || ownerForm?.IsHandleCreated != true)
        {
            return false;
        }

        if (window == ownerForm.Handle)
        {
            return true;
        }

        var control = Control.FromChildHandle(window);
        return control?.FindForm() == ownerForm;
    }

    internal static bool IsPopupWindow(
        IntPtr window,
        BootstrapOverlayDropDown? dropDown,
        BootstrapOverlaySurface? surface)
    {
        if (window == IntPtr.Zero
            || dropDown?.IsHandleCreated != true
            || surface is null)
        {
            return false;
        }

        if (window == dropDown.Handle)
        {
            return true;
        }

        var control = Control.FromChildHandle(window);
        return control is not null
            && (ReferenceEquals(control, dropDown)
                || ReferenceEquals(control, surface)
                || surface.Contains(control));
    }
}
