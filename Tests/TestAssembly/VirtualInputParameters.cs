#pragma warning disable CA1801 // Review unused parameters

namespace TestAssembly;

/// <summary>
/// The compiler marks <see langword="in"/> parameters of virtual methods with a custom modifier.
/// </summary>
public class VirtualInputParameters
{
    public virtual void InReference(in string value) { }

    public virtual void GenericInReference<T>(in T value) where T : notnull { }
}
