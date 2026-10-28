using RuntimeNullables;

namespace TestAssemblyNonPublic;

[NullChecks(true)]
public abstract class PublicGenericBaseClass<T>
{
    public abstract void AbstractGenericMethod(T value, string text);
}

/// <summary>
/// The override's signature has <see cref="int"/> where the base method has <c>T</c>.
/// </summary>
[NullChecks(true)]
internal class InternalGenericSubClass : PublicGenericBaseClass<int>
{
    public override void AbstractGenericMethod(int value, string text) { }
}
