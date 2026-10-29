using System;
using System.Collections;
using System.Collections.Generic;

namespace TestAssembly;

public static class Enumerables
{
    public static IEnumerable<string> GoodGetReferences()
    {
        yield return string.Empty;
        yield return string.Empty;
        yield return string.Empty;
        yield return string.Empty;
        yield return string.Empty;
    }

    public static IEnumerable<string> BadGetReferences()
    {
        yield return string.Empty;
        yield return null!;
        yield return string.Empty;
    }

    public static IEnumerable<T> GoodGetGenerics<T>() where T : notnull, new()
    {
        yield return new T();
        yield return new T();
        yield return new T();
        yield return new T();
        yield return new T();
    }

    public static IEnumerable<T> BadGetGenerics<T>() where T : notnull, new()
    {
        yield return new T();
        yield return default!;
        yield return new T();
    }

    // Iterators that never yield a value have no values to check.

    public static IEnumerable<string> GetNoReferences()
    {
        yield break;
    }

    public static IEnumerable<string> ThrowingGetReferences()
    {
        throw new InvalidOperationException();

#pragma warning disable CS0162 // Unreachable code detected (an iterator needs a yield statement)
        yield break;
#pragma warning restore CS0162
    }

    public static IEnumerable<string?> GetNullableReferences()
    {
        yield return null;
    }

    public static IEnumerable GetNullNonGenerics()
    {
        yield return null;
    }
}
