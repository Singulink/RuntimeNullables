using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

/// <summary>
/// Each method sets its output to the given value and returns the given result.
/// </summary>
[TestClass]
public class ConditionalOutputParametersTests
{
    private static readonly string _outputMessage = NullCheckMessages.OutputParameter("output");

    [TestMethod]
    public void MaybeNullWhenFalse()
    {
        ConditionalOutputParameters.MaybeNullWhenFalse(true, string.Empty, out _);
        ConditionalOutputParameters.MaybeNullWhenFalse(false, null, out _);
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.MaybeNullWhenFalse(true, null, out _));
    }

    [TestMethod]
    public void MaybeNullWhenTrue()
    {
        ConditionalOutputParameters.MaybeNullWhenTrue(false, string.Empty, out _);
        ConditionalOutputParameters.MaybeNullWhenTrue(true, null, out _);
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.MaybeNullWhenTrue(false, null, out _));
    }

    [TestMethod]
    public void NotNullWhenTrue()
    {
        ConditionalOutputParameters.NotNullWhenTrue(true, string.Empty, out _);
        ConditionalOutputParameters.NotNullWhenTrue(false, null, out _);
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.NotNullWhenTrue(true, null, out _));
    }

    [TestMethod]
    public void NotNullWhenFalse()
    {
        ConditionalOutputParameters.NotNullWhenFalse(false, string.Empty, out _);
        ConditionalOutputParameters.NotNullWhenFalse(true, null, out _);
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.NotNullWhenFalse(false, null, out _));
    }

    [TestMethod]
    public void RefMaybeNullWhenFalse()
    {
        string output = string.Empty;
        ConditionalOutputParameters.RefMaybeNullWhenFalse(false, null, ref output);

        output = string.Empty;
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.RefMaybeNullWhenFalse(true, null, ref output));
    }

    [TestMethod]
    public void GenericMaybeNullWhenFalse()
    {
        ConditionalOutputParameters.GenericMaybeNullWhenFalse<string>(false, null, out _);
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.GenericMaybeNullWhenFalse<string>(true, null, out _));
    }

    [TestMethod]
    public void GenericUnconstrainedNotNullWhenTrue()
    {
        ConditionalOutputParameters.GenericUnconstrainedNotNullWhenTrue<string>(false, null, out _);
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.GenericUnconstrainedNotNullWhenTrue<string>(true, null, out _));
    }

    [TestMethod]
    public void MaybeNullAndNotNullWhenTrue()
    {
        ConditionalOutputParameters.MaybeNullAndNotNullWhenTrue(false, null, out _);
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.MaybeNullAndNotNullWhenTrue(true, null, out _));
    }

    [TestMethod]
    public void NotNullAndMaybeNullWhenFalse()
    {
        ConditionalOutputParameters.NotNullAndMaybeNullWhenFalse(false, null, out _);
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.NotNullAndMaybeNullWhenFalse(true, null, out _));
    }

    [TestMethod]
    public void NonBoolMaybeNullWhenFalse()
    {
        NullCheckAssert.ThrowsOutputNull(_outputMessage, () => ConditionalOutputParameters.NonBoolMaybeNullWhenFalse(null, out _));
    }

    [TestMethod]
    public void NonBoolNotNullWhenTrue() => ConditionalOutputParameters.NonBoolNotNullWhenTrue(null, out _);

    [TestMethod]
    public void UncheckedMaybeNullWhenFalse() => ConditionalOutputParameters.UncheckedMaybeNullWhenFalse(true, null, out _);

    [TestMethod]
    public void Combined()
    {
        // Each output is set to the value given for it.

        string reference = "reference";
        Assert.IsTrue(ConditionalOutputParameters.Combined(true, "argument", ref reference, "reference", out _, "output", "source", out _, "echo"));
        Assert.IsFalse(ConditionalOutputParameters.Combined(false, null, ref reference, "reference", out _, null, null, out _, null));

        NullCheckAssert.ThrowsOutputNull(
            NullCheckMessages.Parameter("argument"),
            () => ConditionalOutputParameters.Combined(true, null, ref reference, "reference", out _, "output", null, out _, null));

        NullCheckAssert.ThrowsOutputNull(
            NullCheckMessages.OutputParameter("reference"),
            () => ConditionalOutputParameters.Combined(false, null, ref reference, null, out _, null, null, out _, null));

        reference = "reference";
        NullCheckAssert.ThrowsOutputNull(
            _outputMessage, () => ConditionalOutputParameters.Combined(true, "argument", ref reference, "reference", out _, null, null, out _, null));

        NullCheckAssert.ThrowsOutputNull(
            NullCheckMessages.OutputParameter("echo"),
            () => ConditionalOutputParameters.Combined(false, null, ref reference, "reference", out _, null, "source", out _, null));
    }

    [TestMethod]
    public void TryGetValue()
    {
        Assert.IsTrue(ConditionalOutputParameters.TryGetValue("key", out string? value));
        Assert.AreEqual("key", value);

        Assert.IsFalse(ConditionalOutputParameters.TryGetValue("missing", out value));
        Assert.IsNull(value);

        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.OutputParameter("value"), () => ConditionalOutputParameters.TryGetValue("bad", out _));
    }
}
