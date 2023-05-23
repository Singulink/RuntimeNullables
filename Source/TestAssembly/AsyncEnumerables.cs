using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace TestAssembly;

public static class AsyncEnumerables
{
    public static async IAsyncEnumerable<string> GoodGetReferencesAsync()
    {
        await Task.Delay(10);
        yield return string.Empty;
    }

    public static async IAsyncEnumerable<string> BadGetReferencesAsync()
    {
        await Task.Delay(10);
        yield return string.Empty;
        yield return null!;
        yield return string.Empty;
    }

    public static async IAsyncEnumerable<string> GoodGetReferencesAsyncWithoutAwait()
    {
        yield return string.Empty;
    }

    public static async IAsyncEnumerable<string> BadGetReferencesAsyncWithoutAwait()
    {
        yield return string.Empty;
        yield return null!;
        yield return string.Empty;
    }

    public static async IAsyncEnumerable<T> GoodGetGenericsAsync<T>() where T : notnull, new()
    {
        await Task.Delay(10);
        yield return new T();
    }

    public static async IAsyncEnumerable<T> BadGetGenericsAsync<T>() where T : notnull, new()
    {
        await Task.Delay(10);
        yield return new T();
        yield return default!;
        yield return new T();
    }
}