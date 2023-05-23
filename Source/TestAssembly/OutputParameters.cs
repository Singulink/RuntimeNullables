using System.Diagnostics.CodeAnalysis;

namespace TestAssembly;

public static class OutputParameters
{
    public static void GoodReference(out string value) => value = string.Empty;

    public static void GoodReferenceWithTryFinally(out string value)
    {
        value = string.Empty;

        try {
            value += string.Empty;
        }
        finally {
            value += string.Empty;
        }
    }

    public static void BadReference(out string value) => value = null!;

    public static void NullableReference(out string? value) => value = null;

    public static void GoodGenericNotNullConstraint<T>(out T value) where T : notnull, new() => value = new T();

    public static void BadGenericNotNullConstraint<T>(out T value) where T : notnull, new() => value = default!;

    public static void GoodGenericNotNullAttribute<T>([NotNull] out T value) where T : new() => value = new T();

    public static void BadGenericNotNullAttribute<T>([NotNull] out T value) => value = default!;

    public static void GenericMaybeNullAttribute<T>([MaybeNull] out T value) where T : class => value = null;

    public static void GenericUnconstrained<T>(out T value) => value = default!;
}