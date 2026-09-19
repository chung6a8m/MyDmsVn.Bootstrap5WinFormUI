using System;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListGroupItemTests
{
    [Test]
    public void DefaultsArePresentationalAndNeutral()
    {
        using var item = new SelectabilityProbeItem();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item, Is.InstanceOf<Panel>());
            Assert.That(item.Active, Is.False);
            Assert.That(item.Actionable, Is.False);
            Assert.That(item.Variant, Is.Null);
            Assert.That(item.UseThemeFont, Is.True);
            Assert.That(item.TabStop, Is.False);
            Assert.That(item.HasSelectableStyle, Is.False);
            Assert.That(item.CanSelect, Is.False);
            Assert.That(item.AccessibleRole, Is.EqualTo(AccessibleRole.ListItem));
        }));
    }

    [Test]
    public void ActionableSynchronizesTabStopAndRealSelectability()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var item = new SelectabilityProbeItem { Size = new System.Drawing.Size(160, 40) };
        form.Controls.Add(item);
        form.Show();

        item.Actionable = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.TabStop, Is.True);
            Assert.That(item.HasSelectableStyle, Is.True);
            Assert.That(item.CanSelect, Is.True);
            Assert.That(item.Focus(), Is.True);
        }));

        item.Actionable = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.TabStop, Is.False);
            Assert.That(item.HasSelectableStyle, Is.False);
            Assert.That(item.CanSelect, Is.False);
        }));
    }

    [Test]
    public void InvalidContextualVariantIsRejected()
    {
        using var item = new BootstrapListGroupItem();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => item.Variant = (BootstrapVariant)99));
        Assert.DoesNotThrow((Action)(() => item.Variant = null));
    }

    [Test]
    public void EventArgsRejectNullAndPreserveItemIdentity()
    {
        using var item = new BootstrapListGroupItem();

        Assert.Throws<ArgumentNullException>((Action)(() => _ = new BootstrapListGroupItemEventArgs(null!)));
        Assert.That(new BootstrapListGroupItemEventArgs(item).Item, Is.SameAs(item));
    }

    private sealed class SelectabilityProbeItem : BootstrapListGroupItem
    {
        public bool HasSelectableStyle => GetStyle(ControlStyles.Selectable);
    }
}
