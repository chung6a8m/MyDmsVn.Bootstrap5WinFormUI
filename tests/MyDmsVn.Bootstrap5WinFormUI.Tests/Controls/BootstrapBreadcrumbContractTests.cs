using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapBreadcrumbContractTests
{
    [Test]
    public void DefaultsAndDesignerMetadataMatchTheBreadcrumbContract()
    {
        using var breadcrumb = new BootstrapBreadcrumb();
        var defaultEvent = TypeDescriptor.GetDefaultEvent(typeof(BootstrapBreadcrumb));
        var itemsProperty = TypeDescriptor.GetProperties(typeof(BootstrapBreadcrumb))[nameof(BootstrapBreadcrumb.Items)]!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.AutoSize, Is.True);
            Assert.That(breadcrumb.AutoSizeMode, Is.EqualTo(System.Windows.Forms.AutoSizeMode.GrowAndShrink));
            Assert.That(breadcrumb.BackColor, Is.EqualTo(Color.Transparent));
            Assert.That(breadcrumb.TabStop, Is.False);
            Assert.That(breadcrumb.AccessibleRole, Is.EqualTo(System.Windows.Forms.AccessibleRole.Grouping));
            Assert.That(breadcrumb.AccessibleName, Is.EqualTo("Breadcrumb"));
            Assert.That(breadcrumb.AccessibleDescription, Is.EqualTo("Breadcrumb navigation."));
            Assert.That(breadcrumb.Divider, Is.EqualTo("/"));
            Assert.That(breadcrumb.RightToLeftDivider, Is.Null);
            Assert.That(breadcrumb.WrapContents, Is.True);
            Assert.That(breadcrumb.Items, Is.Empty);
            Assert.That(breadcrumb.MaximumSize, Is.EqualTo(Size.Empty));
            Assert.That(defaultEvent?.Name, Is.EqualTo(nameof(BootstrapBreadcrumb.ItemClicked)));
            Assert.That(
                itemsProperty.Attributes[typeof(DesignerSerializationVisibilityAttribute)],
                Is.EqualTo(DesignerSerializationVisibilityAttribute.Content));
        }));
    }

    [Test]
    public void DeclaredPublicSurfaceContainsOnlyThePlannedBreadcrumbMembers()
    {
        var type = typeof(BootstrapBreadcrumb);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();
        var events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(@event => @event.Name)
            .ToArray();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(properties, Is.EqualTo(new[] { "Divider", "Items", "RightToLeftDivider", "WrapContents" }));
            Assert.That(events, Is.EqualTo(new[] { "ItemClicked" }));
            Assert.That(methods, Is.EqualTo(new[] { "GetPreferredSize" }));
            Assert.That(type.GetConstructor(Type.EmptyTypes), Is.Not.Null);
        }));
    }

    [Test]
    public void RoutingSelectionAndStyleApisAreNotDeclared()
    {
        var prohibited = new[]
        {
            "CurrentIndex", "SelectedIndex", "ActiveItem", "NavigateTo", "NavigateBack", "Uri", "Route",
            "Variant", "BorderRadius", "BackgroundColor", "DividerColor", "LinkColor", "CurrentColor", "Icon"
        };
        var declaredNames = typeof(BootstrapBreadcrumb)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .ToArray();

        Assert.That(declaredNames.Intersect(prohibited), Is.Empty);
    }

    [Test]
    public void GetPreferredSizeIncludesCallerPaddingForAnEmptyBreadcrumb()
    {
        using var breadcrumb = new BootstrapBreadcrumb
        {
            Padding = new System.Windows.Forms.Padding(3, 5, 7, 11)
        };

        Assert.That(breadcrumb.GetPreferredSize(Size.Empty), Is.EqualTo(new Size(10, 16)));
    }
}
