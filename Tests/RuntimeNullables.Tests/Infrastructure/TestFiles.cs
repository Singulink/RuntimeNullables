using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Locations of the test assemblies and the files needed to weave them in tests.
/// </summary>
internal static class TestFiles
{
    /// <summary>
    /// Gets the path of the strong name key the signed test assembly is signed with.
    /// </summary>
    public static string KeyFilePath => Path.Combine(AppContext.BaseDirectory, "key.snk");

    /// <summary>
    /// Gets the path of a woven test assembly in the test output directory.
    /// </summary>
    public static string GetWovenAssemblyPath(string assemblyName) => Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll");

    /// <summary>
    /// Gets the path of a test assembly as the compiler produced it, before weaving. Its PDB is next to it.
    /// </summary>
    public static string GetUnwovenAssemblyPath(string assemblyName) => Path.Combine(AppContext.BaseDirectory, "Unwoven", assemblyName + ".dll");

    /// <summary>
    /// Gets the paths of the references a test assembly was compiled against.
    /// </summary>
    public static IEnumerable<string> GetReferencePaths(string assemblyName)
    {
        return File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Unwoven", assemblyName + ".references.txt")).Where(p => p.Length > 0);
    }

    /// <summary>
    /// Reads an unwoven test assembly into memory, with an assembly resolver (disposed with the module) that finds the references the assembly was
    /// compiled against.
    /// </summary>
    public static ModuleDefinition ReadUnwovenModule(string assemblyName)
    {
        var module = ModuleDefinition.ReadModule(GetUnwovenAssemblyPath(assemblyName), new ReaderParameters { InMemory = true });

        // The module creates (and owns) a default resolver. Only search the reference directories, like the weaver's own resolver.

        var resolver = (DefaultAssemblyResolver)module.AssemblyResolver;

        foreach (string directory in resolver.GetSearchDirectories())
            resolver.RemoveSearchDirectory(directory);

        foreach (string directory in GetReferencePaths(assemblyName).Select(p => Path.GetDirectoryName(p)!).Distinct(StringComparer.OrdinalIgnoreCase))
            resolver.AddSearchDirectory(directory);

        return module;
    }

    /// <summary>
    /// Writes a copy of an unwoven test assembly (without its debug symbols) with the changes that <paramref name="modify"/> makes.
    /// </summary>
    public static void WriteModifiedCopy(string assemblyName, string path, Action<ModuleDefinition> modify)
    {
        using var module = ModuleDefinition.ReadModule(GetUnwovenAssemblyPath(assemblyName), new ReaderParameters { InMemory = true });

        modify(module);
        module.Write(path);
    }

    /// <summary>
    /// Counts the calls to throw helper methods in an assembly, i.e. the null checks that were injected into it.
    /// </summary>
    public static int CountThrowHelperCalls(string assemblyPath)
    {
        using var module = ModuleDefinition.ReadModule(assemblyPath);
        return module.GetTypes().SelectMany(t => t.Methods).Sum(CountThrowHelperCalls);
    }

    /// <summary>
    /// Counts the calls to throw helper methods in a method, i.e. the null checks that were injected into it.
    /// </summary>
    public static int CountThrowHelperCalls(MethodDefinition method)
    {
        if (!method.HasBody)
            return 0;

        return method.Body.Instructions.Count(i =>
            i.OpCode == OpCodes.Call && i.Operand is MethodReference { DeclaringType.FullName: "RuntimeNullables.ThrowHelpers" });
    }
}
