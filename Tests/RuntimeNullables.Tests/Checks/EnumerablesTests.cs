using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class EnumerablesTests
{
    [TestMethod]
    public void GoodGetReferences() => Enumerate(Enumerables.GoodGetReferences());

    [TestMethod]
    public void BadGetReferences()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.EnumeratorResult, () => Enumerate(Enumerables.BadGetReferences()));
    }

    [TestMethod]
    public void GoodGetGenerics() => Enumerate(Enumerables.GoodGetGenerics<object>());

    [TestMethod]
    public void BadGetGenerics()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.EnumeratorResult, () => Enumerate(Enumerables.BadGetGenerics<object>()));
    }

    [TestMethod]
    public void GetNullableReferences() => Enumerate(Enumerables.GetNullableReferences());

    [TestMethod]
    public void GetNullNonGenerics() => Enumerate(Enumerables.GetNullNonGenerics());

    private static void Enumerate(IEnumerable values)
    {
        foreach (object? value in values)
            _ = value;
    }
}
