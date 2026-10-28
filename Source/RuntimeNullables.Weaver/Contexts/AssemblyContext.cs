using Mono.Cecil;

namespace RuntimeNullables.Weaver.Contexts;

internal class AssemblyContext : NullableContext
{
    public AssemblyContext(AssemblyDefinition assembly, WeavingContext weavingContext) : base(assembly, weavingContext) { }
}
