using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapModalKeyboardFocusTests
{
    [Test]
    public void InitialFocusUsesValidRequestedDescendant()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var focused = false;

        host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            using var first = new TextBox { TabIndex = 0 };
            using var requested = new TextBox { TabIndex = 1 };
            modal.BodyPanel.Controls.Add(first);
            modal.BodyPanel.Controls.Add(requested);
            modal.InitialFocusControl = requested;
            owner.Show();
            ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                focused = requested.Focused;
                dialog.DialogResult = DialogResult.Cancel;
            });
        });

        Assert.That(focused, Is.True);
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void EscapeDismissesOnlyWhenEnabledAndNoNativeCancelButton(bool closeOnEscape, bool shouldDismiss)
    {
        using var modal = new TestModal { CloseOnEscape = closeOnEscape };

        var handled = modal.ProcessDialogKeyForTest(Keys.Escape);

        Assert.That(handled, Is.EqualTo(shouldDismiss));
    }

    [Test]
    public void EscapeDelegatesToNativeCancelButton()
    {
        using var modal = new TestModal();
        using var cancel = new Button { DialogResult = DialogResult.Cancel };
        modal.BodyPanel.Controls.Add(cancel);
        modal.CancelButton = cancel;

        var handled = modal.ProcessDialogKeyForTest(Keys.Escape);

        Assert.That(handled, Is.True);
    }

    private sealed class TestModal : BootstrapModal
    {
        public bool ProcessDialogKeyForTest(Keys keyData) => ProcessDialogKey(keyData);
    }
}
