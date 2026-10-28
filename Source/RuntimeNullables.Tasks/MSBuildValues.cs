using System;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeNullables.Tasks;

/// <summary>
/// Reads property values passed to tasks as strings the way MSBuild reads them.
/// </summary>
internal static class MSBuildValues
{
    // The values that MSBuild conditions (e.g. '$(RuntimeNullablesEnabled)' != 'false') and bool task parameters treat as true or false.
    private static readonly string[] TrueValues = ["true", "on", "yes", "!false", "!off", "!no"];
    private static readonly string[] FalseValues = ["false", "off", "no", "!true", "!on", "!yes"];

    /// <summary>
    /// Splits a list separated by semicolons or commas (like <c>$(DefineConstants)</c> and <c>$(NoWarn)</c>), without empty entries.
    /// </summary>
    public static IEnumerable<string> SplitList(string? value)
    {
        return (value ?? string.Empty).Split([';', ','], StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).Where(c => c.Length > 0);
    }

    /// <summary>
    /// Parses a boolean the way MSBuild does: <c>true</c>, <c>on</c> or <c>yes</c>, <c>false</c>, <c>off</c> or <c>no</c>, or any of them negated
    /// with <c>!</c>, in any case.
    /// </summary>
    public static bool TryParseBoolean(string value, out bool result)
    {
        result = TrueValues.Contains(value, StringComparer.OrdinalIgnoreCase);
        return result || FalseValues.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}
