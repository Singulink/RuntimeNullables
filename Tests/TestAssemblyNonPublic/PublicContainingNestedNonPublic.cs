using RuntimeNullables;

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable CA1801 // Review unused parameters

namespace TestAssemblyNonPublic;

[NullChecks(true)]
public static class PublicContainingNestedNonPublic
{
    internal static class Nested
    {
        public static void Method(string value) { }
    }
}

/// <summary>
/// The nested class is public, but other assemblies can't access it through its internal containing class.
/// </summary>
[NullChecks(true)]
internal static class InternalContainingNestedPublic
{
    public static class Nested
    {
        public static void Method(string value) { }
    }
}
