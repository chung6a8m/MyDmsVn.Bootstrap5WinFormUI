using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
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

    [Test]
    public void DesignerMetadataKeepsItemsHiddenAndPublicDefaultsSerializable()
    {
        var groupProperties = TypeDescriptor.GetProperties(typeof(BootstrapListGroup));
        var itemProperties = TypeDescriptor.GetProperties(typeof(BootstrapListGroupItem));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(groupProperties[nameof(BootstrapListGroup.Items)]!.IsBrowsable, Is.False);
            Assert.That(groupProperties[nameof(BootstrapListGroup.Items)]!.SerializationVisibility,
                Is.EqualTo(DesignerSerializationVisibility.Hidden));
            Assert.That(groupProperties[nameof(BootstrapListGroup.Orientation)]!.Attributes[typeof(DefaultValueAttribute)],
                Is.EqualTo(new DefaultValueAttribute(Orientation.Vertical)));
            Assert.That(groupProperties[nameof(BootstrapListGroup.Flush)]!.Attributes[typeof(DefaultValueAttribute)],
                Is.EqualTo(new DefaultValueAttribute(false)));
            Assert.That(groupProperties[nameof(BootstrapListGroup.BorderRadius)]!.Attributes[typeof(DefaultValueAttribute)],
                Is.EqualTo(new DefaultValueAttribute(-1)));
            Assert.That(itemProperties[nameof(BootstrapListGroupItem.Actionable)]!.Attributes[typeof(DefaultValueAttribute)],
                Is.EqualTo(new DefaultValueAttribute(false)));
        }));
    }

    [Test]
    public void PublicSurfaceStaysCompositionFocusedAndRenderingTypesStayInternal()
    {
        var groupProperties = DeclaredPublicPropertyNames(typeof(BootstrapListGroup));
        var groupMethods = DeclaredPublicMethodNames(typeof(BootstrapListGroup));
        var itemProperties = DeclaredPublicPropertyNames(typeof(BootstrapListGroupItem));
        var assembly = typeof(BootstrapListGroup).Assembly;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(groupProperties, Is.EqualTo(new[] { "BorderRadius", "Flush", "Items", "Orientation" }));
            Assert.That(groupMethods, Is.EqualTo(new[] { "AddItem", "AddItem", "ClearItems", "GetPreferredSize", "RemoveItem" }));
            Assert.That(itemProperties, Is.EqualTo(new[] { "Actionable", "Active", "UseThemeFont", "Variant" }));
            Assert.That(typeof(BootstrapListGroup).GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(value => value.Name), Is.EqualTo(new[] { "ItemClick" }));
            Assert.That(typeof(BootstrapListGroupItemEventArgs).IsSealed, Is.True);
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapListGroupRenderLogic"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapListGroupPalette"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapListGroupVisualState"));
        }));
    }

    private static string[] DeclaredPublicPropertyNames(Type type) => type
        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .Select(value => value.Name)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();

    private static string[] DeclaredPublicMethodNames(Type type) => type
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .Where(value => !value.IsSpecialName)
        .Select(value => value.Name)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();
}
