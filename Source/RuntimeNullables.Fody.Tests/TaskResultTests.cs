using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class TaskResultTests
{
    [TestMethod]
    public void GoodReference()
    {
        TaskResults.GoodReference();
    }

    [TestMethod]
    public void BadReference()
    {
        Assert.ThrowsException<NullReferenceException>(() => TaskResults.BadReference());
    }

    [TestMethod]
    public void GoodGeneric()
    {
        TaskResults.GoodGeneric<object>();
    }

    [TestMethod]
    public void BadGeneric()
    {
        Assert.ThrowsException<NullReferenceException>(() => TaskResults.BadGeneric<object>());
    }

    [TestMethod]
    public Task GoodReferenceAsync()
    {
        return TaskResults.GoodReferenceAsync();
    }

    [TestMethod]
    public Task BadReferenceAsync()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(() => TaskResults.BadReferenceAsync());
    }

    [TestMethod]
    public Task GoodReferenceAsyncWithoutAwait()
    {
        return TaskResults.GoodReferenceAsyncWithoutAwait();
    }

    [TestMethod]
    public Task BadReferenceAsyncWithoutAwait()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(() => TaskResults.BadReferenceAsyncWithoutAwait());
    }

    [TestMethod]
    public Task GoodReferenceValue()
    {
        return TaskResults.GoodReferenceValue().AsTask();
    }

    [TestMethod]
    public Task BadReferenceValue()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(() => TaskResults.BadReferenceValue().AsTask());
    }

    [TestMethod]
    public Task GoodReferenceValueAsync()
    {
        return TaskResults.GoodReferenceValueAsync().AsTask();
    }

    [TestMethod]
    public Task BadReferenceValueAsync()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(() => TaskResults.BadReferenceValueAsync().AsTask());
    }

    [TestMethod]
    public Task GoodReferenceValueAsyncWithoutAwait()
    {
        return TaskResults.GoodReferenceValueAsyncWithoutAwait().AsTask();
    }

    [TestMethod]
    public Task BadReferenceAsyncValueWithoutAwait()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(() => TaskResults.BadReferenceAsyncValueWithoutAwait().AsTask());
    }

    [TestMethod]
    public Task GoodGenericNotNullConstraintAsync()
    {
        return TaskResults.GoodGenericAsync<object>();
    }

    [TestMethod]
    public Task BadGenericNotNullConstraintAsync()
    {
        return Assert.ThrowsExceptionAsync<NullReferenceException>(() => TaskResults.BadGenericAsync<object>());
    }

    [TestMethod]
    public async Task DefaultValueType()
    {
        Assert.AreEqual(false, await TaskResults.DefaultValueType());
    }

    [TestMethod]
    public async Task DefaultNullableValueType()
    {
        Assert.AreEqual(null, await TaskResults.DefaultNullableValueType());
    }
}