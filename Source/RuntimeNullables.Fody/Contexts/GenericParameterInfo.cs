using Mono.Cecil;

namespace RuntimeNullables.Fody.Contexts
{
    internal struct GenericParameterInfo
    {
        public GenericParameter Parameter { get; }

        public bool Nullable { get; }

        public GenericParameterInfo(GenericParameter parameter, bool nullable)
        {
            Parameter = parameter;
            Nullable = nullable;
        }
    }
}