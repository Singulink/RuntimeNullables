using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using RuntimeNullables.Fody.Contexts;

namespace RuntimeNullables.Fody
{
    internal static class AttributeProviderExtensions
    {
        public static bool HasNotNullAttribute(this ICustomAttributeProvider item) => HasAttribute(item, "System.Diagnostics.CodeAnalysis.NotNullAttribute");

        public static bool HasAnyMaybeNullAttribute(this ICustomAttributeProvider item)
        {
            return item.CustomAttributes.Any(a => a.AttributeType.FullName.StartsWith("System.Diagnostics.CodeAnalysis.MaybeNull", StringComparison.Ordinal));
        }

        /// <summary>
        /// Gets a value indicating whether the item has <see cref="GeneratedCodeAttribute"/> or <see cref="CompilerGeneratedAttribute"/> applied to it.
        /// </summary>
        public static bool HasAnyGeneratedAttribute(this ICustomAttributeProvider item)
        {
            return item.CustomAttributes.Select(a => a.AttributeType.FullName).Any(
                n => n == "System.CodeDom.Compiler.GeneratedCodeAttribute" || n == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        }

        public static bool? GetNullChecksAttributeValue(this ICustomAttributeProvider item, WeavingContext weavingContext, bool removeAttribute)
        {
            return GetConstructorArgValue<bool?>(item, "RuntimeNullables.NullChecksAttribute", weavingContext, removeAttribute);
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
        public static T GetConstructorArgValue<T>(this ICustomAttributeProvider item, string attributeFullName, WeavingContext weavingContext, bool removeAttribute = false)
        {
            var attribute = item.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == attributeFullName);

            if (attribute == null)
                return default;

            if (removeAttribute)
                item.CustomAttributes.Remove(attribute);

            if (attribute.ConstructorArguments.Count is var argCount && argCount != 1) {
                weavingContext.WriteError($"Unexpected '{attributeFullName}' constructor argument count.");
                return default;
            }

            object value = attribute.ConstructorArguments[0].Value;

            try {
                return (T)value;
            }
            catch (InvalidCastException) {
                weavingContext.WriteError($"Unexpected '{attributeFullName}' constructor argument type: '{value.GetType()}'.");
                return default;
            }
        }
    }
}
