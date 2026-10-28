using System.Collections.Generic;
using RuntimeNullables;

#pragma warning disable CA1801 // Review unused parameters

namespace TestAssembly;

/// <summary>
/// C# 14 extension members. Their code is in static implementation methods in this class, which are checked like extension methods. The compiler
/// also declares the members in nested types, with bodies that only throw, which aren't checked.
/// </summary>
public static class ExtensionMembers
{
    extension(string text)
    {
        public string Echo(string value) => value;

        public string BadReturn() => null!;

        public string Property
        {
            get => null!;
            set { }
        }

        // Only the property's declaration has the attribute, not the implementation methods of its accessors.
        [NullChecks(false)]
        public string UncheckedProperty
        {
            get => null!;
            set { }
        }

        public static string StaticProperty
        {
            get => null!;
            set { }
        }
    }

    // The same type of receiver with different nullability, so the compiler declares the members of both blocks in the same nested type.
    extension(string? text)
    {
        public bool IsMissing => string.IsNullOrEmpty(text);
    }

    extension<T>(List<T> list) where T : class
    {
        public T FirstItem => list[0];

        public T? FirstItemOrNull() => list.Count > 0 ? list[0] : null;
    }

    // The same as the block above with a different type parameter name, which the compiler also declares in the same nested type.
    extension<TItem>(List<TItem> items) where TItem : class
    {
        [NullChecks(false)]
        public TItem UncheckedFirstItem => items[0];
    }
}
