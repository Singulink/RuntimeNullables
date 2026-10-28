#pragma warning disable CA1801 // Review unused parameters

namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// The weaver skips types in this namespace (and in <c>RuntimeNullables</c> and <c>System.Runtime.CompilerServices</c>), where projects put copies of
/// the attributes it reads and their own throw helpers.
/// </summary>
public static class CodeAnalysisNamespaceType
{
    public static void Method(string value) { }
}
