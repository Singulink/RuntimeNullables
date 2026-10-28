using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using RuntimeNullables;

#pragma warning disable CA1801 // Review unused parameters

namespace TestAssembly;

public static class InputParameters
{
    public static void Reference(string value) { }

    [NullChecks(false)]
    public static void NoNullChecksReference(string value) { }

    public static void NullableReference(string? value) { }

    public static void InReference(in string value) { }

    public static void RefReference(ref string value) { }

    public static void ValueArray(byte[] value) { }

    /// <summary>
    /// [Out] on a by-value array (which the method fills) doesn't make it an out parameter, so the array is still an input.
    /// </summary>
    public static void OutAttributeArray([Out] byte[] value) { }

    public static void RefValueArray(ref byte[] value) { }

    public static void GenericArray<T>(T[] values) { }

    public static void GenericRefArray<T>(ref T[] values) { }

    public static void GenericUnconstrained<T>(T value) { }

    public static void GenericUnconstrainedIn<T>(in T value) { }

    public static void GenericNotNullConstraint<T>(T value) where T : notnull { }

    public static void GenericDisallowNull<T>([DisallowNull] T value) { }

    public static void GenericClassConstraint<T>(T value) where T : class { }

    public static void GenericClassConstraintWithAllowNull<T>([AllowNull] T value) where T : class { }

    public static void GenericClassConstraintWithNullable<T>(T? value) where T : class { }

    public static void GenericNullableClassConstraint<T>(T value) where T : class? { }
}
