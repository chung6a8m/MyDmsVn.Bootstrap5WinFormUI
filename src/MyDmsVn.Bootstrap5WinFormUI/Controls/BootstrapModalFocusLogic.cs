using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapModalFocusLogic
{
    public static void ApplyInitialFocus(BootstrapModal modal, Control? requested)
    {
        if (IsUsableDescendant(modal, requested)) { requested!.Focus(); return; }
        if (IsUsableDescendant(modal, modal.ActiveControl)) { modal.ActiveControl!.Focus(); return; }
        modal.SelectNextControl(null, true, true, true, false);
    }

    private static bool IsUsableDescendant(Control modal, Control? control)
    {
        return control is not null && !control.IsDisposed && control.CanSelect && control.Visible && control.Enabled && modal.Contains(control);
    }
}
