using Mono.Cecil;

namespace RuntimeNullables.Fody.Extensions
{
    internal static class ParameterDefinitionExtensions
    {
        // Preconditions:

        public static bool HasAllowNullAttribute(this ParameterDefinition parameter)
        {
            return parameter.HasAttribute("System.Diagnostics.CodeAnalysis.AllowNullAttribute");
        }

        public static bool HasDisallowNullAttribute(this ParameterDefinition parameter)
        {
            return parameter.HasAttribute("System.Diagnostics.CodeAnalysis.DisallowNullAttribute");
        }
    }
}
