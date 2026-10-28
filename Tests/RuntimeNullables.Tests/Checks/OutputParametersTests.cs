using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class OutputParametersTests
{
    private static readonly string _valueMessage = NullCheckMessages.OutputParameter("value");

    [TestMethod]
    public void GoodReference() => OutputParameters.GoodReference(out _);

    [TestMethod]
    public void GoodReferenceWithTryFinally() => OutputParameters.GoodReferenceWithTryFinally(out _);

    [TestMethod]
    public void BadReference() => NullCheckAssert.ThrowsOutputNull(_valueMessage, () => OutputParameters.BadReference(out _));

    [TestMethod]
    public void NullableReference() => OutputParameters.NullableReference(out _);

    [TestMethod]
    public void GoodGenericNotNullConstraint() => OutputParameters.GoodGenericNotNullConstraint<object>(out _);

    [TestMethod]
    public void BadGenericNotNullConstraint()
    {
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => OutputParameters.BadGenericNotNullConstraint<object>(out _));
    }

    [TestMethod]
    public void GoodGenericNotNullAttribute() => OutputParameters.GoodGenericNotNullAttribute<object>(out _);

    [TestMethod]
    public void BadGenericNotNullAttribute()
    {
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => OutputParameters.BadGenericNotNullAttribute<object>(out _));
    }

    [TestMethod]
    public void GenericMaybeNullAttribute() => OutputParameters.GenericMaybeNullAttribute<object>(out _);

    [TestMethod]
    public void GenericUnconstrained() => OutputParameters.GenericUnconstrained<object>(out _);
}
