using System;
using System.Collections.Generic;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSelectTests
{
    [Test]
    public void DefaultsMatchApprovedContract()
    {
        using var select = new BootstrapSelect();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select, Is.InstanceOf<System.Windows.Forms.UserControl>());
            Assert.That(select.SelectionMode, Is.EqualTo(BootstrapSelectMode.Single));
            Assert.That(select.AllowClear, Is.True);
            Assert.That(select.AllowCustomValues, Is.False);
            Assert.That(select.SearchEnabled, Is.True);
            Assert.That(select.MinimumSearchLength, Is.EqualTo(0));
            Assert.That(select.SearchDebounce, Is.EqualTo(TimeSpan.FromMilliseconds(250)));
            Assert.That(select.PageSize, Is.EqualTo(20));
            Assert.That(select.DropDownWidth, Is.EqualTo(0));
            Assert.That(select.MaxDropDownHeight, Is.EqualTo(320));
            Assert.That(select.MaximumSelectionRows, Is.EqualTo(3));
            Assert.That(select.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(select.BorderRadius, Is.EqualTo(-1));
            Assert.That(select.Matcher, Is.TypeOf<BootstrapSelectTextMatcher>());
            Assert.That(select.Renderer, Is.TypeOf<BootstrapSelectRenderer>());
            Assert.That(select.CloseOnSelect, Is.True);
            Assert.That(select.Items, Is.Not.Null);
            Assert.That(select.SelectedItems, Is.Empty);
        }));
    }

    [Test]
    public void InvalidConfigurationIsRejected()
    {
        using var select = new BootstrapSelect();

        Assert.That((Action)(() => select.MinimumSearchLength = -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => select.SearchDebounce = TimeSpan.FromMilliseconds(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => select.PageSize = 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => select.DropDownWidth = -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => select.MaxDropDownHeight = 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => select.MaximumSelectionRows = 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => select.BorderRadius = -2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => select.Matcher = null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That((Action)(() => select.Renderer = null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That((Action)(() => select.ValueComparer = null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ModeSensitiveCloseOnSelectDefaultSurvivesUntilExplicitlyOverridden()
    {
        using var select = new BootstrapSelect();
        select.SelectionMode = BootstrapSelectMode.Multiple;
        Assert.That(select.CloseOnSelect, Is.False);

        select.CloseOnSelect = true;
        select.SelectionMode = BootstrapSelectMode.Single;
        select.SelectionMode = BootstrapSelectMode.Multiple;
        Assert.That(select.CloseOnSelect, Is.True);
    }
}
