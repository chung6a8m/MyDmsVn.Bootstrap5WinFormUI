using System;
using System.Linq;
using System.Reflection;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapPlaceholderContractTests
{
    [Test]
    public void DeclaredPublicSurfaceContainsOnlyReviewedPlaceholderMembers()
    {
        var type = typeof(BootstrapPlaceholder);
        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();
        var methods = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();
        var events = type
            .GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(item => item.Name)
            .ToArray();

        Assert.Multiple((Action)delegate
        {
            Assert.That(properties, Is.EqualTo(new[]
            {
                "Animation",
                "AnimationDuration",
                "BorderRadius",
                "CustomColor",
                "PlaceholderSize",
                "Variant"
            }));
            Assert.That(methods, Is.EqualTo(new[] { "GetPreferredSize" }));
            Assert.That(events, Is.Empty);
        });
    }

    [Test]
    public void ExportedPlaceholderTypesAreExactlyControlAndTwoEnums()
    {
        var exported = typeof(BootstrapPlaceholder).Assembly
            .GetExportedTypes()
            .Where(type => type.Name.IndexOf("Placeholder", StringComparison.Ordinal) >= 0)
            .Select(type => type.FullName)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(exported, Is.EqualTo(new[]
        {
            "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholder",
            "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholderAnimation",
            "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholderSize"
        }));
        Assert.That(exported.Any(name => name!.IndexOf("BootstrapSkeleton", StringComparison.Ordinal) >= 0), Is.False);
        Assert.That(exported.Any(name => name!.IndexOf("PlaceholderRenderLogic", StringComparison.Ordinal) >= 0), Is.False);
    }

    [Test]
    public void DeclaredProtectedSurfaceContainsOnlyLifecycleAndRenderingOverrides()
    {
        var protectedMethods = typeof(BootstrapPlaceholder)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.IsFamily)
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(protectedMethods, Is.EqualTo(new[]
        {
            "Dispose",
            "OnAutoSizeChanged",
            "OnDpiChangedAfterParent",
            "OnEnabledChanged",
            "OnFontChanged",
            "OnHandleCreated",
            "OnHandleDestroyed",
            "OnPaint"
        }));
    }
}
