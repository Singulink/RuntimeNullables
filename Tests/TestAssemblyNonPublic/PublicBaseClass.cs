using RuntimeNullables;

namespace TestAssemblyNonPublic;

[NullChecks(true)]
public abstract class PublicBaseClass
{
    protected PublicBaseClass() { }

    protected PublicBaseClass(string value) { }

    /// <summary>
    /// Outputs aren't checked in this project, even with [NullChecks(true)] (the settings take precedence).
    /// </summary>
    public static string UncheckedBadReferenceReturn() => null!;

    [return: NullChecks(true)]
    public static string UncheckedBadReferenceReturnWithNullChecks() => null!;

    /// <summary>
    /// Members outside the public API aren't checked in this project, even with [NullChecks(true)] (the settings take precedence).
    /// </summary>
    public static void CallInternalMethodWithNullChecks(string? value) => InternalMethodWithNullChecks(value!);

    [NullChecks(true)]
    internal static void InternalMethodWithNullChecks(string value) { }

    public abstract void AbstractPublicMethod(string value);

    public virtual void VirtualPublicMethod(string value) { }

    internal abstract void AbstractInternalMethod(string value);

    internal virtual void VirtualInternalMethod(string value) { }

    protected void ProtectedMethod(string value) { }

    protected internal void ProtectedInternalMethod(string value) { }

    private protected void PrivateProtectedMethod(string value) { }

    protected static class ProtectedNested
    {
        public static void Method(string value) { }
    }

    private protected static class PrivateProtectedNested
    {
        public static void Method(string value) { }
    }
}
