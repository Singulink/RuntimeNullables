#if NET9_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting (the bad case breaks its contract on purpose)

namespace TestAssembly;

/// <summary>
/// Generic parameters that allow ref structs (C# 13, .NET 9). The checks box the values, which the runtime only allows for ref structs right before
/// a null test.
/// </summary>
public static class RefStructGenerics
{
    public static T Echo<T>(T value) where T : notnull, allows ref struct => value;

    public static T BadReturn<T>() where T : notnull, allows ref struct => default!;

    public static void BadOut<T>(out T value) where T : notnull, allows ref struct => value = default!;

    public static void BadNotNullArgument<T>([NotNull] T? value) where T : allows ref struct { }
}

#endif
