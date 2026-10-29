using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class EnumeratorsTests
{
    [TestMethod]
    public void GoodGetReferences() => Enumerate(Enumerators.GoodGetReferences());

    [TestMethod]
    public void BadGetReferences()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.EnumeratorResult, () => Enumerate(Enumerators.BadGetReferences()));
    }

    [TestMethod]
    public void GoodGetGenerics() => Enumerate(Enumerators.GoodGetGenerics<object>());

    [TestMethod]
    public void BadGetGenerics()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.EnumeratorResult, () => Enumerate(Enumerators.BadGetGenerics<object>()));
    }

    [TestMethod]
    public void GetNoReferences() => Enumerate(Enumerators.GetNoReferences());

    [TestMethod]
    public void ThrowingGetReferences() => Assert.ThrowsException<InvalidOperationException>(() => Enumerate(Enumerators.ThrowingGetReferences()));

    [TestMethod]
    public void GetNullableReferences() => Enumerate(Enumerators.GetNullableReferences());

    [TestMethod]
    public void GetNullNonGenerics() => Enumerate(Enumerators.GetNullNonGenerics());

    private static void Enumerate(IEnumerator enumerator)
    {
        while (enumerator.MoveNext())
            _ = enumerator.Current;
    }
}
