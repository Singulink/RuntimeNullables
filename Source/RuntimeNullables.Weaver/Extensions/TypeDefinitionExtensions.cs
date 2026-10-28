using System;
using Mono.Cecil;

namespace RuntimeNullables.Weaver.Extensions;

internal static class TypeDefinitionExtensions
{
    /// <summary>
    /// Gets a value indicating whether the type is a grouping type of C# 14 extension blocks. The compiler declares the blocks' members in it (and
    /// their receiver parameters in nested marker types), with bodies that only throw. The members' code is in implementation methods in the
    /// enclosing static class, which also have the attributes of the members, except for the attributes of properties (see <see
    /// cref="MethodDefinitionExtensions.FindExtensionAccessorImplementation"/>).
    /// </summary>
    public static bool IsExtensionGroupingType(this TypeDefinition type)
    {
        return type.IsNested && type.IsSpecialName && type.Name.StartsWith("<G>$", StringComparison.Ordinal);
    }
}
