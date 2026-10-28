namespace TestAssemblySigned;

/// <summary>
/// This project uses the default settings, so the internal method and the return value are only checked in Debug builds.
/// </summary>
public static class Signed
{
    public static string Echo(string value) => value;

    public static void CallInternalMethod(string? value) => InternalMethod(value!);

    public static string BadReturn() => null!;

    internal static void InternalMethod(string value) { }
}
