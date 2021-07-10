using Mono.Cecil;

namespace RuntimeNullables.Fody.Extensions
{
    internal static class TypeReferenceExtensions
    {
        public static bool IsReferenceType(this TypeReference type)
        {
            if (type.IsPointer || type.IsArray)
                return true;

            type = type.GetElementType();
            return !(type.IsValueType || (type is GenericParameter param && param.HasNotNullableValueTypeConstraint));
        }

        public static bool IsNonGenericTaskType(this TypeReference type)
        {
            return type.GetElementType()?.FullName is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask";
        }

        public static bool IsTaskWithResultType(this TypeReference type)
        {
            return type.GetElementType()?.FullName is "System.Threading.Tasks.Task`1" or "System.Threading.Tasks.ValueTask`1";
        }

        public static bool IsNonGenericEnumeratorType(this TypeReference type)
        {
            return type.GetElementType()?.FullName is "System.Collections.IEnumerable" or "System.Collections.IEnumerator";
        }

        public static bool IsGenericEnumeratorType(this TypeReference type)
        {
            return type.GetElementType()?.FullName is "System.Collections.Generic.IEnumerable`1" or "System.Collections.Generic.IEnumerator`1";
        }

        public static bool IsAsyncEnumeratorType(this TypeReference type)
        {
            return type.GetElementType()?.FullName is "System.Collections.Generic.IAsyncEnumerable`1" or "System.Collections.Generic.IAsyncEnumerator`1";
        }
    }
}
