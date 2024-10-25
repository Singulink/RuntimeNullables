using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssemblyPointers;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class PointerTests
{
    private readonly int _value = 5;

    [TestMethod]
    public unsafe void PointerIgnored()
    {
        fixed (int* value = &_value) {
            Pointers.Ignored(value);
            Pointers.Ignored(null);
        }
    }
}