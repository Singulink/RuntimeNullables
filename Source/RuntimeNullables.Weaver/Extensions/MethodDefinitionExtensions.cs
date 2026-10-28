using System.Linq;
using Mono.Cecil;
using RuntimeNullables.Weaver.Contexts;

namespace RuntimeNullables.Weaver.Extensions;

internal static class MethodDefinitionExtensions
{
    private const string IteratorStateMachineAttributeFullName = "System.Runtime.CompilerServices.IteratorStateMachineAttribute";
    private const string AsyncStateMachineAttributeFullName = "System.Runtime.CompilerServices.AsyncStateMachineAttribute";
    private const string AsyncIteratorStateMachineAttributeFullName = "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute";
    private const string ExtensionMarkerAttributeFullName = "System.Runtime.CompilerServices.ExtensionMarkerAttribute";
    private const string ExtensionMarkerMethodName = "<Extension>$";

    // MethodImplAttributes.Async (added in .NET 10, used from .NET 11), which Mono.Cecil doesn't define.
    private const MethodImplAttributes RuntimeAsyncImplAttribute = (MethodImplAttributes)0x2000;

    public static TypeDefinition? GetIteratorStateMachineType(this MethodDefinition method, WeavingContext weavingContext)
    {
        return method.GetConstructorArgValue<TypeReference>(IteratorStateMachineAttributeFullName, weavingContext)?.Resolve();
    }

    public static TypeDefinition? GetAsyncStateMachineType(this MethodDefinition method, WeavingContext weavingContext)
    {
        return method.GetConstructorArgValue<TypeReference>(AsyncStateMachineAttributeFullName, weavingContext)?.Resolve();
    }

    public static TypeDefinition? GetAsyncIteratorStateMachineType(this MethodDefinition method, WeavingContext weavingContext)
    {
        return method.GetConstructorArgValue<TypeReference>(AsyncIteratorStateMachineAttributeFullName, weavingContext)?.Resolve();
    }

    /// <summary>
    /// Gets a value indicating whether the method is a runtime async method (.NET 11), which the runtime runs as an async method without a state
    /// machine. Its body returns the task's result rather than the task (nothing for a task without a result), and the runtime stores the result or
    /// any exception it throws in the task it returns to the caller.
    /// </summary>
    public static bool IsRuntimeAsync(this MethodDefinition method) => (method.ImplAttributes & RuntimeAsyncImplAttribute) != 0;

    /// <summary>
    /// Gets a value indicating whether the method is an async method or an iterator, so its code doesn't all run before it returns to the caller.
    /// </summary>
    public static bool IsAsyncOrIterator(this MethodDefinition method)
    {
        return method.IsRuntimeAsync() ||
            method.HasAttribute(AsyncStateMachineAttributeFullName) ||
            method.HasAttribute(IteratorStateMachineAttributeFullName) ||
            method.HasAttribute(AsyncIteratorStateMachineAttributeFullName);
    }

    /// <summary>
    /// Finds the implementation method of a property accessor declared in a C# 14 extension grouping type (see <see
    /// cref="TypeDefinitionExtensions.IsExtensionGroupingType"/>), or returns <see langword="null"/> if there isn't exactly one. It's the static
    /// method in the enclosing class with the same name and the extension block's type parameters, whose parameters are the block's receiver
    /// parameter (for instance properties) followed by the accessor's. The compiler doesn't allow two such methods with the same name and parameter
    /// types.
    /// </summary>
    public static MethodDefinition? FindExtensionAccessorImplementation(this MethodDefinition accessor, WeavingContext weavingContext)
    {
        var groupingType = accessor.DeclaringType;
        TypeReference? receiverType = null;

        if (!accessor.IsStatic)
        {
            // The attribute names the nested marker type whose <Extension>$ method has the receiver parameter. The blocks declared in the same
            // grouping type have the same type of receiver, but the names of their type parameters can differ.

            string? markerTypeName = accessor.GetConstructorArgValue<string>(ExtensionMarkerAttributeFullName, weavingContext);
            var markerType = groupingType.NestedTypes.FirstOrDefault(t => t.Name == markerTypeName);
            receiverType = markerType?.Methods.FirstOrDefault(m => m.Name == ExtensionMarkerMethodName)?.Parameters.FirstOrDefault()?.ParameterType;

            if (receiverType == null)
                return null;
        }

        int parameterCount = accessor.Parameters.Count + (receiverType == null ? 0 : 1);

        var implementations = groupingType.DeclaringType.Methods
            .Where(m => m.IsStatic && m.Name == accessor.Name && m.GenericParameters.Count == groupingType.GenericParameters.Count &&
                m.Parameters.Count == parameterCount && (receiverType == null || m.Parameters[0].ParameterType.FullName == receiverType.FullName))
            .ToList();

        return implementations.Count == 1 ? implementations[0] : null;
    }
}
