using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using ILVerify;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Verifies assemblies with ILVerify.
/// </summary>
internal static class ILVerification
{
    /// <summary>
    /// Verifies an assembly and returns its errors as sorted <c>Type::Method: ErrorCode</c> entries. IL offsets are left out so that the results for
    /// different builds of the same code can be compared.
    /// </summary>
    /// <remarks>
    /// <para>ILVerify doesn't know runtime async methods (.NET 11), whose body returns the task's result rather than the task, so it reports an error at
    /// their returns. Those errors are left out too, so that errors anywhere else in those methods (which ILVerify stops at) still show up.</para>
    /// </remarks>
    public static IReadOnlyList<string> Verify(string assemblyPath, params VerifierError[] ignoredErrors)
    {
        using var resolver = new Resolver(Path.GetDirectoryName(assemblyPath)!, AppContext.BaseDirectory, RuntimeEnvironment.GetRuntimeDirectory());
        var verifier = new Verifier(resolver, new VerifierOptions { SanityChecks = true });
        verifier.SetSystemModuleName(new AssemblyName("System.Private.CoreLib"));

        using var peReader = new PEReader(File.OpenRead(assemblyPath));
        var metadata = peReader.GetMetadataReader();

        return verifier.Verify(peReader)
            .Where(r => !ignoredErrors.Contains(r.Code) && !IsRuntimeAsyncReturnError(peReader, metadata, r))
            .Select(r => $"{GetMemberName(metadata, r)}: {r.Code}")
            .OrderBy(e => e, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsRuntimeAsyncReturnError(PEReader peReader, MetadataReader metadata, VerificationResult result)
    {
        const MethodImplAttributes asyncImplAttribute = (MethodImplAttributes)0x2000;
        const byte retOpCode = 0x2A;

        if (result.Method.IsNil)
            return false;

        var method = metadata.GetMethodDefinition(result.Method);

        if ((method.ImplAttributes & asyncImplAttribute) == 0)
            return false;

        if (result.ErrorArguments?.FirstOrDefault(a => a.Name == "Offset")?.Value is not int offset)
            return false;

        byte[] il = peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
        return offset < il.Length && il[offset] == retOpCode;
    }

    private static string GetMemberName(MetadataReader metadata, VerificationResult result)
    {
        if (!result.Method.IsNil)
        {
            var method = metadata.GetMethodDefinition(result.Method);
            return $"{GetTypeName(metadata, method.GetDeclaringType())}::{metadata.GetString(method.Name)}";
        }

        return result.Type.IsNil ? "<module>" : GetTypeName(metadata, result.Type);
    }

    private static string GetTypeName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var type = metadata.GetTypeDefinition(handle);
        string name = metadata.GetString(type.Name);
        var declaringType = type.GetDeclaringType();

        if (!declaringType.IsNil)
            return $"{GetTypeName(metadata, declaringType)}/{name}";

        string typeNamespace = metadata.GetString(type.Namespace);
        return typeNamespace.Length == 0 ? name : $"{typeNamespace}.{name}";
    }

    /// <summary>
    /// Resolves assemblies by simple name from a list of directories, earlier directories first.
    /// </summary>
    private sealed class Resolver : IResolver, IDisposable
    {
        private readonly Dictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PEReader?> _readers = new(StringComparer.OrdinalIgnoreCase);

        public Resolver(params string[] directories)
        {
            foreach (string directory in directories)
            {
                foreach (string path in Directory.EnumerateFiles(directory, "*.dll"))
                    _paths.TryAdd(Path.GetFileNameWithoutExtension(path), path);
            }
        }

        public PEReader ResolveAssembly(AssemblyName assemblyName)
        {
            string name = assemblyName.Name!;

            if (!_readers.TryGetValue(name, out var reader))
            {
                reader = _paths.TryGetValue(name, out string? path) ? new PEReader(File.OpenRead(path)) : null;
                _readers.Add(name, reader);
            }

            return reader!;
        }

        public PEReader ResolveModule(AssemblyName referencingAssembly, string fileName) => null!;

        public void Dispose()
        {
            foreach (var reader in _readers.Values)
                reader?.Dispose();
        }
    }
}
