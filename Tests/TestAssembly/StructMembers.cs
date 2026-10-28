#pragma warning disable CA1801 // Review unused parameters

namespace TestAssembly;

public struct StructMembers
{
    public StructMembers(string value) { }

    public readonly void Method(string value) { }

    public readonly string BadReturn() => null!;
}
