using System;
using System.Reflection;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectItemTests
{
    [Test]
    public void ConstructorRequiresNonNullValueAndText()
    {
        Assert.That((Action)(() => new BootstrapSelectItem(null!, "Alpha")), Throws.TypeOf<ArgumentNullException>());
        Assert.That((Action)(() => new BootstrapSelectItem(1, null!)), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ValueIsImmutableWhilePresentationMetadataRemainsMutable()
    {
        var item = new BootstrapSelectItem(42, "Alpha")
        {
            Disabled = true,
            Group = "Customers",
            Tag = "domain-object"
        };

        item.Text = "Updated";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.Value, Is.EqualTo(42));
            Assert.That(item.Text, Is.EqualTo("Updated"));
            Assert.That(item.Disabled, Is.True);
            Assert.That(item.Group, Is.EqualTo("Customers"));
            Assert.That(item.Tag, Is.EqualTo("domain-object"));
            Assert.That(typeof(BootstrapSelectItem).GetProperty(nameof(BootstrapSelectItem.Value))!.CanWrite, Is.False);
        }));
    }

    [Test]
    public void EnumsUseApprovedStableValues()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That((int)BootstrapSelectMode.Single, Is.EqualTo(0));
            Assert.That((int)BootstrapSelectMode.Multiple, Is.EqualTo(1));
            Assert.That((int)BootstrapSelectChangeReason.Programmatic, Is.EqualTo(0));
            Assert.That((int)BootstrapSelectChangeReason.Mouse, Is.EqualTo(1));
            Assert.That((int)BootstrapSelectChangeReason.Keyboard, Is.EqualTo(2));
            Assert.That((int)BootstrapSelectChangeReason.Clear, Is.EqualTo(3));
            Assert.That((int)BootstrapSelectChangeReason.ChipRemove, Is.EqualTo(4));
            Assert.That((int)BootstrapSelectChangeReason.CustomValue, Is.EqualTo(5));
            Assert.That((int)BootstrapSelectChangeReason.ModeChange, Is.EqualTo(6));
        }));
    }

    [Test]
    public void EventArgsExposeItemReasonAndCancellationWithoutPublicMutationOfIdentity()
    {
        var item = new BootstrapSelectItem("a", "Alpha");
        var changed = new BootstrapSelectItemEventArgs(item, BootstrapSelectChangeReason.Keyboard);
        var changing = new BootstrapSelectItemCancelEventArgs(item, BootstrapSelectChangeReason.Mouse)
        {
            Cancel = true
        };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(changed.Item, Is.SameAs(item));
            Assert.That(changed.Reason, Is.EqualTo(BootstrapSelectChangeReason.Keyboard));
            Assert.That(changing.Item, Is.SameAs(item));
            Assert.That(changing.Reason, Is.EqualTo(BootstrapSelectChangeReason.Mouse));
            Assert.That(changing.Cancel, Is.True);
            Assert.That(typeof(BootstrapSelectItemEventArgs).GetProperty(nameof(BootstrapSelectItemEventArgs.Item))!.CanWrite, Is.False);
            Assert.That(typeof(BootstrapSelectItemEventArgs).GetProperty(nameof(BootstrapSelectItemEventArgs.Reason))!.CanWrite, Is.False);
        }));
    }
}
