using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using RuntimeNullables;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting (the bad case breaks its contract on purpose)

namespace TestAssemblyRuntimeAsync;

/// <summary>
/// Runtime async methods return their result from the method body, and the runtime puts it (or the exception the method throws) in the task.
/// </summary>
public static class RuntimeAsyncMethods
{
    public static async Task<string> GoodReferenceAsync()
    {
        await Task.Yield();
        return string.Empty;
    }

    public static async Task<string> BadReferenceAsync()
    {
        await Task.Yield();
        return null!;
    }

    public static async Task<string> BadReferenceWithoutAwaitAsync() => null!;

    public static async Task<string?> NullableReferenceAsync()
    {
        await Task.Yield();
        return null;
    }

    public static async Task<int> ValueTypeAsync() => 0;

    public static async ValueTask<string> BadReferenceValueAsync()
    {
        await Task.Yield();
        return null!;
    }

    public static async Task<T> BadGenericAsync<T>() where T : notnull
    {
        await Task.Yield();
        return default!;
    }

    public static async Task<T> GenericUnconstrainedAsync<T>() => default!;

    /// <summary>
    /// Several returns: "bad" returns null, anything else returns itself.
    /// </summary>
    public static async Task<string> SeveralReturnsAsync(string mode)
    {
        await Task.Yield();

        if (mode == "bad")
            return null!;

        if (mode.Length == 0)
            return "empty";

        return mode;
    }

    /// <summary>
    /// Returns from inside a <see langword="try"/> block with a <see langword="finally"/> block that awaits: "bad" returns null, anything else
    /// returns itself.
    /// </summary>
    public static async Task<string> TryFinallyAsync(string mode)
    {
        try
        {
            await Task.Yield();

            if (mode == "bad")
                return null!;
        }
        finally
        {
            await Task.Yield();
        }

        return mode;
    }

    [return: NullChecks(false)]
    public static async Task<string> UncheckedBadReferenceAsync() => null!;

    /// <summary>
    /// The input check runs in the method, so its exception goes in the task like the exception from any other code in the method.
    /// </summary>
    public static async Task EchoAsync(string value) => await Task.Yield();

    /// <summary>
    /// The method's code doesn't all run before it returns to the caller, so the argument isn't checked when it returns.
    /// </summary>
    public static async Task BadThrowIfNullAsync([NotNull] object? value) => await Task.Yield();

    // Runtime async doesn't change these, which still use state machines:

    public static async void AsyncVoid(string value) => await Task.Yield();

    public static IEnumerable<string> BadGetReferences()
    {
        yield return string.Empty;
        yield return null!;
    }

    public static async IAsyncEnumerable<string> BadGetReferencesAsync()
    {
        await Task.Yield();
        yield return null!;
    }
}

public class RuntimeAsyncGeneric<T> where T : notnull
{
    public async Task<T> BadAsync()
    {
        await Task.Yield();
        return default!;
    }
}
