using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectSearchTextBox : BootstrapTextBox
{
    internal BootstrapSelectSearchTextBox()
    {
        AccessibleRole = AccessibleRole.Client;
        AccessibleName = null;
        AccessibleDescription = null;

        Editor.AccessibleRole = AccessibleRole.Text;
        Editor.AccessibleName = "Search";
        Editor.AccessibleDescription = "Filters BootstrapSelect results.";
    }

    internal event Action<bool>? TabNavigationRequested;

    internal void FocusEditorAtEnd()
    {
        Focus();
        Editor.Focus();
        Editor.SelectionStart = Editor.TextLength;
        Editor.SelectionLength = 0;
    }

    internal void AppendCharacter(char character)
    {
        if (char.IsControl(character))
        {
            return;
        }

        Editor.AppendText(character.ToString());
        FocusEditorAtEnd();
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        if (keyCode == Keys.Tab &&
            (modifiers & (Keys.Alt | Keys.Control)) == Keys.None)
        {
            var reverse = (modifiers & Keys.Shift) == Keys.Shift;
            TabNavigationRequested?.Invoke(reverse);
            return true;
        }

        return base.ProcessDialogKey(keyData);
    }
}
