using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class ReturnsTests
{
    [TestMethod]
    public void GoodReference() => Returns.GoodReference();

    [TestMethod]
    public void BadReference() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.BadReference());

    [TestMethod]
    public void NullableReference() => Returns.NullableReference();

    [TestMethod]
    public void NoNullChecksBadReference() => Returns.NoNullChecksBadReference();

    [TestMethod]
    public void NullChecksOnNoNullChecksMethodBadReference()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.NullChecksOnNoNullChecksMethodBadReference());
    }

    [TestMethod]
    public void BadRefReference() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.BadRefReference());

    [TestMethod]
    public void BadRefReadonlyReference() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.BadRefReadonlyReference());

    [TestMethod]
    public void BadRefTask() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.BadRefTask());

    [TestMethod]
    public void GoodGenericNotNullConstraint() => Returns.GoodGenericNotNullConstraint<object>();

    [TestMethod]
    public void BadGenericNotNullConstraint()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.BadGenericNotNullConstraint<object>());
    }

    [TestMethod]
    public void GoodGenericNotNullAttribute() => Returns.GoodGenericNotNullAttribute<object>();

    [TestMethod]
    public void BadGenericNotNullAttribute()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.BadGenericNotNullAttribute<object>());
    }

    [TestMethod]
    public void GenericMaybeNullAttribute() => Returns.GenericMaybeNullAttribute<object>();

    [TestMethod]
    public void GenericUnconstrained() => Returns.GenericUnconstrained<object>();

    [TestMethod]
    public void GenericUnconstrainedRef() => Returns.GenericUnconstrainedRef<object>();

    [TestMethod]
    public void BadGenericArray() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.BadGenericArray<object?>());

    [TestMethod]
    public void BadTaskArray() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Returns.BadTaskArray());

    [TestMethod]
    public void Throwing() => Assert.ThrowsException<InvalidOperationException>(() => Returns.Throwing());

    [TestMethod]
    public void ThrowingRefReference() => Assert.ThrowsException<InvalidOperationException>(() => Returns.ThrowingRefReference());
}
