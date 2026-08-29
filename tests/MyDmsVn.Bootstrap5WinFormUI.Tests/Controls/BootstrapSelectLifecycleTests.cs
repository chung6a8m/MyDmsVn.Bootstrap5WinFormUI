using System;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSelectLifecycleTests
{
    [Test]
    public void DisabledControlClosesPopupAndPreservesSelection()
    {
        using var form = new Form();
        using var select = new BootstrapSelect();
        var item = new BootstrapSelectItem(1, "Alpha");
        select.Items.Add(item);
        select.Select(item);
        form.Controls.Add(select);
        form.Show();
        select.CreateControl();
        select.OpenDropDownInternal();
        Assert.That(select.IsDropDownOpenForTest, Is.True);

        select.Enabled = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.False);
            Assert.That(select.SelectedValue, Is.EqualTo(1));
        }));
    }

    [Test]
    public void HandleRecreationClosesPopupPreservesSelectionAndCanOpenAgain()
    {
        using var form = new Form();
        using var select = new RecreatingBootstrapSelect();
        var item = new BootstrapSelectItem(7, "Seven");
        select.Items.Add(item);
        select.Select(item);
        form.Controls.Add(select);
        form.Show();
        select.CreateControl();
        select.OpenDropDownInternal();
        Assert.That(select.IsDropDownOpenForTest, Is.True);

        select.RecreateForTest();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.False);
            Assert.That(select.SelectedValue, Is.EqualTo(7));
            Assert.That(select.IsHandleCreated, Is.True);
        }));
        select.OpenDropDownInternal();
        Assert.That(select.IsDropDownOpenForTest, Is.True);
    }

    private sealed class RecreatingBootstrapSelect : BootstrapSelect
    {
        internal void RecreateForTest()
        {
            RecreateHandle();
        }
    }
}
