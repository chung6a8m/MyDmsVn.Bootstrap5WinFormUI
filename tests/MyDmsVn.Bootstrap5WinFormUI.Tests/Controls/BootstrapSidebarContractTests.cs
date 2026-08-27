using System;
using System.Linq;
using System.Reflection;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSidebarContractTests
{
    [Test]
    public void Phase12PublicContractExists()
    {
        var assembly = typeof(BootstrapButton).Assembly;
        var sidebarType = assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSidebar");
        var itemType = assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSidebarItem");

        Assert.That(sidebarType, Is.Not.Null, "Phase 12 requires BootstrapSidebar.");
        Assert.That(itemType, Is.Not.Null, "Phase 12 requires BootstrapSidebarItem.");

        var sidebarProperties = new[]
        {
            "ExpandedWidth",
            "CollapsedWidth",
            "Expanded",
            "SelectedItem",
            "Items",
            "AnimationDuration",
            "IconRenderer"
        };

        foreach (var propertyName in sidebarProperties)
        {
            Assert.That(sidebarType!.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public), Is.Not.Null, propertyName);
        }

        foreach (var methodName in new[] { "Expand", "Collapse", "Toggle" })
        {
            var method = sidebarType!.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .SingleOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == 0);
            Assert.That(method, Is.Not.Null, methodName);
        }

        var itemProperties = new[]
        {
            "Text",
            "Icon",
            "BadgeText",
            "Enabled",
            "Expanded",
            "Selected",
            "Tag",
            "Items"
        };

        foreach (var propertyName in itemProperties)
        {
            Assert.That(itemType!.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public), Is.Not.Null, propertyName);
        }
    }
}
