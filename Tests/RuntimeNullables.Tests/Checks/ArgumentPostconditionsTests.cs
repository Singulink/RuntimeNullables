using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace RuntimeNullables.Tests;

[TestClass]
public class ArgumentPostconditionsTests
{
    private static readonly string _valueMessage = NullCheckMessages.Parameter("value");

    [TestMethod]
    public void IsNullOrEmpty()
    {
        Assert.IsTrue(ArgumentPostconditions.IsNullOrEmpty(null));
        Assert.IsTrue(ArgumentPostconditions.IsNullOrEmpty(string.Empty));
        Assert.IsFalse(ArgumentPostconditions.IsNullOrEmpty("value"));
    }

    [TestMethod]
    public void BadIsNullOrEmpty()
    {
        Assert.IsTrue(ArgumentPostconditions.BadIsNullOrEmpty(string.Empty));
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => ArgumentPostconditions.BadIsNullOrEmpty(null));
    }

    [TestMethod]
    public void IsNotNullOrEmpty()
    {
        Assert.IsFalse(ArgumentPostconditions.IsNotNullOrEmpty(null));
        Assert.IsTrue(ArgumentPostconditions.IsNotNullOrEmpty("value"));
    }

    [TestMethod]
    public void BadIsNotNullOrEmpty()
    {
        Assert.IsTrue(ArgumentPostconditions.BadIsNotNullOrEmpty("value"));
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => ArgumentPostconditions.BadIsNotNullOrEmpty(null));
    }

    [TestMethod]
    public void BadIsValid()
    {
        Assert.IsTrue(ArgumentPostconditions.BadIsValid("bad", "value"));
        Assert.IsFalse(ArgumentPostconditions.BadIsValid("good", "value"));
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => ArgumentPostconditions.BadIsValid("bad", null));
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => ArgumentPostconditions.BadIsValid("good", null));
    }

    [TestMethod]
    public void MaybeNullAndNotNullWhenTrue() => Assert.IsTrue(ArgumentPostconditions.MaybeNullAndNotNullWhenTrue(null));

    [TestMethod]
    public void ThrowIfNull()
    {
        ArgumentPostconditions.ThrowIfNull(new object());
        Assert.ThrowsException<ArgumentNullException>(() => ArgumentPostconditions.ThrowIfNull(null));
    }

    [TestMethod]
    public void BadThrowIfNull()
    {
        ArgumentPostconditions.BadThrowIfNull(new object());
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => ArgumentPostconditions.BadThrowIfNull(null));
    }

    [TestMethod]
    public void BadThrowIfNullThatAssigns()
    {
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => ArgumentPostconditions.BadThrowIfNullThatAssigns(null));
    }

    [TestMethod]
    public void BadGenericThrowIfNull()
    {
        ArgumentPostconditions.BadGenericThrowIfNull(0);
        NullCheckAssert.ThrowsOutputNull(_valueMessage, () => ArgumentPostconditions.BadGenericThrowIfNull<string?>(null));
    }

    [TestMethod]
    public void NotNullArgument() => NullCheckAssert.ThrowsArgumentNull("value", () => ArgumentPostconditions.NotNullArgument(null!));

    [TestMethod]
    public Task BadThrowIfNullAsync() => ArgumentPostconditions.BadThrowIfNullAsync(null);

    [TestMethod]
    public void BadThrowIfNullIterator()
    {
        foreach (int value in ArgumentPostconditions.BadThrowIfNullIterator(null))
            _ = value;
    }
}
