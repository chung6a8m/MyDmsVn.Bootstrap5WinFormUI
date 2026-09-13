using System;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapModalTests
{
    [Test]
    public void HeaderUsesInheritedTextAndOffersAccessibleCloseCommand()
    {
        using var modal = new BootstrapModal { Text = "Order details" };

        var label = modal.Controls.Find("BootstrapModalTitle", true).Single() as Label;
        var close = modal.Controls.Find("BootstrapModalClose", true).Single() as Button;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(label!.Text, Is.EqualTo("Order details"));
            Assert.That(close!.AccessibleName, Is.EqualTo("Close"));
            Assert.That(modal.ShowCloseButton, Is.True);
        }));
    }

    [Test]
    public void HeaderCloseRequestsNativeCancelAndRespectsFormClosingCancellation()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var closingCount = 0;

        var result = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            var close = (Button)modal.Controls.Find("BootstrapModalClose", true).Single();
            modal.FormClosing += (_, e) =>
            {
                closingCount++;
                e.Cancel = closingCount == 1;
                if (e.Cancel) modal.BeginInvoke((MethodInvoker)close.PerformClick);
            };
            owner.Show();
            return ModalTestHost.ShowAndDrive(modal, owner, _ => close.PerformClick());
        });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result, Is.EqualTo(DialogResult.Cancel));
            Assert.That(closingCount, Is.EqualTo(2));
        }));
    }

    [Test]
    public void NativeDialogResultButtonRemainsAuthoritative()
    {
        using var host = new WinFormsMessageLoopTestHost();

        var result = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            using var ok = new Button { Text = "Save", DialogResult = DialogResult.OK };
            modal.FooterPanel.Controls.Add(ok);
            modal.AcceptButton = ok;
            owner.Show();
            return ModalTestHost.ShowAndDrive(modal, owner, _ => ok.PerformClick());
        });

        Assert.That(result, Is.EqualTo(DialogResult.OK));
    }

    [Test]
    public void FooterCollapsesWithoutVisibleActions()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var states = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            using var action = new Button();
            modal.FooterPanel.Controls.Add(action);
            owner.Show();
            var visibleWithAction = false;
            var hiddenWithoutVisibleAction = false;
            ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                visibleWithAction = modal.FooterPanel.Visible;
                action.Visible = false;
                modal.PerformLayout();
                hiddenWithoutVisibleAction = !modal.FooterPanel.Visible;
                dialog.DialogResult = DialogResult.Cancel;
            });
            return (visibleWithAction, hiddenWithoutVisibleAction);
        });

        Assert.That(states, Is.EqualTo((true, true)));
    }
}
