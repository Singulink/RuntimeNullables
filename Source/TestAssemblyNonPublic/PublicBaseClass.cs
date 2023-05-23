using RuntimeNullables;

namespace TestAssemblyNonPublic;

[NullChecks(true)]
public abstract class PublicBaseClass
{
    /// <summary>
    /// CheckOutputs is turned off for this project so this should work without throwing an exception.
    /// </summary>
    public static string UncheckedBadReferenceReturn() => null!;

    public abstract void AbstractPublicMethod(string value);

    public virtual void VirtualPublicMethod(string value) { }

    internal abstract void AbstractInternalMethod(string value);

    internal virtual void VirtualInternalMethod(string value) { }
}