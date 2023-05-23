using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class InputParametersTests
{
    [TestMethod]
    public void Reference()
    {
        InputParameters.Reference(string.Empty);
        Assert.ThrowsException<ArgumentNullException>(() => InputParameters.Reference(null!));
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
    public void ValueArray()
    {
        InputParameters.ValueArray(new byte[0]);
        Assert.ThrowsException<ArgumentNullException>(() => InputParameters.ValueArray(null!));
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
        InputParameters.GenericNotNullConstraint<string>(string.Empty);
        Assert.ThrowsException<ArgumentNullException>(() => InputParameters.GenericNotNullConstraint<string>(null!));
    }

    [TestMethod]
    public void GenericDisallowNull()
    {
        InputParameters.GenericDisallowNull<string>(string.Empty);
        Assert.ThrowsException<ArgumentNullException>(() => InputParameters.GenericDisallowNull<string>(null!));
    }

    [TestMethod]
    public void GenericClassConstraint()
    {
        InputParameters.GenericClassConstraint<string>(string.Empty);
        Assert.ThrowsException<ArgumentNullException>(() => InputParameters.GenericClassConstraint<string>(null!));
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
        InputParameters.GenericNullableClassConstraint<string>(null!);
    }
}