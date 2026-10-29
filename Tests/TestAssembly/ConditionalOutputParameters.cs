using System;
using System.Diagnostics.CodeAnalysis;
using RuntimeNullables;

#pragma warning disable CS8762 // Parameter must have a non-null value when exiting with a result (the bad cases break their contracts on purpose)

namespace TestAssembly;

/// <summary>
/// <c>out</c> and <c>ref</c> parameters with <c>[MaybeNullWhen]</c> or <c>[NotNullWhen]</c>, which are only checked when the method returns the result
/// that makes them non-null. Each method sets the output to <c>value</c> and returns <c>result</c>.
/// </summary>
public static class ConditionalOutputParameters
{
    public static bool MaybeNullWhenFalse(bool result, string? value, [MaybeNullWhen(false)] out string output)
    {
        output = value!;
        return result;
    }

    public static bool MaybeNullWhenTrue(bool result, string? value, [MaybeNullWhen(true)] out string output)
    {
        output = value!;
        return result;
    }

    public static bool NotNullWhenTrue(bool result, string? value, [NotNullWhen(true)] out string? output)
    {
        output = value;
        return result;
    }

    public static bool NotNullWhenFalse(bool result, string? value, [NotNullWhen(false)] out string? output)
    {
        output = value;
        return result;
    }

    public static bool RefMaybeNullWhenFalse(bool result, string? value, [MaybeNullWhen(false)] ref string output)
    {
        output = value!;
        return result;
    }

    public static bool GenericMaybeNullWhenFalse<T>(bool result, T? value, [MaybeNullWhen(false)] out T output) where T : notnull
    {
        output = value!;
        return result;
    }

    public static bool GenericUnconstrainedNotNullWhenTrue<T>(bool result, T? value, [NotNullWhen(true)] out T? output)
    {
        output = value;
        return result;
    }

    /// <summary>
    /// Same as <see cref="MaybeNullWhenFalse"/>: the conditional attribute overrides the unconditional one for its result.
    /// </summary>
    public static bool MaybeNullAndNotNullWhenTrue(bool result, string? value, [MaybeNull][NotNullWhen(true)] out string output)
    {
        output = value!;
        return result;
    }

    /// <summary>
    /// Same as <see cref="MaybeNullWhenFalse"/>: the conditional attribute overrides the unconditional one for its result.
    /// </summary>
    public static bool NotNullAndMaybeNullWhenFalse(bool result, string? value, [NotNull][MaybeNullWhen(false)] out string? output)
    {
        output = value!;
        return result;
    }

    /// <summary>
    /// The method doesn't return <see langword="bool"/>, so the attribute is ignored and the output is always checked.
    /// </summary>
    public static int NonBoolMaybeNullWhenFalse(string? value, [MaybeNullWhen(false)] out string output)
    {
        output = value!;
        return 0;
    }

    /// <summary>
    /// The method doesn't return <see langword="bool"/>, so the attribute is ignored and the output is never checked.
    /// </summary>
    public static int NonBoolNotNullWhenTrue(string? value, [NotNullWhen(true)] out string? output)
    {
        output = value;
        return 0;
    }

    [NullChecks(false)]
    public static bool UncheckedMaybeNullWhenFalse(bool result, string? value, [MaybeNullWhen(false)] out string output)
    {
        output = value!;
        return result;
    }

    /// <summary>
    /// Every kind of output check in one method. Each output is set to the value given for it.
    /// </summary>
    public static bool Combined(
        bool result,
        [NotNullWhen(true)] string? argument,
        ref string reference,
        string? referenceValue,
        [MaybeNullWhen(false)] out string output,
        string? outputValue,
        string? echoSource,
        [NotNullIfNotNull(nameof(echoSource))] out string? echo,
        string? echoValue)
    {
        reference = referenceValue!;
        output = outputValue!;
        echo = echoValue;
        return result;
    }

    /// <summary>
    /// Several returns, like a typical <c>TryGetValue</c>: "missing" returns false with null, "bad" returns true with null, anything else returns itself.
    /// </summary>
    public static bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        if (key == "missing")
        {
            value = null;
            return false;
        }

        if (key == "bad")
        {
            value = null!;
            return true;
        }

        value = key;
        return true;
    }

    public static bool ThrowingNotNullWhenTrue([NotNullWhen(true)] out string? output) => throw new InvalidOperationException();
}
