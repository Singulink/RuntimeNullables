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

    public static IEnumerator GetNullNonGenerics()
    {
        yield return null;
    }
}