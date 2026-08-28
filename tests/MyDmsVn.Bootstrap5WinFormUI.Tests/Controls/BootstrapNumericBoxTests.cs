using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapNumericBoxTests
{
    [Test]
    public void DefaultsMatchNativeBackedContract()
    {
        using var input = new BootstrapNumericBox();
        var native = input.Controls.OfType<NumericUpDown>().Single();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Controls.Count, Is.EqualTo(1));
            Assert.That(native.BorderStyle, Is.EqualTo(BorderStyle.None));
            Assert.That(native.TabStop, Is.False);
            Assert.That(input.TabStop, Is.True);
            Assert.That(input.AccessibleRole, Is.EqualTo(AccessibleRole.SpinButton));
            Assert.That(input.AccessibleDescription, Is.EqualTo("Bootstrap-inspired numeric input."));
            Assert.That(input.Value, Is.EqualTo(0m));
            Assert.That(input.Minimum, Is.EqualTo(0m));
            Assert.That(input.Maximum, Is.EqualTo(100m));
            Assert.That(input.Increment, Is.EqualTo(1m));
            Assert.That(input.DecimalPlaces, Is.EqualTo(0));
            Assert.That(input.ThousandsSeparator, Is.False);
            Assert.That(input.ReadOnly, Is.False);
            Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(input.BorderRadius, Is.EqualTo(-1));
        }));

        Assert.That(
            typeof(BootstrapNumericBox).GetCustomAttribute<DefaultPropertyAttribute>()?.Name,
            Is.EqualTo(nameof(BootstrapNumericBox.Value)));
        Assert.That(
            typeof(BootstrapNumericBox).GetCustomAttribute<DefaultEventAttribute>()?.Name,
            Is.EqualTo(nameof(BootstrapNumericBox.ValueChanged)));
    }

    [Test]
    public void PublicDeclaredSurfaceContainsOnlyPlannedMembers()
    {
        var names = typeof(BootstrapNumericBox)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(member =>
                member.MemberType is MemberTypes.Constructor or MemberTypes.Event or MemberTypes.Property ||
                member is MethodInfo method && !method.IsSpecialName)
            .Select(member => member.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(names, Is.EqualTo(new[]
        {
            ".ctor",
            "BorderRadius",
            "DecimalPlaces",
            "Increment",
            "Maximum",
            "Minimum",
            "ReadOnly",
            "ThousandsSeparator",
            "ValidationState",
            "Value",
            "ValueChanged"
        }));
    }

    [Test]
    public void NumericPropertiesForwardDirectlyToOwnedNativeEditor()
    {
        using var input = new BootstrapNumericBox();
        var native = input.Controls.OfType<NumericUpDown>().Single();

        input.Minimum = -100m;
        input.Maximum = 1000m;
        input.Value = 12.5m;
        input.Increment = 0.25m;
        input.DecimalPlaces = 2;
        input.ThousandsSeparator = true;
        input.ReadOnly = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Minimum, Is.EqualTo(native.Minimum));
            Assert.That(input.Maximum, Is.EqualTo(native.Maximum));
            Assert.That(input.Value, Is.EqualTo(native.Value));
            Assert.That(input.Increment, Is.EqualTo(native.Increment));
            Assert.That(input.DecimalPlaces, Is.EqualTo(native.DecimalPlaces));
            Assert.That(input.ThousandsSeparator, Is.EqualTo(native.ThousandsSeparator));
            Assert.That(input.ReadOnly, Is.EqualTo(native.ReadOnly));
            Assert.That(native.Minimum, Is.EqualTo(-100m));
            Assert.That(native.Maximum, Is.EqualTo(1000m));
            Assert.That(native.Value, Is.EqualTo(12.5m));
            Assert.That(native.Increment, Is.EqualTo(0.25m));
            Assert.That(native.DecimalPlaces, Is.EqualTo(2));
            Assert.That(native.ThousandsSeparator, Is.True);
            Assert.That(native.ReadOnly, Is.True);
        }));
    }

    [Test]
    public void NativeRangeNormalizationAndValueExceptionsArePreserved()
    {
        using var input = new BootstrapNumericBox();
        var native = input.Controls.OfType<NumericUpDown>().Single();

        input.Maximum = 10m;
        input.Value = 8m;
        input.Minimum = 9m;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Minimum, Is.EqualTo(native.Minimum));
            Assert.That(input.Maximum, Is.EqualTo(native.Maximum));
            Assert.That(input.Value, Is.EqualTo(native.Value));
        }));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => input.Value = input.Maximum + 1m));
        Assert.That(input.Value, Is.EqualTo(native.Value));
    }

    [Test]
    public void ValidationAndRadiusRejectInvalidValuesBeforeMutation()
    {
        using var input = new BootstrapNumericBox();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => input.ValidationState = (BootstrapValidationState)999));
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.None));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => input.BorderRadius = -2));
        Assert.That(input.BorderRadius, Is.EqualTo(-1));

        input.ValidationState = BootstrapValidationState.Valid;
        input.BorderRadius = 6;
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.Valid));
        Assert.That(input.BorderRadius, Is.EqualTo(6));
    }

    [Test]
    public void ValueChangedUsesOneNativeEventPathAndReportsWrapperSender()
    {
        using var input = new BootstrapNumericBox();
        var native = input.Controls.OfType<NumericUpDown>().Single();
        var count = 0;
        object? lastSender = null;

        input.ValueChanged += (sender, _) =>
        {
            count++;
            lastSender = sender;
        };

        input.Value = 1m;
        input.Value = 1m;
        native.Value = 2m;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(lastSender, Is.SameAs(input));
            Assert.That(input.Value, Is.EqualTo(2m));
        }));
    }
}
