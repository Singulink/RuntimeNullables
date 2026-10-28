using Mono.Cecil;

namespace RuntimeNullables.Weaver.Extensions;

internal static class TypeReferenceExtensions
{
    /// <summary>
    /// Gets the type without its custom modifiers, such as the <c>modreq</c> the compiler adds to <c>in</c> parameters of virtual methods, <c>ref
    /// readonly</c> returns and the return type of <c>init</c> setters.
    /// </summary>
    public static TypeReference WithoutModifiers(this TypeReference type)
    {
        while (type is IModifierType modifierType)
            type = modifierType.ElementType;

        return type;
    }

    /// <summary>
    /// Gets the type of the value that a parameter or return value of this type holds: the type without custom modifiers and, for <c>ref</c>,
    /// <c>out</c> and <c>in</c> types, the type they refer to.
    /// </summary>
    public static TypeReference WithoutRefAndModifiers(this TypeReference type)
    {
        type = type.WithoutModifiers();
        return type is ByReferenceType byReferenceType ? byReferenceType.ElementType.WithoutModifiers() : type;
    }

    /// <summary>
    /// Gets a value indicating whether the value that a parameter or return value of this type holds can be a reference (see <see
    /// cref="WithoutRefAndModifiers"/>).
    /// </summary>
    public static bool IsReferenceType(this TypeReference type)
    {
        type = type.WithoutRefAndModifiers();

        if (type.IsPointer)
            return false;

        if (type.IsArray)
            return true;

        type = type.GetElementType();
        return !(type.IsValueType || (type is GenericParameter param && param.HasNotNullableValueTypeConstraint));
    }

    public static bool IsNullableValueType(this TypeReference type) => GetOpenTypeName(type) == "System.Nullable`1";

    public static bool IsNonGenericTaskType(this TypeReference type)
    {
        return GetOpenTypeName(type) is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask";
    }

    public static bool IsTaskWithResultType(this TypeReference type)
    {
        return GetOpenTypeName(type) is "System.Threading.Tasks.Task`1" or "System.Threading.Tasks.ValueTask`1";
    }

    public static bool IsNonGenericEnumeratorType(this TypeReference type)
    {
        return GetOpenTypeName(type) is "System.Collections.IEnumerable" or "System.Collections.IEnumerator";
    }

    public static bool IsGenericEnumeratorType(this TypeReference type)
    {
        return GetOpenTypeName(type) is "System.Collections.Generic.IEnumerable`1" or "System.Collections.Generic.IEnumerator`1";
    }

    public static bool IsAsyncEnumeratorType(this TypeReference type)
    {
        return GetOpenTypeName(type) is "System.Collections.Generic.IAsyncEnumerable`1" or "System.Collections.Generic.IAsyncEnumerator`1";
    }

    /// <summary>
    /// Gets the full name of the type, or of the generic type definition for generic instances (e.g. <c>System.Threading.Tasks.Task`1</c>). Unlike
    /// <see cref="TypeReference.GetElementType"/>, this doesn't look through arrays or <c>ref</c> types.
    /// </summary>
    private static string GetOpenTypeName(TypeReference type) => (type is GenericInstanceType genericInstance ? genericInstance.ElementType : type).FullName;
}
