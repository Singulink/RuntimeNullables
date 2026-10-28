using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace RuntimeNullables.Tests;

[TestClass]
public class AsyncEnumeratorsTests
{
    [TestMethod]
    public Task GoodGetReferencesAsync() => EnumerateAsync(AsyncEnumerators.GoodGetReferencesAsync());

    [TestMethod]
    public Task BadGetReferencesAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncEnumeratorResult, () => EnumerateAsync(AsyncEnumerators.BadGetReferencesAsync()));
    }

    [TestMethod]
    public Task GoodGetReferencesAsyncWithoutAwait() => EnumerateAsync(AsyncEnumerators.GoodGetReferencesAsyncWithoutAwait());

    [TestMethod]
    public Task BadGetReferencesAsyncWithoutAwait()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(
            NullCheckMessages.AsyncEnumeratorResult, () => EnumerateAsync(AsyncEnumerators.BadGetReferencesAsyncWithoutAwait()));
    }

    [TestMethod]
    public Task GoodGetGenericsAsync() => EnumerateAsync(AsyncEnumerators.GoodGetGenericsAsync<object>());

    [TestMethod]
    public Task BadGetGenericsAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(
            NullCheckMessages.AsyncEnumeratorResult, () => EnumerateAsync(AsyncEnumerators.BadGetGenericsAsync<object>()));
    }

    [TestMethod]
    public Task GetNullableReferencesAsync() => EnumerateAsync(AsyncEnumerators.GetNullableReferencesAsync());

    private static async Task EnumerateAsync<T>(IAsyncEnumerator<T> enumerator)
    {
        while (await enumerator.MoveNextAsync())
            _ = enumerator.Current;
    }
}
