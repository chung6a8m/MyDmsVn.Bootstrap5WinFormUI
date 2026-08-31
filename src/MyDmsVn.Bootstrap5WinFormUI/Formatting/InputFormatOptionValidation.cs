using System;
using System.Collections.Generic;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

internal static class InputFormatOptionValidation
{
    internal static string Normalize(string? value) => value ?? string.Empty;

    internal static void ValidateSingleCharacter(string value, string parameterName, bool allowEmpty = true)
    {
        if ((!allowEmpty && value.Length == 0) || value.Length > 1)
        {
            throw new ArgumentException("The value must contain exactly one character.", parameterName);
        }
    }

    internal static void ValidateEnum<T>(T value, string parameterName) where T : struct
    {
        if (!Enum.IsDefined(typeof(T), value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    internal static void ValidatePattern(string value, string allowed, int maximumComponents, bool rejectYearConflict)
    {
        if (value.Length < 1 || value.Length > maximumComponents)
        {
            throw new ArgumentException("The pattern has an invalid component count.", nameof(value));
        }

        var seen = new HashSet<char>();
        foreach (var component in value)
        {
            if (allowed.IndexOf(component) < 0 || !seen.Add(component))
            {
                throw new ArgumentException("The pattern contains an invalid or duplicate component.", nameof(value));
            }
        }

        if (rejectYearConflict && seen.Contains('y') && seen.Contains('Y'))
        {
            throw new ArgumentException("A date pattern cannot contain both y and Y.", nameof(value));
        }
    }
}
