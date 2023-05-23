using System.Diagnostics.CodeAnalysis;
using RuntimeNullables;

namespace TestAssembly;

public static class Returns
{
    public static string GoodReference() => string.Empty;

    public static string BadReference() => null!;

    public static string? NullableReference() => null;

    [return: NullChecks(false)]
    public static string NoNullChecksBadReference() => null!;

    [NullChecks(false)]
    [return: NullChecks(true)]
    public static string NullChecksOnNoNullChecksMethodBadReference() => null!;

    public static T GoodGenericNotNullConstraint<T>() where T : notnull, new() => new T();

    public static T BadGenericNotNullConstraint<T>() where T : notnull, new() => default!;

    [return: NotNull]
    public static T GoodGenericNotNullAttribute<T>() where T : new() => new T();

    [return: NotNull]
    public static T BadGenericNotNullAttribute<T>() => default!;

    [return: MaybeNull]
    public static T GenericMaybeNullAttribute<T>() where T : class => null;

    public static T GenericUnconstrained<T>() => default!;
}