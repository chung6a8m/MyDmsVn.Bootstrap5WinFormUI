using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Release;

[TestFixture]
public sealed class Phase16PublicApiBaselineTests
{
    private const string ApprovedV1Fingerprint = "29e6fca0440d4696a03471802b5c3b10aa3214b2c6ee2c2a715f58f298f2bd37";

    [Test]
    public void ExportedApiMatchesApprovedV1Baseline()
    {
        var assembly = typeof(BootstrapButton).Assembly;
        var api = BuildApiSurface(assembly);
        var fingerprint = ComputeSha256(api);

        Assert.That(
            fingerprint,
            Is.EqualTo(ApprovedV1Fingerprint),
            "Public API baseline changed. Review the exported API deliberately before updating the approved v1 fingerprint.\n" +
            "Actual fingerprint: " + fingerprint + "\n\n" + api);
    }

    [Test]
    public void V1CompatibilityAssemblyVersionIsStable()
    {
        Assert.That(typeof(BootstrapButton).Assembly.GetName().Version, Is.EqualTo(new Version(1, 0, 0, 0)));
    }

    [Test]
    public void OverlayApiExportsOnlyTheReviewedPublicContract()
    {
        var assembly = typeof(BootstrapPopover).Assembly;
        var exportedNames = assembly.GetExportedTypes().Select(type => type.FullName).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(exportedNames, Does.Contain(typeof(BootstrapOverlayPlacement).FullName));
            Assert.That(exportedNames, Does.Contain(typeof(BootstrapOverlayCollisionBehavior).FullName));
            Assert.That(exportedNames, Does.Contain(typeof(BootstrapTooltipPositioning).FullName));
            Assert.That(exportedNames, Does.Contain(typeof(BootstrapPopoverTrigger).FullName));
            Assert.That(exportedNames, Does.Contain(typeof(BootstrapPopover).FullName));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapOverlaySurface"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapOverlayDropDown"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapOverlayAnchorTracker"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Rendering.BootstrapOverlayPlacementEngine"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Rendering.BootstrapOverlayPlacementRequest"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Rendering.BootstrapOverlayPlacementResult"));
        }));
    }

    private static string BuildApiSurface(Assembly assembly)
    {
        var builder = new StringBuilder();
        var types = assembly.GetExportedTypes()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        foreach (var type in types)
        {
            AppendType(builder, type);
        }

        return builder.ToString();
    }

    private static void AppendType(StringBuilder builder, Type type)
    {
        builder.Append(type.IsInterface ? "interface " : type.IsEnum ? "enum " : type.IsValueType ? "struct " : "class ")
            .Append(type.FullName);

        if (type.BaseType is not null && type.BaseType != typeof(object) && !type.IsEnum)
        {
            builder.Append(" : ").Append(FormatType(type.BaseType));
        }

        builder.AppendLine();

        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(IsVisible)
                     .OrderBy(FormatConstructor, StringComparer.Ordinal))
        {
            builder.Append("  ").AppendLine(FormatConstructor(constructor));
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(IsVisible)
                     .OrderBy(FormatField, StringComparer.Ordinal))
        {
            builder.Append("  ").AppendLine(FormatField(field));
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(IsVisible)
                     .OrderBy(FormatProperty, StringComparer.Ordinal))
        {
            builder.Append("  ").AppendLine(FormatProperty(property));
        }

        foreach (var eventInfo in type.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(IsVisible)
                     .OrderBy(FormatEvent, StringComparer.Ordinal))
        {
            builder.Append("  ").AppendLine(FormatEvent(eventInfo));
        }

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(method => IsVisible(method) && !method.IsSpecialName)
                     .OrderBy(FormatMethod, StringComparer.Ordinal))
        {
            builder.Append("  ").AppendLine(FormatMethod(method));
        }
    }

    private static bool IsVisible(MethodBase method)
    {
        return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
    }

    private static bool IsVisible(FieldInfo field)
    {
        return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
    }

    private static bool IsVisible(PropertyInfo property)
    {
        return (property.GetMethod is not null && IsVisible(property.GetMethod))
            || (property.SetMethod is not null && IsVisible(property.SetMethod));
    }

    private static bool IsVisible(EventInfo eventInfo)
    {
        return eventInfo.AddMethod is not null && IsVisible(eventInfo.AddMethod);
    }

    private static string FormatConstructor(ConstructorInfo constructor)
    {
        return Visibility(constructor) + " ctor(" + string.Join(",", constructor.GetParameters().Select(FormatParameter)) + ")";
    }

    private static string FormatField(FieldInfo field)
    {
        var value = field.IsLiteral && field.GetRawConstantValue() is object constant
            ? " = " + Convert.ToString(constant, System.Globalization.CultureInfo.InvariantCulture)
            : string.Empty;
        return Visibility(field) + " field " + FormatType(field.FieldType) + " " + field.Name + value;
    }

    private static string FormatProperty(PropertyInfo property)
    {
        var accessor = property.GetMethod ?? property.SetMethod!;
        return Visibility(accessor) + " property " + FormatType(property.PropertyType) + " " + property.Name;
    }

    private static string FormatEvent(EventInfo eventInfo)
    {
        return Visibility(eventInfo.AddMethod!) + " event " + FormatType(eventInfo.EventHandlerType!) + " " + eventInfo.Name;
    }

    private static string FormatMethod(MethodInfo method)
    {
        return Visibility(method) + " method " + FormatType(method.ReturnType) + " " + method.Name + "(" + string.Join(",", method.GetParameters().Select(FormatParameter)) + ")";
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        var prefix = parameter.ParameterType.IsByRef ? (parameter.IsOut ? "out " : "ref ") : string.Empty;
        var type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
        return prefix + FormatType(type) + (parameter.IsOptional ? " optional" : string.Empty);
    }

    private static string FormatType(Type type)
    {
        if (type.IsGenericType)
        {
            var definitionName = type.GetGenericTypeDefinition().FullName!;
            var tickIndex = definitionName.IndexOf('`');
            if (tickIndex >= 0)
            {
                definitionName = definitionName.Substring(0, tickIndex);
            }

            return definitionName + "<" + string.Join(",", type.GetGenericArguments().Select(FormatType)) + ">";
        }

        if (type.IsArray)
        {
            return FormatType(type.GetElementType()!) + "[]";
        }

        return type.FullName ?? type.Name;
    }

    private static string Visibility(MethodBase method)
    {
        if (method.IsPublic) return "public";
        if (method.IsFamilyOrAssembly) return "protected internal";
        if (method.IsFamily) return "protected";
        throw new InvalidOperationException("Member is not part of the exported API baseline.");
    }

    private static string Visibility(FieldInfo field)
    {
        if (field.IsPublic) return "public";
        if (field.IsFamilyOrAssembly) return "protected internal";
        if (field.IsFamily) return "protected";
        throw new InvalidOperationException("Member is not part of the exported API baseline.");
    }

    private static string ComputeSha256(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha256.ComputeHash(bytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}
