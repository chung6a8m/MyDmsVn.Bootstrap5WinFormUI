using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapModalBackdropWindow : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    public BootstrapModalBackdropWindow(BootstrapModalVisualState state)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        ControlBox = false;
        MinimizeBox = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = false;
        AccessibleRole = AccessibleRole.None;
        TabStop = false;
        ApplyVisualState(state);
    }

    public event EventHandler? BackdropClicked;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExNoActivate | WsExToolWindow;
            return createParams;
        }
    }

    public void ApplyVisualState(BootstrapModalVisualState state)
    {
        BackColor = state.Backdrop;
        Opacity = state.BackdropOpacity;
        Invalidate();
    }

    internal void RequestClickForTests() => BackdropClicked?.Invoke(this, EventArgs.Empty);

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left) BackdropClicked?.Invoke(this, EventArgs.Empty);
    }
}
