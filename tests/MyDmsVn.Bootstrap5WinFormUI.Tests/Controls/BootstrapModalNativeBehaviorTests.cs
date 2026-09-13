using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapModalNativeBehaviorTests
{
    [Test]
    public void NativeShowDialogDisablesOwnerDuringLoopAndRestoresItAfterExit()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var duringLoop = false;

        var result = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new Form();
            owner.Show();
            var dialogResult = ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                duringLoop = !IsWindowEnabled(owner.Handle) && dialog.ContainsFocus;
                dialog.DialogResult = DialogResult.OK;
            });
            Assert.That(owner.Enabled, Is.True);
            return dialogResult;
        });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(duringLoop, Is.True);
            Assert.That(result, Is.EqualTo(DialogResult.OK));
        }));
    }

    [Test]
    public void NativeAcceptAndCancelButtonsReturnTheirDialogResultsExactlyOnce()
    {
        using var host = new WinFormsMessageLoopTestHost();

        var results = host.Run(() =>
        {
            var values = new List<DialogResult>();
            foreach (var useAccept in new[] { true, false })
            {
                using var owner = new Form();
                using var modal = new Form();
                using var button = new Button { DialogResult = useAccept ? DialogResult.OK : DialogResult.Cancel };
                modal.Controls.Add(button);
                if (useAccept) modal.AcceptButton = button; else modal.CancelButton = button;
                owner.Show();
                values.Add(ModalTestHost.ShowAndDrive(modal, owner, _ => button.PerformClick()));
            }

            return values;
        });

        Assert.That(results, Is.EqualTo(new[] { DialogResult.OK, DialogResult.Cancel }));
    }

    [Test]
    public void NativeDialogResultCancellationKeepsLoopOpenUntilNextResult()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var closingCount = 0;

        var result = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new Form();
            modal.FormClosing += (_, e) =>
            {
                e.Cancel = ++closingCount == 1;
                if (e.Cancel)
                {
                    modal.BeginInvoke((MethodInvoker)(() => modal.DialogResult = DialogResult.OK));
                }
            };
            owner.Show();
            return ModalTestHost.ShowAndDrive(modal, owner, dialog => dialog.DialogResult = DialogResult.Cancel);
        });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result, Is.EqualTo(DialogResult.OK));
            Assert.That(closingCount, Is.EqualTo(2));
        }));
    }

    [Test]
    public void PreShowBackdropIsDisabledButBackdropCreatedFromShownRemainsEnabled()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var states = host.Run(() =>
        {
            using var owner = new Form { Bounds = new Rectangle(100, 100, 500, 400) };
            using var preShowBackdrop = new Form { ShowInTaskbar = false };
            using var modal = new Form();
            owner.Show();
            preShowBackdrop.Show();
            bool preEnabled = true;
            bool postEnabled = false;
            bool ownerEnabled = true;
            var result = ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                using var postSnapshotBackdrop = new Form { ShowInTaskbar = false, Bounds = owner.Bounds };
                postSnapshotBackdrop.Show(owner);
                dialog.BringToFront();
                preEnabled = IsWindowEnabled(preShowBackdrop.Handle);
                postEnabled = IsWindowEnabled(postSnapshotBackdrop.Handle);
                ownerEnabled = IsWindowEnabled(owner.Handle);
                dialog.DialogResult = DialogResult.Cancel;
            });
            return (preEnabled, postEnabled, ownerEnabled, result);
        });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(states.preEnabled, Is.False);
            Assert.That(states.postEnabled, Is.True);
            Assert.That(states.ownerEnabled, Is.False);
            Assert.That(states.result, Is.EqualTo(DialogResult.Cancel));
        }));
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr windowHandle);
}
