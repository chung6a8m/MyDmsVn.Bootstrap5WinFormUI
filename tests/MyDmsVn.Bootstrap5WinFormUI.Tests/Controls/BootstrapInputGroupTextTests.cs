using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapInputGroupTextTests
{
    [Test]
    public void DefaultsAreDesignerSafeAndNonFocusable()
    {
        using var addon = new BootstrapInputGroupText();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(addon.Text, Is.Empty);
            Assert.That(addon.Icon, Is.Null);
            Assert.That(addon.TextAlign, Is.EqualTo(ContentAlignment.MiddleCenter));
            Assert.That(addon.BorderRadius, Is.EqualTo(-1));
            Assert.That(addon.TabStop, Is.False);
            Assert.That(addon.AccessibleRole, Is.EqualTo(AccessibleRole.StaticText));
            Assert.That(TypeDescriptor.GetAttributes(addon)[typeof(DefaultPropertyAttribute)], Is.EqualTo(new DefaultPropertyAttribute("Text")));
        }));
    }

    [Test]
    public void TextNormalizesNullAndPreferredWidthTracksContent()
    {
        using var addon = new BootstrapInputGroupText();
        var empty = addon.GetPreferredSize(Size.Empty);
        addon.Text = "Username";
        var populated = addon.GetPreferredSize(Size.Empty);
        addon.Text = null!;

        Assert.That(populated.Width, Is.GreaterThan(empty.Width));
        Assert.That(addon.Text, Is.Empty);
    }

    [Test]
    public void ConnectedContractIsExplicitAndReportsTargetHeight()
    {
        using var addon = new BootstrapInputGroupText();
        var connected = (IBootstrapConnectedControl)addon;
        connected.ConnectedSizeOverride = BootstrapConnectedControlSize.Large;

        Assert.That(connected.GetConnectedSafeMinimumHeight(BootstrapConnectedControlSize.Large, 96), Is.GreaterThan(0));
        Assert.That(typeof(BootstrapInputGroupText).GetProperty("ConnectedSizeOverride"), Is.Null);
    }

    [Test]
    public void InvalidRadiusIsRejectedBeforeMutation()
    {
        using var addon = new BootstrapInputGroupText();
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => addon.BorderRadius = -2));
        Assert.That(addon.BorderRadius, Is.EqualTo(-1));
    }
}
