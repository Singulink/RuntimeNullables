#pragma warning disable CA1801 // Review unused parameters
namespace TestAssemblyPointers;

public static class Pointers
{
    public static unsafe int* Ignored(int* value) { return value; }
}