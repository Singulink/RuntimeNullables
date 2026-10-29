using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace TestAssembly;

public static class TaskResults
{
    public static Task<string> GoodReference()
    {
        return Task.FromResult<string>(string.Empty);
    }

    public static Task<string> BadReference()
    {
        return Task.FromResult<string>(null!);
    }

    public static Task<T> GoodGeneric<T>() where T : notnull, new()
    {
        return Task.FromResult<T>(new T());
    }

    public static Task<T> BadGeneric<T>() where T : notnull
    {
        return Task.FromResult<T>(default!);
    }

    public static async Task<string> GoodReferenceAsync()
    {
        await Task.Delay(10);
        return string.Empty;
    }

    public static async Task<string> BadReferenceAsync()
    {
        await Task.Delay(10);
        return null!;
    }

    public static async Task<string> GoodReferenceAsyncWithoutAwait()
    {
        return string.Empty;
    }

    public static async Task<string> BadReferenceAsyncWithoutAwait()
    {
        return null!;
    }

    public static ValueTask<string> GoodReferenceValue()
    {
        return new ValueTask<string>(string.Empty);
    }

    public static ValueTask<string> BadReferenceValue()
    {
        return new ValueTask<string>((string)null!);
    }

    public static async ValueTask<string> GoodReferenceValueAsync()
    {
        await Task.Delay(10);
        return string.Empty;
    }

    public static async ValueTask<string> BadReferenceValueAsync()
    {
        await Task.Delay(100);
        return null!;
    }

    public static async ValueTask<string> GoodReferenceValueAsyncWithoutAwait()
    {
        return string.Empty;
    }

    public static async ValueTask<string> BadReferenceValueAsyncWithoutAwait()
    {
        return null!;
    }

    public static async Task<T> GoodGenericAsync<T>() where T : notnull, new()
    {
        await Task.Delay(10);
        return new T();
    }

    public static async Task<T> BadGenericAsync<T>() where T : notnull
    {
        await Task.Delay(10);
        return default!;
    }

    /// <summary>
    /// A builder other than the default one.
    /// </summary>
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public static async ValueTask<string> BadReferencePoolingValueAsync()
    {
        await Task.Delay(10);
        return null!;
    }

    // Methods that always throw have no result to check.

    public static Task<string> Throwing() => throw new InvalidOperationException();

    public static ValueTask<string> ThrowingValue() => throw new InvalidOperationException();

    public static async Task<string> ThrowingAsync()
    {
        await Task.Delay(10);
        throw new InvalidOperationException();
    }

    public static async Task<string> ThrowingAsyncWithoutAwait()
    {
        throw new InvalidOperationException();
    }

    public static async ValueTask<string> ThrowingValueAsync()
    {
        await Task.Delay(10);
        throw new InvalidOperationException();
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public static async ValueTask<string> ThrowingPoolingValueAsync()
    {
        await Task.Delay(10);
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Tasks that are returned before they complete aren't checked.
    /// </summary>
    public static Task<string> Pending(TaskCompletionSource<string> source)
    {
        return source.Task;
    }

    public static Task<string> Canceled()
    {
        return Task.FromCanceled<string>(new CancellationToken(canceled: true));
    }

    public static Task<string> Faulted()
    {
        return Task.FromException<string>(new InvalidOperationException());
    }

    public static ValueTask<string> PendingValue(TaskCompletionSource<string> source)
    {
        return new ValueTask<string>(source.Task);
    }

    public static ValueTask<string> CanceledValue()
    {
        return new ValueTask<string>(Task.FromCanceled<string>(new CancellationToken(canceled: true)));
    }

    public static ValueTask<string> FaultedValue()
    {
        return new ValueTask<string>(Task.FromException<string>(new InvalidOperationException()));
    }

    /// <summary>
    /// The task itself may be null, so there's no result to check.
    /// </summary>
    public static Task<string>? NullTask()
    {
#pragma warning disable RCS1210 // Return completed task instead of returning null (a null task is what this tests)
        return null;
#pragma warning restore RCS1210
    }

    public static Task<string?> NullableReference()
    {
        return Task.FromResult<string?>(null);
    }

    public static async Task<string?> NullableReferenceAsync()
    {
        await Task.Delay(10);
        return null;
    }

    public static Task<bool> DefaultValueType()
    {
        return Task.FromResult(false);
    }

    public static Task<bool?> DefaultNullableValueType()
    {
        return Task.FromResult((bool?)null);
    }
}
