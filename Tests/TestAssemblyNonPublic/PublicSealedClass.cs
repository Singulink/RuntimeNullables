using RuntimeNullables;

#pragma warning disable CS0628 // New protected member declared in sealed type

namespace TestAssemblyNonPublic;

/// <summary>
/// No other type can derive from this class, so its protected members aren't part of the public API.
/// </summary>
[NullChecks(true)]
public sealed class PublicSealedClass
{
    public static void CallProtectedMethod(string? value) => ProtectedMethod(value!);

    protected static void ProtectedMethod(string value) { }
}
