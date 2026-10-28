using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class InputParametersTests
{
    [TestMethod]
    public void Reference()
    {
        InputParameters.Reference(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.Reference(null!));
    }

    [TestMethod]
    public void NoNullChecksReference()
    {
        InputParameters.NoNullChecksReference(null!);
    }

    [TestMethod]
    public void NullableReference()
    {
        InputParameters.NullableReference(null);
    }

    [TestMethod]
    public void InReference()
    {
        string value = null!;
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.InReference(in value));
    }

    [TestMethod]
    public void RefReference()
    {
        string value = null!;
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.RefReference(ref value));
    }

    [TestMethod]
    public void ValueArray()
    {
        InputParameters.ValueArray([]);
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.ValueArray(null!));
    }

    [TestMethod]
    public void OutAttributeArray()
    {
        InputParameters.OutAttributeArray([]);
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.OutAttributeArray(null!));
    }

    [TestMethod]
    public void RefValueArray()
    {
        byte[] value = null!;
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.RefValueArray(ref value));
    }

    [TestMethod]
    public void GenericArray()
    {
        // The array isn't nullable, even if its elements are.
        NullCheckAssert.ThrowsArgumentNull("values", () => InputParameters.GenericArray<string?>(null!));
    }

    [TestMethod]
    public void GenericRefArray()
    {
        string?[] values = null!;
        NullCheckAssert.ThrowsArgumentNull("values", () => InputParameters.GenericRefArray(ref values));
    }

    [TestMethod]
    public void GenericUnconstrained()
    {
        InputParameters.GenericUnconstrained<string?>(null);
    }

    [TestMethod]
    public void GenericUnconstrainedIn()
    {
        InputParameters.GenericUnconstrainedIn<string?>(null);
    }

    [TestMethod]
    public void GenericNotNullConstraint()
    {
        InputParameters.GenericNotNullConstraint(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.GenericNotNullConstraint<string>(null!));
    }

    [TestMethod]
    public void GenericDisallowNull()
    {
        InputParameters.GenericDisallowNull(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.GenericDisallowNull<string>(null!));
    }

    [TestMethod]
    public void GenericClassConstraint()
    {
        InputParameters.GenericClassConstraint(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => InputParameters.GenericClassConstraint<string>(null!));
    }

    [TestMethod]
    public void GenericClassConstraintWithAllowNull()
    {
        InputParameters.GenericClassConstraintWithAllowNull<string>(null);
    }

    [TestMethod]
    public void GenericClassConstraintWithNullable()
    {
        InputParameters.GenericClassConstraintWithNullable<string>(null);
    }

    [TestMethod]
    public void GenericNullableClassConstraint()
    {
        InputParameters.GenericNullableClassConstraint<string?>(null);
    }
}
