using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using RuntimeNullables.Weaver.Extensions;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Determines which methods are part of a module's public API, i.e. which methods code in other assemblies may be able to call.
/// </summary>
internal sealed class PublicApi
{
    private readonly Dictionary<TypeDefinition, List<TypeDefinition>> _derivedTypes = [];

    public PublicApi(ModuleDefinition module)
    {
        foreach (var type in module.GetTypes())
        {
            // Base types defined in the module are type definitions (or generic instances of them), so this doesn't need to resolve anything.

            if (type.BaseType?.GetElementType() is TypeDefinition baseType)
            {
                if (!_derivedTypes.TryGetValue(baseType, out var derivedTypes))
                    _derivedTypes.Add(baseType, derivedTypes = []);

                derivedTypes.Add(type);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether code in other assemblies may be able to call the method.
    /// </summary>
    /// <remarks>
    /// <para>That is the case if the method is public, or protected in an unsealed type, and its type is accessible to other assemblies the same way.
    /// It is also the case if the method overrides or implements such a method, since it can be called through it.</para>
    /// <para>Overridden and implemented methods are matched by name, parameter count and generic parameter count rather than by signature, since
    /// signatures differ when a base type or interface has generic type arguments (e.g. an override in a class that derives from
    /// <c>Base&lt;string&gt;</c>). Matching an extra method only adds checks.</para>
    /// </remarks>
    public bool Contains(MethodDefinition method)
    {
        if (IsAccessible(method) && IsAccessible(method.DeclaringType))
            return true;

        // Explicit overrides (.override), e.g. explicit interface implementations:

        if (method.HasOverrides && method.Overrides.Any(m => Contains(m.ResolveOrThrow())))
            return true;

        // Overrides and implicit interface implementations are always virtual in IL:

        if (!method.IsVirtual)
            return false;

        if (!method.IsNewSlot && GetBaseTypes(method.DeclaringType).Any(t => HasMatchingMethodInPublicApi(t, method)))
            return true;

        // A public method can implement interfaces of its own type, or of a derived type that doesn't implement the method itself:

        return method.IsPublic && GetTypeAndDerivedTypes(method.DeclaringType)
            .SelectMany(t => t.Interfaces)
            .Any(i => HasMatchingMethodInPublicApi(i.InterfaceType.ResolveOrThrow(), method));
    }

    private static bool IsAccessible(MethodDefinition method)
    {
        return method.IsPublic || ((method.IsFamily || method.IsFamilyOrAssembly) && !method.DeclaringType.IsSealed);
    }

    private static bool IsAccessible(TypeDefinition type)
    {
        if (type.DeclaringType == null)
            return type.IsPublic;

        bool isNestedAccessible = type.IsNestedPublic || ((type.IsNestedFamily || type.IsNestedFamilyOrAssembly) && !type.DeclaringType.IsSealed);
        return isNestedAccessible && IsAccessible(type.DeclaringType);
    }

    private static IEnumerable<TypeDefinition> GetBaseTypes(TypeDefinition type)
    {
        for (var baseType = type.BaseType?.ResolveOrThrow(); baseType != null; baseType = baseType.BaseType?.ResolveOrThrow())
            yield return baseType;
    }

    private IEnumerable<TypeDefinition> GetTypeAndDerivedTypes(TypeDefinition type)
    {
        yield return type;

        if (_derivedTypes.TryGetValue(type, out var derivedTypes))
        {
            foreach (var derivedType in derivedTypes.SelectMany(GetTypeAndDerivedTypes))
                yield return derivedType;
        }
    }

    private bool HasMatchingMethodInPublicApi(TypeDefinition type, MethodDefinition method)
    {
        return type.Methods.Any(m =>
            m.IsVirtual &&
            m.Name == method.Name &&
            m.Parameters.Count == method.Parameters.Count &&
            m.GenericParameters.Count == method.GenericParameters.Count &&
            Contains(m));
    }
}
