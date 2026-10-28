using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssemblyPointers;

namespace RuntimeNullables.Tests;

[TestClass]
public class PointersTests
{
    private readonly int _value = 5;

    [TestMethod]
    public unsafe void Ignored()
    {
        fixed (int* value = &_value)
        {
            Pointers.Ignored(value);
            Pointers.Ignored(null);
        }
    }

    [TestMethod]
    public unsafe void FunctionPointer() => Pointers.FunctionPointer(null);
}
