using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

#pragma warning disable CA1801 // Review unused parameters

namespace TestAssembly;

[GeneratedCode("RuntimeNullables.Tests", "1.0.0")]
public static class GeneratedCode
{
    // Nested type without GeneratedCodeAttribute to ensure it doesn't trigger parent type context building (fixed in v1.0.5)
    public delegate void SomeDelegate(object obj);

    public static void Method(object obj)
    {
    }

    /// <summary>
    /// Types nested in generated types aren't checked either.
    /// </summary>
    public static class Nested
    {
        public static void Method(string value) { }
    }
}

/// <summary>
/// Only the members marked as generated aren't checked.
/// </summary>
public static class PartlyGeneratedCode
{
    /// <summary>
    /// Gets null. Only the property's own attributes count, not its accessors' (the accessors of auto-properties are marked [CompilerGenerated]).
    /// </summary>
    public static string GeneratedAccessorProperty
    {
        [CompilerGenerated]
        get => null!;
    }

    [GeneratedCode("RuntimeNullables.Tests", "1.0.0")]
    public static string GeneratedPropertyWithAttribute => null!;

    public static void Method(string value) { }

    [GeneratedCode("RuntimeNullables.Tests", "1.0.0")]
    public static void GeneratedMethod(string value) { }

    [CompilerGenerated]
    public static void CompilerGeneratedMethod(string value) { }
}
