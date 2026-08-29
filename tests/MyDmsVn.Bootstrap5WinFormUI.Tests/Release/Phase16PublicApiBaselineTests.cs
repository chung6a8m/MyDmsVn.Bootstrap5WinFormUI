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
    private const string ApprovedV1Fingerprint = "f586623a5061308a6f09e179bfb65a74ad82f68d5fd5e94fe16e84d2a8a48faa";

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
            Assert.That(typeof(BootstrapPopover).GetProperty(nameof(BootstrapPopover.Content))!.PropertyType, Is.EqualTo(typeof(Control)));
            Assert.That(typeof(BootstrapTooltip).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(typeof(BootstrapTooltip).GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Rendering.BootstrapOverlayPlacementEngine"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Rendering.BootstrapOverlayPlacementRequest"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapOverlayDropDown"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapOverlaySurface"));
        }));
    }

    [Test]
    public void AdvancedDropdownApiExportsOnlyTheReviewedPublicContract()
    {
        var assembly = typeof(BootstrapDropdown).Assembly;
        var exportedNames = assembly.GetExportedTypes().Select(type => type.FullName).ToArray();
        var splitProperties = typeof(BootstrapSplitButton)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var splitMethods = typeof(BootstrapSplitButton)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Enum.GetValues(typeof(BootstrapDropdownItemKind)), Is.EqualTo(new[]
            {
                BootstrapDropdownItemKind.Item,
                BootstrapDropdownItemKind.Separator,
                BootstrapDropdownItemKind.HostedControl
            }));
            Assert.That(typeof(BootstrapDropdownItem).GetProperty(nameof(BootstrapDropdownItem.DropDownItems)), Is.Not.Null);
            Assert.That(typeof(BootstrapDropdownItem).GetProperty(nameof(BootstrapDropdownItem.HostedControlFactory)), Is.Not.Null);
            Assert.That(splitProperties, Is.EqualTo(new[]
            {
                "BorderRadius", "ButtonSize", "Icon", "IconPosition", "IconRenderer", "Items",
                "Loading", "LoadingText", "MinimumWidth", "Outline", "Text", "Variant"
            }));
            Assert.That(splitMethods, Is.EqualTo(new[] { "CloseDropDown", "GetPreferredSize", "ShowDropDown" }));
            Assert.That(typeof(BootstrapSplitButton).GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(eventInfo => eventInfo.Name).OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(new[] { "Closed", "Opened" }));
            Assert.That(typeof(BootstrapSplitButton).GetProperty("Font", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Null);
            Assert.That(typeof(BootstrapSplitButton).GetProperty("AccessibleName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Null);
            Assert.That(splitProperties.Any(name => name.IndexOf("Button", StringComparison.Ordinal) >= 0 && name != "ButtonSize"), Is.False);
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapConnectedButtonLayoutLogic"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapDropdownRenderer"));
        }));
    }

    private static string BuildApiSurface(Assembly assembly)
    {
        var lines = new List<string>();

        foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            lines.Add(FormatTypeDeclaration(type));

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (var constructor in type.GetConstructors(flags).Where(IsVisible).OrderBy(FormatConstructor, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatConstructor(constructor));
            }

            foreach (var field in type.GetFields(flags).Where(IsVisible).OrderBy(FormatField, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatField(field));
            }

            foreach (var property in type.GetProperties(flags).Where(IsVisible).OrderBy(FormatProperty, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatProperty(property));
            }

            foreach (var eventInfo in type.GetEvents(flags).Where(IsVisible).OrderBy(FormatEvent, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatEvent(eventInfo));
            }

            foreach (var method in type.GetMethods(flags)
                         .Where(method => !method.IsSpecialName && IsVisible(method))
                         .OrderBy(FormatMethod, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatMethod(method));
            }
        }

        return string.Join("\n", lines);
    }

    private static string FormatTypeDeclaration(Type type)
    {
        if (type.IsEnum)
        {
            return "enum " + FormatType(type) + " : " + FormatType(Enum.GetUnderlyingType(type));
        }

        if (type.IsInterface)
        {
            return "interface " + FormatType(type);
        }

        var kind = type.IsValueType ? "struct " : "class ";
        var baseType = type.BaseType;
        return kind + FormatType(type) + (baseType is null ? string.Empty : " : " + FormatType(baseType));
    }

    private static string FormatConstructor(ConstructorInfo constructor)
    {
        return Visibility(constructor) + " ctor(" + string.Join(",", constructor.GetParameters().Select(FormatParameter)) + ")";
    }

    private static string FormatField(FieldInfo field)
    {
        var constant = field.IsLiteral ? " = " + Convert.ToString(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
        return Visibility(field) + " field " + FormatType(field.FieldType) + " " + field.Name + constant;
    }

    private static string FormatProperty(PropertyInfo property)
    {
        var accessors = property.GetAccessors(nonPublic: true).Where(IsVisible).Select(Visibility).Distinct().OrderBy(value => value, StringComparer.Ordinal);
        var indexParameters = property.GetIndexParameters();
        var index = indexParameters.Length == 0 ? string.Empty : "[" + string.Join(",", indexParameters.Select(FormatParameter)) + "]";
        return string.Join("/", accessors) + " property " + FormatType(property.PropertyType) + " " + property.Name + index;
    }

    private static string FormatEvent(EventInfo eventInfo)
    {
        var methods = new[] { eventInfo.AddMethod, eventInfo.RemoveMethod }.Where(method => method is not null).Cast<MethodInfo>().Where(IsVisible);
        return string.Join("/", methods.Select(Visibility).Distinct().OrderBy(value => value, StringComparer.Ordinal)) +
               " event " + FormatType(eventInfo.EventHandlerType!) + " " + eventInfo.Name;
    }

    private static string FormatMethod(MethodInfo method)
    {
        var genericArity = method.IsGenericMethodDefinition ? "`" + method.GetGenericArguments().Length : string.Empty;
        return Visibility(method) + " method " + FormatType(method.ReturnType) + " " + method.Name + genericArity +
               "(" + string.Join(",", method.GetParameters().Select(FormatParameter)) + ")";
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        var modifier = parameter.IsOut ? "out " : parameter.ParameterType.IsByRef ? "ref " : string.Empty;
        var parameterType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
        var optional = parameter.IsOptional ? " optional" : string.Empty;
        return modifier + FormatType(parameterType) + optional;
    }

    private static string FormatType(Type type)
    {
        if (type.IsByRef)
        {
            return FormatType(type.GetElementType()!) + "&";
        }

        if (type.IsArray)
        {
            return FormatType(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            return "!" + type.GenericParameterPosition;
        }

        if (!type.IsGenericType)
        {
            return (type.FullName ?? type.Name).Replace('+', '.');
        }

        var definition = type.GetGenericTypeDefinition();
        var definitionName = (definition.FullName ?? definition.Name).Replace('+', '.');
        var tickIndex = definitionName.IndexOf('`');
        if (tickIndex >= 0)
        {
            definitionName = definitionName.Substring(0, tickIndex);
        }

        return definitionName + "<" + string.Join(",", type.GetGenericArguments().Select(FormatType)) + ">";
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
        return property.GetAccessors(nonPublic: true).Any(IsVisible);
    }

    private static bool IsVisible(EventInfo eventInfo)
    {
        return (eventInfo.AddMethod is not null && IsVisible(eventInfo.AddMethod)) ||
               (eventInfo.RemoveMethod is not null && IsVisible(eventInfo.RemoveMethod));
    }

    private static string Visibility(MethodBase method)
    {
        if (method.IsPublic)
        {
            return "public";
        }

        return method.IsFamilyOrAssembly ? "protected-internal" : "protected";
    }

    private static string Visibility(FieldInfo field)
    {
        if (field.IsPublic)
        {
            return "public";
        }

        return field.IsFamilyOrAssembly ? "protected-internal" : "protected";
    }

    private static string ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
}
