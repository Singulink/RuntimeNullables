#if NET11_0_OR_GREATER

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssemblyRuntimeAsync;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace RuntimeNullables.Tests;

/// <summary>
/// Runtime async methods (.NET 11) are checked like async methods with state machines, except that a failed input check also puts its exception in
/// the task instead of throwing it from the call.
/// </summary>
[TestClass]
public class RuntimeAsyncMethodsTests
{
    [TestMethod]
    public Task GoodReferenceAsync() => RuntimeAsyncMethods.GoodReferenceAsync();

    [TestMethod]
    public Task BadReferenceAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, RuntimeAsyncMethods.BadReferenceAsync);
    }

    [TestMethod]
    public Task BadReferenceWithoutAwaitAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, RuntimeAsyncMethods.BadReferenceWithoutAwaitAsync);
    }

    [TestMethod]
    public async Task NullableReferenceAsync() => Assert.IsNull(await RuntimeAsyncMethods.NullableReferenceAsync());

    [TestMethod]
    public async Task ValueTypeAsync() => Assert.AreEqual(0, await RuntimeAsyncMethods.ValueTypeAsync());

    [TestMethod]
    public Task BadReferenceValueAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, () => RuntimeAsyncMethods.BadReferenceValueAsync().AsTask());
    }

    [TestMethod]
    public async Task BadGenericAsync()
    {
        Assert.AreEqual(0, await RuntimeAsyncMethods.BadGenericAsync<int>());
        await NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, RuntimeAsyncMethods.BadGenericAsync<string>);
    }

    [TestMethod]
    public async Task GenericUnconstrainedAsync() => Assert.IsNull(await RuntimeAsyncMethods.GenericUnconstrainedAsync<string?>());

    [TestMethod]
    public async Task SeveralReturnsAsync()
    {
        Assert.AreEqual("empty", await RuntimeAsyncMethods.SeveralReturnsAsync(string.Empty));
        Assert.AreEqual("value", await RuntimeAsyncMethods.SeveralReturnsAsync("value"));
        await NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, () => RuntimeAsyncMethods.SeveralReturnsAsync("bad"));
    }

    [TestMethod]
    public async Task TryFinallyAsync()
    {
        Assert.AreEqual("value", await RuntimeAsyncMethods.TryFinallyAsync("value"));
        await NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, () => RuntimeAsyncMethods.TryFinallyAsync("bad"));
    }

    [TestMethod]
    public async Task UncheckedBadReferenceAsync() => Assert.IsNull(await RuntimeAsyncMethods.UncheckedBadReferenceAsync());

    [TestMethod]
    public async Task EchoAsync()
    {
        await RuntimeAsyncMethods.EchoAsync("value");

        // The call returns a task, which holds the exception.

        var task = RuntimeAsyncMethods.EchoAsync(null!);
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => task);
        Assert.AreEqual("value", exception.ParamName);
    }

    [TestMethod]
    public Task BadThrowIfNullAsync() => RuntimeAsyncMethods.BadThrowIfNullAsync(null);

    [TestMethod]
    public void AsyncVoid()
    {
        // Async void methods still have a state machine, so the input check runs before it starts and throws from the call.
        NullCheckAssert.ThrowsArgumentNull("value", () => RuntimeAsyncMethods.AsyncVoid(null!));
    }

    [TestMethod]
    public void BadGetReferences()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.EnumeratorResult, () => {
            foreach (string value in RuntimeAsyncMethods.BadGetReferences())
                _ = value;
        });
    }

    [TestMethod]
    public Task BadGetReferencesAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncEnumeratorResult, async () => {
            await foreach (string value in RuntimeAsyncMethods.BadGetReferencesAsync())
                _ = value;
        });
    }

    [TestMethod]
    public Task GenericClassBadAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, new RuntimeAsyncGeneric<string>().BadAsync);
    }
}

#endif
