using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace RuntimeNullables.Tests;

/// <summary>
/// The result of a task returned by a non-async method is checked when the method returns the task (if it completed successfully), so the method
/// throws. Async methods put the exception in the task instead.
/// </summary>
[TestClass]
public class TaskResultsTests
{
    [TestMethod]
    public Task GoodReference() => TaskResults.GoodReference();

    [TestMethod]
    public void BadReference() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.TaskResult, () => TaskResults.BadReference());

    [TestMethod]
    public Task GoodGeneric() => TaskResults.GoodGeneric<object>();

    [TestMethod]
    public void BadGeneric() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.TaskResult, () => TaskResults.BadGeneric<object>());

    [TestMethod]
    public Task GoodReferenceAsync() => TaskResults.GoodReferenceAsync();

    [TestMethod]
    public Task BadReferenceAsync() => NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, TaskResults.BadReferenceAsync);

    [TestMethod]
    public Task GoodReferenceAsyncWithoutAwait() => TaskResults.GoodReferenceAsyncWithoutAwait();

    [TestMethod]
    public Task BadReferenceAsyncWithoutAwait()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, TaskResults.BadReferenceAsyncWithoutAwait);
    }

    [TestMethod]
    public Task GoodReferenceValue() => TaskResults.GoodReferenceValue().AsTask();

    [TestMethod]
    public void BadReferenceValue() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.TaskResult, () => TaskResults.BadReferenceValue());

    [TestMethod]
    public Task GoodReferenceValueAsync() => TaskResults.GoodReferenceValueAsync().AsTask();

    [TestMethod]
    public Task BadReferenceValueAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, () => TaskResults.BadReferenceValueAsync().AsTask());
    }

    [TestMethod]
    public Task GoodReferenceValueAsyncWithoutAwait() => TaskResults.GoodReferenceValueAsyncWithoutAwait().AsTask();

    [TestMethod]
    public Task BadReferenceValueAsyncWithoutAwait()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, () => TaskResults.BadReferenceValueAsyncWithoutAwait().AsTask());
    }

    [TestMethod]
    public Task GoodGenericAsync() => TaskResults.GoodGenericAsync<object>();

    [TestMethod]
    public Task BadGenericAsync() => NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, TaskResults.BadGenericAsync<object>);

    [TestMethod]
    public Task BadReferencePoolingValueAsync()
    {
        return NullCheckAssert.ThrowsOutputNullAsync(NullCheckMessages.AsyncTaskResult, () => TaskResults.BadReferencePoolingValueAsync().AsTask());
    }

    [TestMethod]
    public void Throwing() => Assert.ThrowsException<InvalidOperationException>(() => TaskResults.Throwing());

    [TestMethod]
    public void ThrowingValue() => Assert.ThrowsException<InvalidOperationException>(() => TaskResults.ThrowingValue());

    [TestMethod]
    public Task ThrowingAsync() => Assert.ThrowsExceptionAsync<InvalidOperationException>(TaskResults.ThrowingAsync);

    [TestMethod]
    public Task ThrowingAsyncWithoutAwait() => Assert.ThrowsExceptionAsync<InvalidOperationException>(TaskResults.ThrowingAsyncWithoutAwait);

    [TestMethod]
    public Task ThrowingValueAsync() => Assert.ThrowsExceptionAsync<InvalidOperationException>(() => TaskResults.ThrowingValueAsync().AsTask());

    [TestMethod]
    public Task ThrowingPoolingValueAsync()
    {
        return Assert.ThrowsExceptionAsync<InvalidOperationException>(() => TaskResults.ThrowingPoolingValueAsync().AsTask());
    }

    // A check on a pending task would block on its result, so these time out if the check is wrong.

    [TestMethod]
    [Timeout(5000)]
    public async Task Pending()
    {
        var source = new TaskCompletionSource<string>();
        var task = TaskResults.Pending(source);

        source.SetResult(null!);
        Assert.IsNull(await task);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PendingValue()
    {
        var source = new TaskCompletionSource<string>();
        var task = TaskResults.PendingValue(source);

        source.SetResult(null!);
        Assert.IsNull(await task);
    }

    [TestMethod]
    public void Canceled() => Assert.IsTrue(TaskResults.Canceled().IsCanceled);

    [TestMethod]
    public void CanceledValue() => Assert.IsTrue(TaskResults.CanceledValue().IsCanceled);

    [TestMethod]
    public void Faulted() => Assert.IsTrue(TaskResults.Faulted().IsFaulted);

    [TestMethod]
    public void FaultedValue() => Assert.IsTrue(TaskResults.FaultedValue().IsFaulted);

    [TestMethod]
    public void NullTask() => Assert.IsNull(TaskResults.NullTask());

    [TestMethod]
    public async Task NullableReference() => Assert.IsNull(await TaskResults.NullableReference());

    [TestMethod]
    public async Task NullableReferenceAsync() => Assert.IsNull(await TaskResults.NullableReferenceAsync());

    [TestMethod]
    public async Task DefaultValueType() => Assert.AreEqual(false, await TaskResults.DefaultValueType());

    [TestMethod]
    public async Task DefaultNullableValueType() => Assert.AreEqual(null, await TaskResults.DefaultNullableValueType());
}
