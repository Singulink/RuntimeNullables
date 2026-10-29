using System;
using System.Collections;
using System.Collections.Generic;

namespace TestAssembly;

public static class Enumerators
{
    public static IEnumerator<string> GoodGetReferences()
    {
        yield return string.Empty;
        yield return string.Empty;
        yield return string.Empty;
        yield return string.Empty;
        yield return string.Empty;
    }

    public static IEnumerator<string> BadGetReferences()
    {
        yield return string.Empty;
        yield return null!;
        yield return string.Empty;
    }

    public static IEnumerator<T> GoodGetGenerics<T>() where T : notnull, new()
    {
        yield return new T();
        yield return new T();
        yield return new T();
        yield return new T();
        yield return new T();
    }

    public static IEnumerator<T> BadGetGenerics<T>() where T : notnull, new()
    {
        yield return new T();
        yield return default!;
        yield return new T();
    }

    // Iterators that never yield a value have no values to check.

    public static IEnumerator<string> GetNoReferences()
    {
        yield break;
    }

    public static IEnumerator<string> ThrowingGetReferences()
    {
        throw new InvalidOperationException();

#pragma warning disable CS0162 // Unreachable code detected (an iterator needs a yield statement)
        yield break;
#pragma warning restore CS0162
    }

    public static IEnumerator<string?> GetNullableReferences()
    {
        yield return null;
    }

    public static IEnumerator GetNullNonGenerics()
    {
        yield return null;
    }
}
