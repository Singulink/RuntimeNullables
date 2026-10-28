using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace RuntimeNullables.Tests;

[TestClass]
public class AsyncEnumerablesTests
{
    [TestMethod]
    public Task GoodGetReferencesAsync() => EnumerateAsync(AsyncEnumerables.GoodGetReferencesAsync());

    [TestMethod]
    public Task BadGetReferencesAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncEnumeratorResult, () => EnumerateAsync(AsyncEnumerables.BadGetReferencesAsync()));
    }

    [TestMethod]
    public Task GoodGetReferencesAsyncWithoutAwait() => EnumerateAsync(AsyncEnumerables.GoodGetReferencesAsyncWithoutAwait());

    [TestMethod]
    public Task BadGetReferencesAsyncWithoutAwait()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(
            NullCheckMessages.AsyncEnumeratorResult, () => EnumerateAsync(AsyncEnumerables.BadGetReferencesAsyncWithoutAwait()));
    }

    [TestMethod]
    public Task GoodGetGenericsAsync() => EnumerateAsync(AsyncEnumerables.GoodGetGenericsAsync<object>());

    [TestMethod]
    public Task BadGetGenericsAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(
            NullCheckMessages.AsyncEnumeratorResult, () => EnumerateAsync(AsyncEnumerables.BadGetGenericsAsync<object>()));
    }

    [TestMethod]
    public Task GetNullableReferencesAsync() => EnumerateAsync(AsyncEnumerables.GetNullableReferencesAsync());

    private static async Task EnumerateAsync<T>(IAsyncEnumerable<T> values)
    {
        await foreach (var value in values)
            _ = value;
    }
}
