using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Icons;

[TestFixture]
public sealed class IconDescriptorDesignerTests
{
    [Test]
    public void AllFactoryDescriptorsRoundTripThroughInstanceDescriptor()
    {
        var descriptors = new[]
        {
            IconDescriptor.SegoeMdl2('\uE10B'),
            IconDescriptor.Svg("<svg viewBox=\"0 0 1 1\"><path d=\"M0 0h1v1H0z\"/></svg>"),
            IconDescriptor.Framework(FrameworkIconGlyph.Check),
            IconDescriptor.External("sample-provider", "sample-icon")
        };
        var converter = TypeDescriptor.GetConverter(typeof(IconDescriptor));

        Assert.That(converter.CanConvertTo(typeof(InstanceDescriptor)), Is.True);

        foreach (var descriptor in descriptors)
        {
            var serialized = converter.ConvertTo(
                context: null,
                culture: CultureInfo.InvariantCulture,
                value: descriptor,
                destinationType: typeof(InstanceDescriptor)) as InstanceDescriptor;

            Assert.That(serialized, Is.Not.Null, $"Missing designer serialization for {descriptor.SourceKind}.");

            var roundTripped = serialized!.Invoke() as IconDescriptor;
            Assert.That(roundTripped, Is.Not.Null);
            Assert.Multiple((Action)(() =>
            {
                Assert.That(roundTripped!.SourceKind, Is.EqualTo(descriptor.SourceKind));
                Assert.That(roundTripped.Value, Is.EqualTo(descriptor.Value));
                Assert.That(roundTripped.SourceId, Is.EqualTo(descriptor.SourceId));
            }));
        }
    }
}
