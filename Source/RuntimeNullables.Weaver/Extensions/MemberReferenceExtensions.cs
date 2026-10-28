using System;
using Mono.Cecil;

namespace RuntimeNullables.Weaver.Extensions;

internal static class MemberReferenceExtensions
{
    /// <summary>
    /// Resolves the type or throws a <see cref="RuntimeNullablesException"/> that says which type couldn't be resolved.
    /// </summary>
    public static TypeDefinition ResolveOrThrow(this TypeReference type)
    {
        // Generic instances, arrays etc. resolve to the definition of their element type, so name that.

        return Resolve(type.Resolve) ?? throw CreateResolutionException(type.GetElementType().FullName, type.GetElementType());
    }

    /// <summary>
    /// Resolves the method or throws a <see cref="RuntimeNullablesException"/> that says which method couldn't be resolved.
    /// </summary>
    public static MethodDefinition ResolveOrThrow(this MethodReference method)
    {
        return Resolve(method.Resolve) ?? throw CreateResolutionException(method.FullName, method.DeclaringType.GetElementType());
    }

    private static T? Resolve<T>(Func<T> resolve) where T : class
    {
        // The weaver's resolver returns null for assemblies it can't find, but Cecil's default resolver throws.

        try
        {
            return resolve();
        }
        catch (AssemblyResolutionException)
        {
            return null;
        }
    }

    private static RuntimeNullablesException CreateResolutionException(string memberName, TypeReference type)
    {
        // The scope is the assembly the type comes from, or the module for types defined in it.

        return new RuntimeNullablesException(
            $"Could not resolve '{memberName}' from '{type.Scope.Name}'. Make sure all of the project's compile references are available to the weaver.");
    }
}
