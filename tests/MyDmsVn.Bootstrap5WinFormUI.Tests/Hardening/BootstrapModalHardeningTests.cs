using System;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Hardening;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapModalHardeningTests
{
    [Test]
    public void SameInstanceCanShowDismissAndShowAgainWithoutBackdropLeak()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var results = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            owner.Show();
            var first = ModalTestHost.ShowAndDrive(modal, owner, dialog => dialog.DialogResult = DialogResult.Cancel);
            var second = ModalTestHost.ShowAndDrive(modal, owner, dialog => dialog.DialogResult = DialogResult.OK);
            var leaked = Application.OpenForms.Cast<Form>().Count(form => form.GetType().Name == "BootstrapModalBackdropWindow");
            return (first, second, leaked);
        });

        Assert.That(results, Is.EqualTo((DialogResult.Cancel, DialogResult.OK, 0)));
    }

    [Test]
    public void CallerCloseDoesNotEnterFrameworkDismissState()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var pending = true;
        host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            owner.Show();
            ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                dialog.Close();
                pending = modal.FrameworkDismissPending;
            });
        });

        Assert.That(pending, Is.False);
    }

    [Test]
    public void InvalidInitialFocusControlFallsBackWithoutCrashing()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var fallbackFocused = false;
        host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            using var outsider = new TextBox();
            using var fallback = new TextBox();
            modal.BodyPanel.Controls.Add(fallback);
            modal.InitialFocusControl = outsider;
            owner.Show();
            ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                fallbackFocused = fallback.Focused;
                dialog.DialogResult = DialogResult.Cancel;
            });
        });

        Assert.That(fallbackFocused, Is.True);
    }

    [Test]
    public void ChangingBackdropModeWhileOpenAppliesImmediately()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var states = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            owner.Show();
            var removed = false;
            var recreated = false;
            ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                modal.BackdropMode = BootstrapModalBackdropMode.None;
                removed = modal.BackdropWindow is null;
                modal.BackdropMode = BootstrapModalBackdropMode.Static;
                recreated = modal.BackdropWindow is not null;
                dialog.DialogResult = DialogResult.Cancel;
            });
            return (removed, recreated);
        });

        Assert.That(states, Is.EqualTo((true, true)));
    }

    [Test]
    public void ModelessShowDoesNotCreateBackdropOrSecondModalityContract()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var backdropCreated = host.Run(() =>
        {
            using var modal = new BootstrapModal();
            modal.Show();
            Application.DoEvents();
            var created = modal.BackdropWindow is not null;
            modal.Close();
            return created;
        });

        Assert.That(backdropCreated, Is.False);
    }

    [Test]
    public void ExplicitBorderRadiusCreatesRoundedWindowRegion()
    {
        using var modal = new BootstrapModal { BorderRadius = 12 };
        modal.CreateControl();
        modal.PerformLayout();

        Assert.That(modal.Region, Is.Not.Null);
    }
}
