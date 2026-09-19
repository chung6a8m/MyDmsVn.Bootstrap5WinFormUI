using System;
using System.Drawing;
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
    public void DerivedCancellationAfterBaseAllowsSecondFrameworkDismissal()
    {
        using var host = new WinFormsMessageLoopTestHost(TimeSpan.FromSeconds(2));

        var state = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new CancelAfterBaseModal();
            var close = (Button)modal.Controls.Find("BootstrapModalClose", true).Single();
            modal.RetryDismiss = close.PerformClick;
            owner.Show();
            var result = ModalTestHost.ShowAndDrive(
                modal,
                owner,
                _ => close.PerformClick(),
                TimeSpan.FromSeconds(1));
            return (result, modal.ClosingCount, modal.FrameworkDismissPending);
        });

        Assert.That(state, Is.EqualTo((DialogResult.Cancel, 2, false)));
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
    public void PresetHeightIsMeasuredAgainstResolvedWidth()
    {
        using var host = new WinFormsMessageLoopTestHost();

        var sizes = host.Run(() =>
        {
            using var owner = new Form();
            owner.Show();
            return (
                small: ShowWidthSensitivePreset(owner, BootstrapModalSize.Small),
                large: ShowWidthSensitivePreset(owner, BootstrapModalSize.Large));
        });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(sizes.small, Is.EqualTo(new Size(300, 414)));
            Assert.That(sizes.large, Is.EqualTo(new Size(800, 214)));
        }));
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

    private static Size ShowWidthSensitivePreset(Form owner, BootstrapModalSize modalSize)
    {
        using var modal = new BootstrapModal
        {
            ModalSize = modalSize,
            BackdropMode = BootstrapModalBackdropMode.None
        };
        using var content = new WidthSensitiveContent { AutoSize = true, Dock = DockStyle.Top };
        modal.BodyPanel.Controls.Add(content);
        Size resolvedSize = Size.Empty;
        ModalTestHost.ShowAndDrive(modal, owner, dialog =>
        {
            resolvedSize = dialog.Size;
            dialog.DialogResult = DialogResult.Cancel;
        });
        return resolvedSize;
    }

    private sealed class CancelAfterBaseModal : BootstrapModal
    {
        public int ClosingCount { get; private set; }
        public Action? RetryDismiss { get; set; }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            ClosingCount++;
            if (ClosingCount != 1) return;

            e.Cancel = true;
            BeginInvoke((MethodInvoker)(() => RetryDismiss?.Invoke()));
        }
    }

    private sealed class WidthSensitiveContent : Control
    {
        public override Size GetPreferredSize(Size proposedSize)
        {
            var width = proposedSize.Width > 0 ? proposedSize.Width : Parent?.ClientSize.Width ?? 1;
            return new Size(Math.Max(1, width), width < 500 ? 300 : 100);
        }
    }
}
