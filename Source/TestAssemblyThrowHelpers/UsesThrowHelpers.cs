using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace TestAssemblyThrowHelpers;

public static class UsesThrowHelpers
{
    public static string ReturnParameter(string value) => value;

    public static string ReturnNullableParameterNonNullReturn(string? value) => value!;

    public static async Task<string> ReturnAllowNullParameterNonNullReturnAsync([AllowNull] string value) => value!;
}