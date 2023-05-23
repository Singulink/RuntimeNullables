using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1801 // Review unused parameters

namespace TestAssemblyPointers;

public static class Pointers
{
    public static unsafe int* ReturnSameValueNullableParameter([AllowNull, MaybeNull] int* value) => value;

    public static unsafe void AllowNullOnlyNoOp([AllowNull] int* value) { }

    public static unsafe void NonNullParameter(int* value) { }
}