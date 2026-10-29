using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using RuntimeNullables;

namespace TestAssembly;

public static class Returns
{
    private static string _nullString = null!;
    private static Task<string> _nullTask = null!;

    public static string GoodReference() => string.Empty;

    public static string BadReference() => null!;

    public static string? NullableReference() => null;

    [return: NullChecks(false)]
    public static string NoNullChecksBadReference() => null!;

    [NullChecks(false)]
    [return: NullChecks(true)]
    public static string NullChecksOnNoNullChecksMethodBadReference() => null!;

    public static ref string BadRefReference() => ref _nullString;

    public static ref readonly string BadRefReadonlyReference() => ref _nullString;

    /// <summary>
    /// The task itself is checked, but not its result (only tasks returned by value are).
    /// </summary>
    public static ref Task<string> BadRefTask() => ref _nullTask;

    public static T GoodGenericNotNullConstraint<T>() where T : notnull, new() => new T();

    public static T BadGenericNotNullConstraint<T>() where T : notnull, new() => default!;

    [return: NotNull]
    public static T GoodGenericNotNullAttribute<T>() where T : new() => new T();

    [return: NotNull]
    public static T BadGenericNotNullAttribute<T>() => default!;

    [return: MaybeNull]
    public static T GenericMaybeNullAttribute<T>() where T : class => null;

    public static T GenericUnconstrained<T>() => default!;

    public static ref T GenericUnconstrainedRef<T>() => ref Holder<T>.Value;

    public static T[] BadGenericArray<T>() => null!;

    /// <summary>
    /// The array is checked like any other reference (the elements' task results aren't checked).
    /// </summary>
    public static Task<string>[] BadTaskArray() => null!;

    // Methods that always throw have no return value to check.

    public static string Throwing() => throw new InvalidOperationException();

    public static ref string ThrowingRefReference() => throw new InvalidOperationException();

    private static class Holder<T>
    {
        public static T Value = default!;
    }
}
