using System;
using System.Linq;
using Mono.Cecil;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Creates references to types in an assembly that isn't one of a test assembly's references, so the weaver can't resolve them.
/// </summary>
internal static class MissingAssembly
{
    public const string Name = "MissingAssembly";

    public static TypeReference GetType(ModuleDefinition module, string @namespace, string name, int genericParameterCount = 0)
    {
        var assembly = module.AssemblyReferences.SingleOrDefault(r => r.Name == Name);

        if (assembly == null)
        {
            assembly = new AssemblyNameReference(Name, new Version(1, 0, 0, 0));
            module.AssemblyReferences.Add(assembly);
        }

        var type = new TypeReference(@namespace, name, module, assembly);

        for (int i = 0; i < genericParameterCount; i++)
            type.GenericParameters.Add(new GenericParameter(type));

        return type;
    }
}
