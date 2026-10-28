using RuntimeNullables.Weaver.Contexts;

namespace RuntimeNullables.Weaver.Extensions;

internal static class NullableValueExtensions
{
    public static bool ToIsNullable(this NullableValue nullableValue, WeavingContext weavingContext)
    {
        switch (nullableValue)
        {
            case NullableValue.Oblivious:
            case NullableValue.Annotated:
                return true;
            case NullableValue.NotAnnotated:
                return false;
            default:
                weavingContext.WriteError("Unexpected nullable argument value.");
                return true;
        }
    }
}
