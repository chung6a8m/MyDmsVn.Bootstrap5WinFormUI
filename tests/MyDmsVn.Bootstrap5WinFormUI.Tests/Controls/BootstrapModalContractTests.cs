using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapModalContractTests
{
    private const string Namespace = "MyDmsVn.Bootstrap5WinFormUI.Controls.";

    [Test]
    public void PublicModalTypesExistWithThePlannedEnumMembers()
    {
        var assembly = typeof(MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapButton).Assembly;
        var modalType = assembly.GetType(Namespace + "BootstrapModal");
        var sizeType = assembly.GetType(Namespace + "BootstrapModalSize");
        var backdropType = assembly.GetType(Namespace + "BootstrapModalBackdropMode");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(modalType, Is.Not.Null);
            Assert.That(modalType?.BaseType, Is.EqualTo(typeof(Form)));
            Assert.That(Enum.GetNames(sizeType!), Is.EqualTo(new[] { "Small", "Default", "Large", "ExtraLarge", "Custom" }));
            Assert.That(Enum.GetNames(backdropType!), Is.EqualTo(new[] { "None", "Dismissible", "Static" }));
        }));
    }

    [Test]
    public void ModalDefaultsAndPublicPropertiesMatchContract()
    {
        var assembly = typeof(MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapButton).Assembly;
        var modalType = assembly.GetType(Namespace + "BootstrapModal", throwOnError: true)!;
        using var modal = (Form)Activator.CreateInstance(modalType)!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(modal.FormBorderStyle, Is.EqualTo(FormBorderStyle.None));
            Assert.That(modal.ShowInTaskbar, Is.False);
            Assert.That(modal.StartPosition, Is.EqualTo(FormStartPosition.Manual));
            Assert.That(Read(modal, "ModalSize")?.ToString(), Is.EqualTo("Default"));
            Assert.That(Read(modal, "BackdropMode")?.ToString(), Is.EqualTo("Dismissible"));
            Assert.That(Read(modal, "CloseOnEscape"), Is.True);
            Assert.That(Read(modal, "ShowCloseButton"), Is.True);
            Assert.That(Read(modal, "BorderRadius"), Is.EqualTo(-1));
            Assert.That(Read(modal, "BodyPanel"), Is.InstanceOf<Panel>());
            Assert.That(Read(modal, "FooterPanel"), Is.InstanceOf<FlowLayoutPanel>());
        }));
    }

    [Test]
    public void ModalDoesNotRedeclareNativeDialogAndSizingMembers()
    {
        var assembly = typeof(MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapButton).Assembly;
        var modalType = assembly.GetType(Namespace + "BootstrapModal", throwOnError: true)!;
        var declared = modalType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .ToArray();

        Assert.That(declared, Does.Not.Contain("ShowDialog"));
        Assert.That(declared, Does.Not.Contain("Close"));
        Assert.That(declared, Does.Not.Contain("Size"));
        Assert.That(declared, Does.Not.Contain("ClientSize"));
        Assert.That(declared, Does.Not.Contain("Owner"));
    }

    private static object? Read(object instance, string propertyName)
    {
        return instance.GetType().GetProperty(propertyName)!.GetValue(instance);
    }
}
