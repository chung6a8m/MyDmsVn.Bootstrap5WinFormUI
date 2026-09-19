using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListGroupTests
{
    [Test]
    public void DefaultsDefineACompositionOnlyListContract()
    {
        using var group = new BootstrapListGroup();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group, Is.InstanceOf<Panel>());
            Assert.That(group.Orientation, Is.EqualTo(Orientation.Vertical));
            Assert.That(group.Flush, Is.False);
            Assert.That(group.BorderRadius, Is.EqualTo(-1));
            Assert.That(group.AutoSize, Is.True);
            Assert.That(group.AutoSizeMode, Is.EqualTo(AutoSizeMode.GrowAndShrink));
            Assert.That(group.TabStop, Is.False);
            Assert.That(group.AccessibleRole, Is.EqualTo(AccessibleRole.List));
        }));
    }

    [Test]
    public void InvalidOrientationAndRadiusAreRejected()
    {
        using var group = new BootstrapListGroup();

        Assert.Throws<InvalidEnumArgumentException>((Action)(() => group.Orientation = (Orientation)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => group.BorderRadius = -2));
    }
}
