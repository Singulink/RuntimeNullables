using System.Collections.Generic;

namespace TestAssembly
{
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
    }
}
