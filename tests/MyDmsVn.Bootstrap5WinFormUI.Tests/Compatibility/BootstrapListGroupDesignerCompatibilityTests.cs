using System;
using System.ComponentModel;
using System.Globalization;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Compatibility;

[TestFixture]
public sealed class BootstrapListGroupDesignerCompatibilityTests
{
    [Test]
    public void NullableVariantDescriptorAndConverterRoundTripNeutralAndEveryVariant()
    {
        var descriptor = TypeDescriptor.GetProperties(typeof(BootstrapListGroupItem))[nameof(BootstrapListGroupItem.Variant)];
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.PropertyType, Is.EqualTo(typeof(BootstrapVariant?)));

        var converter = descriptor.Converter;
        var values = (BootstrapVariant[])Enum.GetValues(typeof(BootstrapVariant));
        foreach (var value in values)
        {
            var text = converter.ConvertToString(null, CultureInfo.InvariantCulture, value);
            var roundTrip = converter.ConvertFromString(null, CultureInfo.InvariantCulture, text!);
            Assert.That(roundTrip, Is.EqualTo(value), value.ToString());
        }

        using var item = new BootstrapListGroupItem();
        descriptor.SetValue(item, BootstrapVariant.Success);
        Assert.That(descriptor.GetValue(item), Is.EqualTo(BootstrapVariant.Success));
        descriptor.SetValue(item, null);
        Assert.That(descriptor.GetValue(item), Is.Null);
    }
}
