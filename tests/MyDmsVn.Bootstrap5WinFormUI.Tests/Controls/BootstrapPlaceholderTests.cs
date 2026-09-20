using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapPlaceholderTests
{
    [Test]
    public void EnumValuesMatchBootstrapPlaceholderContract()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That((int)BootstrapPlaceholderSize.ExtraSmall, Is.EqualTo(0));
            Assert.That((int)BootstrapPlaceholderSize.Small, Is.EqualTo(1));
            Assert.That((int)BootstrapPlaceholderSize.Default, Is.EqualTo(2));
            Assert.That((int)BootstrapPlaceholderSize.Large, Is.EqualTo(3));
            Assert.That((int)BootstrapPlaceholderAnimation.None, Is.EqualTo(0));
            Assert.That((int)BootstrapPlaceholderAnimation.Glow, Is.EqualTo(1));
            Assert.That((int)BootstrapPlaceholderAnimation.Wave, Is.EqualTo(2));
        }));
    }

    [Test]
    public void DefaultsMatchPlaceholderContract()
    {
        using var placeholder = new BootstrapPlaceholder();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(placeholder.PlaceholderSize, Is.EqualTo(BootstrapPlaceholderSize.Default));
            Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.None));
            Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Secondary));
            Assert.That(placeholder.CustomColor, Is.EqualTo(Color.Empty));
            Assert.That(placeholder.BorderRadius, Is.Zero);
            Assert.That(placeholder.AnimationDuration, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(placeholder.AutoSize, Is.True);
            Assert.That(placeholder.BackColor, Is.EqualTo(Color.Transparent));
            Assert.That(placeholder.TabStop, Is.False);
            Assert.That(placeholder.AccessibleRole, Is.EqualTo(AccessibleRole.None));
            Assert.That(placeholder.Cursor, Is.SameAs(Cursors.WaitCursor));
        }));
    }

    [Test]
    public void InvalidAssignmentsThrowBeforeMutatingState()
    {
        using var placeholder = new BootstrapPlaceholder
        {
            PlaceholderSize = BootstrapPlaceholderSize.Small,
            Animation = BootstrapPlaceholderAnimation.Glow,
            Variant = BootstrapVariant.Success,
            CustomColor = Color.MediumPurple,
            BorderRadius = 8,
            AnimationDuration = TimeSpan.FromMilliseconds(750)
        };

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.PlaceholderSize = (BootstrapPlaceholderSize)(-1)));
        Assert.That(placeholder.PlaceholderSize, Is.EqualTo(BootstrapPlaceholderSize.Small));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.PlaceholderSize = (BootstrapPlaceholderSize)99));
        Assert.That(placeholder.PlaceholderSize, Is.EqualTo(BootstrapPlaceholderSize.Small));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.Animation = (BootstrapPlaceholderAnimation)(-1)));
        Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.Glow));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.Animation = (BootstrapPlaceholderAnimation)99));
        Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.Glow));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.Variant = (BootstrapVariant)(-1)));
        Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Success));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.Variant = (BootstrapVariant)99));
        Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Success));

        Assert.Throws<ArgumentException>((Action)(() => placeholder.CustomColor = Color.FromArgb(128, 12, 34, 56)));
        Assert.That(placeholder.CustomColor, Is.EqualTo(Color.MediumPurple));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.BorderRadius = -2));
        Assert.That(placeholder.BorderRadius, Is.EqualTo(8));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.AnimationDuration = TimeSpan.Zero));
        Assert.That(placeholder.AnimationDuration, Is.EqualTo(TimeSpan.FromMilliseconds(750)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => placeholder.AnimationDuration = TimeSpan.FromTicks(-1)));
        Assert.That(placeholder.AnimationDuration, Is.EqualTo(TimeSpan.FromMilliseconds(750)));
    }

    [Test]
    public void PresentationChangesPreserveCallerOwnedExplicitBounds()
    {
        using var placeholder = new BootstrapPlaceholder
        {
            AutoSize = false,
            Size = new Size(237, 41)
        };
        var expected = placeholder.Size;

        placeholder.PlaceholderSize = BootstrapPlaceholderSize.Large;
        Assert.That(placeholder.Size, Is.EqualTo(expected));
        placeholder.Variant = BootstrapVariant.Danger;
        Assert.That(placeholder.Size, Is.EqualTo(expected));
        placeholder.CustomColor = Color.CornflowerBlue;
        Assert.That(placeholder.Size, Is.EqualTo(expected));
        placeholder.BorderRadius = 11;
        Assert.That(placeholder.Size, Is.EqualTo(expected));
        placeholder.AnimationDuration = TimeSpan.FromSeconds(3);
        Assert.That(placeholder.Size, Is.EqualTo(expected));
    }
}
