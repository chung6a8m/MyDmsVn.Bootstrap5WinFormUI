using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapSelect
{
    private void OnFocusStateChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }
}
