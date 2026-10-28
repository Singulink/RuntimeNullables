using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Resolves assemblies by simple name from a fixed list of assembly file paths (e.g. the project's compile references).
/// </summary>
/// <remarks>
/// <para>Assemblies are matched by file name first. If that fails, the referenced files are opened once to match by their actual assembly name. Resolved
/// assemblies are read on demand and cached (along with their file handles) until the resolver is disposed.</para>
/// </remarks>
internal sealed class ReferencePathAssemblyResolver : IAssemblyResolver
{
    private readonly Dictionary<string, string> _pathsByFileName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AssemblyDefinition?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly IWeaverLogger _logger;

    private Dictionary<string, string>? _pathsByAssemblyName;

    public ReferencePathAssemblyResolver(IEnumerable<string> referencePaths, IWeaverLogger logger)
    {
        _logger = logger;

        foreach (string path in referencePaths)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);

            // First one wins.
            if (!_pathsByFileName.ContainsKey(fileName))
                _pathsByFileName.Add(fileName, path);
        }
    }

    public AssemblyDefinition? Resolve(AssemblyNameReference name) => Resolve(name, null);

    public AssemblyDefinition? Resolve(AssemblyNameReference name, ReaderParameters? parameters)
    {
        if (!_cache.TryGetValue(name.Name, out var assembly))
        {
            assembly = Resolve(name.Name);
            _cache.Add(name.Name, assembly);
        }

        return assembly;
    }

    public void Dispose()
    {
        foreach (var assembly in _cache.Values)
            assembly?.Dispose();

        _cache.Clear();
    }

    private AssemblyDefinition? Resolve(string name)
    {
        if (_pathsByFileName.TryGetValue(name, out string? path) && TryRead(path) is { } assembly)
        {
            if (string.Equals(assembly.Name.Name, name, StringComparison.OrdinalIgnoreCase))
                return assembly;

            assembly.Dispose();
        }

        return GetPathsByAssemblyName().TryGetValue(name, out path) ? TryRead(path) : null;
    }

    private Dictionary<string, string> GetPathsByAssemblyName()
    {
        if (_pathsByAssemblyName != null)
            return _pathsByAssemblyName;

        _pathsByAssemblyName = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in _pathsByFileName.Values)
        {
            try
            {
                using var module = ModuleDefinition.ReadModule(path, new ReaderParameters(ReadingMode.Deferred));

                if (module.Assembly?.Name.Name is { } assemblyName && !_pathsByAssemblyName.ContainsKey(assemblyName))
                    _pathsByAssemblyName.Add(assemblyName, path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException)
            {
                // Not a readable managed assembly - nothing can be resolved from it.
            }
        }

        return _pathsByAssemblyName;
    }

    private AssemblyDefinition? TryRead(string path)
    {
        try
        {
            return AssemblyDefinition.ReadAssembly(path, new ReaderParameters(ReadingMode.Deferred) { AssemblyResolver = this });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException)
        {
            _logger.LogInfo($"Could not read reference '{path}': {ex.Message}");
            return null;
        }
    }
}
