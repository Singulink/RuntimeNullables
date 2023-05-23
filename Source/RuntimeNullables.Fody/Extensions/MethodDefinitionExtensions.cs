using Mono.Cecil;
using RuntimeNullables.Fody.Contexts;

namespace RuntimeNullables.Fody.Extensions;

internal static class MethodDefinitionExtensions
{
    public static TypeDefinition? GetIteratorStateMachineType(this MethodDefinition method, WeavingContext weavingContext)
    {
        return method.GetConstructorArgValue<TypeReference>("System.Runtime.CompilerServices.IteratorStateMachineAttribute", weavingContext)?.Resolve();
    }

    public static TypeDefinition? GetAsyncStateMachineType(this MethodDefinition method, WeavingContext weavingContext)
    {
        return method.GetConstructorArgValue<TypeReference>("System.Runtime.CompilerServices.AsyncStateMachineAttribute", weavingContext)?.Resolve();
    }

    public static TypeDefinition? GetAsyncIteratorStateMachineType(this MethodDefinition method, WeavingContext weavingContext)
    {
        return method.GetConstructorArgValue<TypeReference>("System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute", weavingContext)?.Resolve();
    }
}