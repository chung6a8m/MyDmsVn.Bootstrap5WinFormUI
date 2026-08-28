using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

internal sealed class IconDescriptorTypeConverter : TypeConverter
{
    private static readonly MethodInfo SegoeMdl2Factory = GetFactory(
        nameof(IconDescriptor.SegoeMdl2),
        typeof(char));
    private static readonly MethodInfo SvgFactory = GetFactory(
        nameof(IconDescriptor.Svg),
        typeof(string));
    private static readonly MethodInfo FrameworkFactory = GetFactory(
        nameof(IconDescriptor.Framework),
        typeof(FrameworkIconGlyph));
    private static readonly MethodInfo ExternalFactory = GetFactory(
        nameof(IconDescriptor.External),
        typeof(string),
        typeof(string));

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType)
    {
        if (destinationType is null)
        {
            throw new ArgumentNullException(nameof(destinationType));
        }

        if (destinationType == typeof(InstanceDescriptor) && value is IconDescriptor descriptor)
        {
            return CreateInstanceDescriptor(descriptor);
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    private static InstanceDescriptor CreateInstanceDescriptor(IconDescriptor descriptor)
    {
        switch (descriptor.SourceKind)
        {
            case IconSourceKind.SegoeMdl2 when descriptor.Value.Length == 1:
                return new InstanceDescriptor(
                    SegoeMdl2Factory,
                    new object[] { descriptor.Value[0] },
                    isComplete: true);

            case IconSourceKind.Svg:
                return new InstanceDescriptor(
                    SvgFactory,
                    new object[] { descriptor.Value },
                    isComplete: true);

            case IconSourceKind.FrameworkVector
                when Enum.TryParse(descriptor.Value, ignoreCase: false, out FrameworkIconGlyph glyph)
                    && Enum.IsDefined(typeof(FrameworkIconGlyph), glyph):
                return new InstanceDescriptor(
                    FrameworkFactory,
                    new object[] { glyph },
                    isComplete: true);

            case IconSourceKind.External when descriptor.SourceId is not null:
                return new InstanceDescriptor(
                    ExternalFactory,
                    new object[] { descriptor.SourceId, descriptor.Value },
                    isComplete: true);

            default:
                throw new NotSupportedException($"Icon descriptor source '{descriptor.SourceKind}' cannot be serialized by the WinForms designer.");
        }
    }

    private static MethodInfo GetFactory(string name, params Type[] parameterTypes)
    {
        return typeof(IconDescriptor).GetMethod(
                   name,
                   BindingFlags.Public | BindingFlags.Static,
                   binder: null,
                   types: parameterTypes,
                   modifiers: null)
               ?? throw new InvalidOperationException($"IconDescriptor factory '{name}' was not found.");
    }
}
