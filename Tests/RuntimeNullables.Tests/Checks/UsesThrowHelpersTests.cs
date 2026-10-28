using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssemblyThrowHelpers;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace RuntimeNullables.Tests;

/// <summary>
/// TestAssemblyThrowHelpers has its own throw helpers, which use different exception types and pass the parameter name or message on as the message.
/// </summary>
[TestClass]
public class UsesThrowHelpersTests
{
    [TestMethod]
    public void ReturnParameter()
    {
        var exception = Assert.ThrowsException<InvalidOperationException>(() => UsesThrowHelpers.ReturnParameter(null!));
        Assert.AreEqual("value", exception.Message);
    }

    [TestMethod]
    public void ReturnNullableParameterNonNullReturn()
    {
        var exception = Assert.ThrowsException<InvalidDataException>(() => UsesThrowHelpers.ReturnNullableParameterNonNullReturn(null));
        Assert.AreEqual(NullCheckMessages.ReturnValue, exception.Message);
    }

    [TestMethod]
    public async Task ReturnAllowNullParameterNonNullReturnAsync()
    {
        var task = UsesThrowHelpers.ReturnAllowNullParameterNonNullReturnAsync(null);

        var exception = await Assert.ThrowsExceptionAsync<InvalidConstraintException>(() => task);
        Assert.AreEqual(NullCheckMessages.AsyncTaskResult, exception.Message);
    }

    [TestMethod]
    public async Task BadGetReferencesAsync()
    {
        var exception = await Assert.ThrowsExceptionAsync<InvalidConstraintException>(async () => {
            await foreach (string value in UsesThrowHelpers.BadGetReferencesAsync())
                _ = value;
        });

        Assert.AreEqual(NullCheckMessages.AsyncEnumeratorResult, exception.Message);
    }
}
