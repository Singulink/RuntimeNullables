using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using RuntimeNullables.Weaver.Contexts;

namespace RuntimeNullables.Weaver;

internal static class AttributeProviderExtensions
{
    private const string NullChecksAttributeFullName = "RuntimeNullables.NullChecksAttribute";

    private const string CodeAnalysisNamespace = "System.Diagnostics.CodeAnalysis.";

    public static bool HasAllowNullAttribute(this ICustomAttributeProvider item) => HasAttribute(item, CodeAnalysisNamespace + "AllowNullAttribute");

    public static bool HasDisallowNullAttribute(this ICustomAttributeProvider item) => HasAttribute(item, CodeAnalysisNamespace + "DisallowNullAttribute");

    public static bool HasNotNullAttribute(this ICustomAttributeProvider item) => HasAttribute(item, CodeAnalysisNamespace + "NotNullAttribute");

    public static bool HasMaybeNullAttribute(this ICustomAttributeProvider item) => HasAttribute(item, CodeAnalysisNamespace + "MaybeNullAttribute");

    /// <summary>
    /// Gets the return value that <c>[MaybeNullWhen]</c> applies to, or <see langword="null"/> if the item doesn't have the attribute.
    /// </summary>
    public static bool? GetMaybeNullWhenValue(this ICustomAttributeProvider item, WeavingContext weavingContext)
    {
        return GetConstructorArgValue<bool?>(item, CodeAnalysisNamespace + "MaybeNullWhenAttribute", weavingContext);
    }

    /// <summary>
    /// Gets the return value that <c>[NotNullWhen]</c> applies to, or <see langword="null"/> if the item doesn't have the attribute.
    /// </summary>
    public static bool? GetNotNullWhenValue(this ICustomAttributeProvider item, WeavingContext weavingContext)
    {
        return GetConstructorArgValue<bool?>(item, CodeAnalysisNamespace + "NotNullWhenAttribute", weavingContext);
    }

    /// <summary>
    /// Gets the parameter names of the item's <c>[NotNullIfNotNull]</c> attributes.
    /// </summary>
    public static IEnumerable<string> GetNotNullIfNotNullParameterNames(this ICustomAttributeProvider item)
    {
        return item.CustomAttributes
            .Where(a => a.AttributeType.FullName == CodeAnalysisNamespace + "NotNullIfNotNullAttribute" && a.ConstructorArguments.Count == 1)
            .Select(a => a.ConstructorArguments[0].Value)
            .OfType<string>();
    }

    /// <summary>
    /// Gets a value indicating whether the item has <see cref="GeneratedCodeAttribute"/> or <see cref="CompilerGeneratedAttribute"/> applied to it.
    /// </summary>
    public static bool HasAnyGeneratedAttribute(this ICustomAttributeProvider item)
    {
        return item.CustomAttributes.Select(a => a.AttributeType.FullName).Any(
            n => n is "System.CodeDom.Compiler.GeneratedCodeAttribute" or "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
    }

    public static bool? GetNullChecksAttributeValue(this ICustomAttributeProvider item, WeavingContext weavingContext)
    {
        return GetConstructorArgValue<bool?>(item, NullChecksAttributeFullName, weavingContext);
    }

    public static void RemoveNullChecksAttributes(this ICustomAttributeProvider item)
    {
        if (!item.HasCustomAttributes)
            return;

        var attributes = item.CustomAttributes;

        for (int i = attributes.Count - 1; i >= 0; i--)
        {
            if (attributes[i].AttributeType.FullName == NullChecksAttributeFullName)
                attributes.RemoveAt(i);
        }
    }

    public static NullableValue? GetNullableAttributeValue(this ICustomAttributeProvider item, int itemOrdinal, WeavingContext weavingContext)
    {
        object argValue = GetConstructorArgValue<object>(item, "System.Runtime.CompilerServices.NullableAttribute", weavingContext);

        if (argValue == null)
            return null;

        if (argValue is CustomAttributeArgument[] arrayValue)
            argValue = arrayValue[Math.Min(arrayValue.Length - 1, itemOrdinal)].Value;

        if (argValue is byte value)
            return (NullableValue)value;

        weavingContext.WriteError("Unexpected nullable attribute constructor argument type.");
        return null;
    }

    public static NullableValue? GetNullableContextAttributeValue(this ICustomAttributeProvider item, WeavingContext weavingContext)
    {
        return (NullableValue?)GetConstructorArgValue<byte?>(item, "System.Runtime.CompilerServices.NullableContextAttribute", weavingContext);
    }

    public static bool HasAttribute(this ICustomAttributeProvider item, string attributeFullName)
    {
        return item.CustomAttributes.Any(a => a.AttributeType.FullName == attributeFullName);
    }

    [return: MaybeNull]
    public static T GetConstructorArgValue<T>(this ICustomAttributeProvider item, string attributeFullName, WeavingContext weavingContext)
    {
        var attribute = item.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == attributeFullName);

        if (attribute == null)
            return default;

        if (attribute.ConstructorArguments.Count is var argCount && argCount != 1)
        {
            weavingContext.WriteError($"Unexpected '{attributeFullName}' constructor argument count.");
            return default;
        }

        object value = attribute.ConstructorArguments[0].Value;

        try
        {
            return (T)value;
        }
        catch (InvalidCastException)
        {
            weavingContext.WriteError($"Unexpected '{attributeFullName}' constructor argument type: '{value.GetType()}'.");
            return default;
        }
    }
}
