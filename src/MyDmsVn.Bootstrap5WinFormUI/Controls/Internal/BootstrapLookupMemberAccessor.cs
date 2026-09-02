using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal static class BootstrapLookupMemberAccessor
{
    private static readonly object Sync = new object();
    private static readonly Dictionary<string, PropertyDescriptor> Cache = new Dictionary<string, PropertyDescriptor>(StringComparer.Ordinal);

    internal static object? GetValue(object item, string memberName)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        if (string.IsNullOrEmpty(memberName)) return item;
        return GetDescriptor(item.GetType(), memberName).GetValue(item);
    }

    internal static void Validate(Type itemType, string memberName)
    {
        if (!string.IsNullOrEmpty(memberName)) GetDescriptor(itemType, memberName);
    }

    private static PropertyDescriptor GetDescriptor(Type itemType, string memberName)
    {
        var key = itemType.AssemblyQualifiedName + "\0" + memberName;
        lock (Sync)
        {
            if (Cache.TryGetValue(key, out var descriptor)) return descriptor;
            descriptor = TypeDescriptor.GetProperties(itemType).Find(memberName, false)
                ?? throw new ArgumentException($"Member '{memberName}' was not found on '{itemType.FullName}'.", nameof(memberName));
            Cache.Add(key, descriptor);
            return descriptor;
        }
    }
}
