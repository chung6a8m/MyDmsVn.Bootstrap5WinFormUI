using System;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapTextBoxValidationLayerTests
{
    [Test]
    public void TransientOverrideHidesButDoesNotOverwriteLatestApplicationState()
    {
        using var input = new BootstrapTextBox { ValidationState = BootstrapValidationState.Valid };
        input.SetTransientValidationStateOverride(BootstrapValidationState.Invalid);
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.Invalid));

        input.ValidationState = BootstrapValidationState.None;
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.Invalid));

        input.SetTransientValidationStateOverride(null);
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.None));
    }

    [Test]
    public void NoOverrideRetainsExistingValidationSemanticsAndIdempotentClearing()
    {
        using var input = new BootstrapTextBox();
        input.ValidationState = BootstrapValidationState.Valid;
        input.SetTransientValidationStateOverride(null);
        input.SetTransientValidationStateOverride(null);
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.Valid));
    }

    [Test]
    public void InvalidTransientEnumIsRejectedBeforeMutation()
    {
        using var input = new BootstrapTextBox { ValidationState = BootstrapValidationState.Valid };
        Assert.That((Action)(() => input.SetTransientValidationStateOverride((BootstrapValidationState)999)), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.Valid));
    }
}
