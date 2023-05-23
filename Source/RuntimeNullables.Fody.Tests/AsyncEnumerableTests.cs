using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class AsyncEnumerableTests
{
    [TestMethod]
    public async Task GoodGetReferencesAsync()
    {
        await foreach (var value in AsyncEnumerables.GoodGetReferencesAsync()) { }
    }

    [TestMethod]
    public Task BadGetReferencesAsync()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(async () => {
            await foreach (var value in AsyncEnumerables.BadGetReferencesAsync()) { }
        });
    }

    [TestMethod]
    public async Task GoodGetReferencesAsyncWithoutAwait()
    {
        await foreach (var value in AsyncEnumerables.GoodGetReferencesAsyncWithoutAwait()) { }
    }

    [TestMethod]
    public Task BadGetReferencesAsyncWithoutAwait()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(async () => {
            await foreach (var value in AsyncEnumerables.BadGetReferencesAsyncWithoutAwait()) { }
        });
    }

    [TestMethod]
    public async Task GoodGetGenericsAsync()
    {
        await foreach (var value in AsyncEnumerables.GoodGetGenericsAsync<object>()) { }
    }

    [TestMethod]
    public Task BadGetGenericsAsync()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(async () => {
            await foreach (var value in AsyncEnumerables.BadGetGenericsAsync<object>()) { }
        });
    }
}