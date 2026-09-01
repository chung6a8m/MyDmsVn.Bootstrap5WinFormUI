using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapConnectedControlContractTests
{
    [TestCase(typeof(BootstrapButton))]
    [TestCase(typeof(BootstrapTextBox))]
    [TestCase(typeof(BootstrapNumericBox))]
    [TestCase(typeof(BootstrapSelect))]
    [TestCase(typeof(BootstrapSplitButton))]
    public void SupportedPrimitiveImplementsConnectedContractExplicitly(Type controlType)
    {
        Assert.That(typeof(IBootstrapConnectedControl).IsAssignableFrom(controlType), Is.True);
        var publicNames = controlType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .ToArray();
        Assert.That(publicNames, Does.Not.Contain("ConnectedCornerRadius"));
        Assert.That(publicNames, Does.Not.Contain("ConnectedSizeOverride"));
        Assert.That(publicNames, Does.Not.Contain("GetConnectedSafeMinimumHeight"));
    }

    [Test]
    public void ButtonConnectedOverridesDoNotMutatePublicPresentationState()
    {
        using var button = new BootstrapButton
        {
            ButtonSize = BootstrapButtonSize.Large,
            BorderRadius = 13
        };
        var connected = (IBootstrapConnectedControl)button;

        connected.ConnectedCornerRadius = CornerRadius.Empty;
        connected.ConnectedSizeOverride = BootstrapConnectedControlSize.Small;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.ButtonSize, Is.EqualTo(BootstrapButtonSize.Large));
            Assert.That(button.BorderRadius, Is.EqualTo(13));
            Assert.That(button.GetEffectiveCornerRadius(MyDmsVn.Bootstrap5WinFormUI.Theme.BootstrapThemeManager.CurrentTheme.Metrics), Is.EqualTo(CornerRadius.Empty));
            Assert.That(connected.GetConnectedSafeMinimumHeight(BootstrapConnectedControlSize.Small, 96), Is.Positive);
        }));
    }

    [Test]
    public void ConnectedInfrastructureIsNotExported()
    {
        var exported = typeof(BootstrapButton).Assembly.GetExportedTypes();
        Assert.That(exported, Does.Not.Contain(typeof(IBootstrapConnectedControl)));
        Assert.That(exported, Does.Not.Contain(typeof(BootstrapConnectedControlSize)));
    }

    [Test]
    public void ConnectedSelectRejectsMultipleModeBeforeMutation()
    {
        using var select = new BootstrapSelect();
        var connected = (IBootstrapConnectedControl)select;
        connected.ConnectedSizeOverride = BootstrapConnectedControlSize.Default;

        Assert.Throws<NotSupportedException>((Action)(() => select.SelectionMode = BootstrapSelectMode.Multiple));
        Assert.That(select.SelectionMode, Is.EqualTo(BootstrapSelectMode.Single));

        connected.ConnectedSizeOverride = null;
        select.SelectionMode = BootstrapSelectMode.Multiple;
        Assert.That(select.SelectionMode, Is.EqualTo(BootstrapSelectMode.Multiple));
    }

    [Test]
    public void ConnectedNumericBoxDoesNotFightParentAssignedHeight()
    {
        using var numeric = new BootstrapNumericBox();
        var connected = (IBootstrapConnectedControl)numeric;
        connected.ConnectedSizeOverride = BootstrapConnectedControlSize.Small;
        numeric.Height = 10;

        numeric.PerformLayout();

        Assert.That(numeric.Height, Is.EqualTo(10));
        Assert.That(connected.GetConnectedSafeMinimumHeight(BootstrapConnectedControlSize.Small, 96), Is.GreaterThan(10));
    }
}
