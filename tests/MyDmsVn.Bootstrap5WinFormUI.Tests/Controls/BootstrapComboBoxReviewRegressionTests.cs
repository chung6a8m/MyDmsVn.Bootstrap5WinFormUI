using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapComboBoxReviewRegressionTests
{
    [Test]
    public void ConstructorDoesNotCreateNativeHandle()
    {
        using var comboBox = new BootstrapComboBox();

        Assert.That(comboBox.IsHandleCreated, Is.False,
            "Construction should remain handle-free so WinForms can establish parent/DPI/thread-affine state when the control is actually hosted.");
    }

    [Test]
    public void LeadingIconSupportsDesignerInstanceSerialization()
    {
        var property = TypeDescriptor.GetProperties(typeof(BootstrapComboBox))[nameof(BootstrapComboBox.LeadingIcon)];
        Assert.That(property, Is.Not.Null);

        var descriptor = IconDescriptor.Framework(FrameworkIconGlyph.Check);
        var converter = property!.Converter;

        Assert.That(converter.CanConvertTo(typeof(InstanceDescriptor)), Is.True,
            "The WinForms designer needs an InstanceDescriptor path to serialize a non-null LeadingIcon.");

        var serialized = converter.ConvertTo(descriptor, typeof(InstanceDescriptor)) as InstanceDescriptor;
        Assert.That(serialized, Is.Not.Null);

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
