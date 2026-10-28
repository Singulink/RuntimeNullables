using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Finds public BCL types in the core framework assemblies available to an assembly resolver.
/// </summary>
/// <remarks>
/// <para>This mirrors how Fody located types for weavers (same assemblies, same scan order and the same precedence of type definitions over type
/// forwarders) so that imported BCL types keep resolving to the same assemblies they did when RuntimeNullables was a Fody add-in. Assemblies are only
/// resolved when a lookup needs them.</para>
/// </remarks>
internal sealed class TypeFinder
{
    // Fody's default scan list followed by the extra assemblies RuntimeNullables asked Fody to scan (duplicates removed).
    private static readonly string[] _scannedAssemblyNames = ["mscorlib", "System", "System.Runtime", "System.Core", "netstandard", "System.Diagnostics.Tools"];

    private readonly IAssemblyResolver _assemblyResolver;
    private readonly AssemblyDefinition?[] _assemblies = new AssemblyDefinition?[_scannedAssemblyNames.Length];
    private readonly bool[] _resolved = new bool[_scannedAssemblyNames.Length];
    private readonly Dictionary<string, TypeDefinition?> _cache = new(StringComparer.Ordinal);

    public TypeFinder(IAssemblyResolver assemblyResolver)
    {
        _assemblyResolver = assemblyResolver;
    }

    public TypeDefinition? Find(string fullName)
    {
        if (!_cache.TryGetValue(fullName, out var type))
            _cache.Add(fullName, type = FindDefinedType(fullName) ?? FindForwardedType(fullName));

        return type;
    }

    private TypeDefinition? FindDefinedType(string fullName)
    {
        for (int i = 0; i < _scannedAssemblyNames.Length; i++)
        {
            if (GetAssembly(i)?.MainModule.GetType(fullName) is { IsPublic: true } type)
                return type;
        }

        return null;
    }

    private TypeDefinition? FindForwardedType(string fullName)
    {
        var assemblies = Enumerable.Range(0, _scannedAssemblyNames.Length).Select(GetAssembly).OfType<AssemblyDefinition>().ToList();
        var assemblyNames = new HashSet<string>(assemblies.Select(a => a.Name.Name), StringComparer.Ordinal);

        // Forwarders into the scanned assemblies are skipped since the defined type search already covered those assemblies.

        foreach (var assembly in assemblies)
        {
            foreach (var exportedType in assembly.MainModule.ExportedTypes)
            {
                if (exportedType.FullName != fullName || assemblyNames.Contains(exportedType.Scope.Name))
                    continue;

                if (TryResolve(exportedType) is { IsPublic: true } type)
                    return type;
            }
        }

        return null;
    }

    private AssemblyDefinition? GetAssembly(int index)
    {
        if (!_resolved[index])
        {
            _assemblies[index] = TryResolve(_scannedAssemblyNames[index]);
            _resolved[index] = true;
        }

        return _assemblies[index];
    }

    private AssemblyDefinition? TryResolve(string name)
    {
        try
        {
            return _assemblyResolver.Resolve(new AssemblyNameReference(name, null));
        }
        catch (AssemblyResolutionException)
        {
            return null;
        }
    }

    private static TypeDefinition? TryResolve(ExportedType exportedType)
    {
        try
        {
            return exportedType.Resolve();
        }
        catch (AssemblyResolutionException)
        {
            return null;
        }
    }
}
