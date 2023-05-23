using Mono.Cecil;

namespace RuntimeNullables.Fody.Contexts;

internal class AssemblyContext : NullableContext
{
    public AssemblyContext(AssemblyDefinition assembly, WeavingContext weavingContext) : base(assembly, weavingContext) { }
}