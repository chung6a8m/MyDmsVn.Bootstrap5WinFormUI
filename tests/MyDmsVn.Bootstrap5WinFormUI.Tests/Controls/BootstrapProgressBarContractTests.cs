using System;
using System.Linq;
using System.Reflection;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapProgressBarContractTests
{
    [Test]
    public void Phase11PublicContractExists()
    {
        var assembly = typeof(BootstrapButton).Assembly;
        var type = assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapProgressBar");

        Assert.That(type, Is.Not.Null, "Phase 11 requires BootstrapProgressBar.");

        var expectedProperties = new[]
        {
            "Minimum",
            "Maximum",
            "Value",
            "Percentage",
            "Variant",
            "CustomColor",
            "BorderRadius",
            "ShowText",
            "TextFormat",
            "Striped",
            "Animated",
            "AnimationDuration",
            "Indeterminate"
        };

        foreach (var propertyName in expectedProperties)
        {
            Assert.That(type!.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public), Is.Not.Null, propertyName);
        }

        var animateTo = type!.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(method =>
                method.Name == "AnimateTo" &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(int));

        Assert.That(animateTo, Is.Not.Null, "Phase 11 requires AnimateTo(int).");
    }
}
