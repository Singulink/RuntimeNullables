using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1801 // Review unused parameters

namespace TestAssembly;

#nullable disable

/// <summary>
/// Types don't say whether values can be null here, but the attributes still count.
/// </summary>
public static class NullableDisabled
{
    public static string NonNull(string x) => x;

    public static void DisallowNullInput([DisallowNull] string value) { }

    public static void BadNotNullOutput([NotNull] out string value) => value = null;
}

#nullable restore

/// <summary>
/// Only the method in the <c>#nullable disable</c> section isn't checked.
/// </summary>
public static class PartlyNullableDisabled
{
    public static void Method(string value) { }

#nullable disable

    public static void NullableDisabledMethod(string value) { }

#nullable restore
}
