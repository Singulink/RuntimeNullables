using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting (the bad cases break their contracts on purpose)
#pragma warning disable IDE0060 // Remove unused parameter

namespace TestAssembly;

/// <summary>
/// <c>[NotNull]</c> and <c>[NotNullWhen]</c> on by-value parameters, which promise that the caller's argument isn't null when the method returns.
/// </summary>
public static class ArgumentPostconditions
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] string? value) => value is null || value.Length == 0;

    /// <summary>
    /// Returns <see langword="false"/> for <see langword="null"/>.
    /// </summary>
    public static bool BadIsNullOrEmpty([NotNullWhen(false)] string? value) => value?.Length == 0;

    public static bool IsNotNullOrEmpty([NotNullWhen(true)] string? value) => value?.Length > 0;

    /// <summary>
    /// Returns <see langword="true"/> for <see langword="null"/>.
    /// </summary>
    public static bool BadIsNotNullOrEmpty([NotNullWhen(true)] string? value) => value is null || value.Length > 0;

    /// <summary>
    /// Several returns: "bad" returns true, null returns true, anything else returns false.
    /// </summary>
    public static bool BadIsValid(string mode, [NotNullWhen(true)] string? value)
    {
        if (mode == "bad")
            return true;

        if (value is null)
            return true;

        return false;
    }

    /// <summary>
    /// For a by-value argument, "may be null" wins: callers can't rely on the argument not being null, so it isn't checked.
    /// </summary>
    public static bool MaybeNullAndNotNullWhenTrue([MaybeNull][NotNullWhen(true)] string? value) => true;

    public static void ThrowIfNull([NotNull] object? value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Doesn't throw.
    /// </summary>
    public static void BadThrowIfNull([NotNull] object? value) { }

    /// <summary>
    /// The caller's argument is checked, not the parameter's value when the method returns.
    /// </summary>
    public static void BadThrowIfNullThatAssigns([NotNull] string? value)
    {
        value = string.Empty;
        _ = value;
    }

    public static void BadGenericThrowIfNull<T>([NotNull] T value) { }

    /// <summary>
    /// The argument can't be null going in, so it isn't checked again.
    /// </summary>
    public static void NotNullArgument([NotNull] string value) { }

    /// <summary>
    /// The body of an async method hasn't run when it returns, so this isn't checked.
    /// </summary>
    public static async Task BadThrowIfNullAsync([NotNull] object? value) => await Task.Yield();

    /// <summary>
    /// The body of an iterator hasn't run when it returns, so this isn't checked.
    /// </summary>
    public static IEnumerable<int> BadThrowIfNullIterator([NotNull] object? value)
    {
        yield break;
    }
}
