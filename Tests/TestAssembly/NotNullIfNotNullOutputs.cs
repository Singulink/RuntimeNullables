using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

#pragma warning disable CS8824 // Parameter must have a non-null value when exiting because the named parameter is non-null (bad cases are on purpose)
#pragma warning disable CS8825 // Return value must be non-null because the named parameter is non-null (the bad cases break their contracts on purpose)
#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable IDE0060 // Remove unused parameter

namespace TestAssembly;

/// <summary>
/// Return values and <c>out</c> parameters with <c>[NotNullIfNotNull]</c>, which are checked when one of the named arguments isn't null.
/// </summary>
public static class NotNullIfNotNullOutputs
{
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Echo(string? value) => value;

    /// <summary>
    /// Always returns <see langword="null"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? BadEcho(string? value) => null;

    /// <summary>
    /// Always returns <see langword="null"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(first))]
    [return: NotNullIfNotNull(nameof(second))]
    public static string? BadEchoEither(string? first, string? second) => null;

    /// <summary>
    /// Returns its parameter after setting it to <see langword="null"/>. The caller's argument is checked, not the parameter's value.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? BadEchoThatAssigns(string? value)
    {
        value = null;
        return value;
    }

    /// <summary>
    /// Always returns <see langword="null"/>. The return value is checked since [MaybeNull] lets it be null.
    /// </summary>
    [return: MaybeNull]
    [return: NotNullIfNotNull(nameof(value))]
    public static string BadEchoMaybeNull(string? value) => null!;

    /// <summary>
    /// Always returns <see langword="null"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static object? BadEchoGeneric<T>(T value) => null;

    /// <summary>
    /// Always returns <see langword="null"/>. An empty <see cref="System.Nullable{T}"/> counts as null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(number))]
    public static string? BadNumberToString(int? number) => null;

    /// <summary>
    /// Always returns <see langword="null"/>. The name doesn't match a parameter, so this isn't checked.
    /// </summary>
    [return: NotNullIfNotNull("missing")]
    public static string? BadEchoUnknownName(string? value) => null;

    /// <summary>
    /// Always returns <see langword="null"/>. Only by-value arguments count, so this isn't checked.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? BadEchoRefArgument(ref string? value) => null;

    /// <summary>
    /// The return value of an async method is the task, so this isn't checked.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static async Task<string?> BadEchoAsync(string? value) => null;

    /// <summary>
    /// The return value of an iterator is the enumerable, so this isn't checked.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static IEnumerable<string?> BadEchoIterator(string? value)
    {
        yield return null;
    }

    public static void OutEcho(string? value, [NotNullIfNotNull(nameof(value))] out string? output) => output = value;

    /// <summary>
    /// Always outputs <see langword="null"/>.
    /// </summary>
    public static void BadOutEcho(string? value, [NotNullIfNotNull(nameof(value))] out string? output) => output = null;

    /// <summary>
    /// Always sets the output to <see langword="null"/>.
    /// </summary>
    public static void BadRefEcho(string? value, [NotNullIfNotNull(nameof(value))] ref string? output) => output = null;
}
