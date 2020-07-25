using System.Collections.Generic;

namespace RuntimeNullables.Fody.Extensions
{
    internal static class ReadOnlyListExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> readOnlyList, T item)
        {
            var comparer = EqualityComparer<T>.Default;

            if (readOnlyList is IList<T> list)
                return list.IndexOf(item);

            for (int i = 0; i < readOnlyList.Count; i++) {
                if (comparer.Equals(readOnlyList[i], item))
                    return i;
            }

            return -1;
        }
    }
}
