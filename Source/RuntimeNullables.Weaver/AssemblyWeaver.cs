using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Weaves null checks into an assembly file.
/// </summary>
public static class AssemblyWeaver
{
    /// <summary>
    /// Weaves the assembly specified by the options and writes the woven assembly (and its debug symbols, if it has any).
    /// </summary>
    /// <returns><see langword="true"/> if the woven assembly was written, otherwise <see langword="false"/>. Nothing is written when weaving fails, unless
    /// signing the woven assembly shows that the strong name key doesn't match the assembly's public key.</returns>
    public static bool Weave(AssemblyWeaverOptions options, IWeaverLogger logger)
    {
        var errorTrackingLogger = new ErrorTrackingLogger(logger);

        try
        {
            return WeaveCore(options, errorTrackingLogger);
        }
        catch (Exception ex)
        {
            errorTrackingLogger.LogError(GetErrorMessage(ex, options));
            return false;
        }
    }

    private static bool WeaveCore(AssemblyWeaverOptions options, ErrorTrackingLogger logger)
    {
        using var assemblyResolver = new ReferencePathAssemblyResolver(options.ReferencePaths, logger);
        using var module = ReadModule(options.AssemblyPath, assemblyResolver, logger, out bool hasSymbols);

        var weaver = new ModuleWeaver(module, logger)
        {
            CheckOutputs = options.CheckOutputs,
            CheckNonPublic = options.CheckNonPublic,
            DefineConstants = options.DefineConstants,
        };

        weaver.Execute();

        if (logger.HasErrors || !ReferenceCleaner.Clean(module, logger))
            return false;

        var writerParameters = new WriterParameters { WriteSymbols = hasSymbols };

        if (!StrongNameSigner.TryConfigure(module, options, writerParameters, logger))
            return false;

        byte[] publicKey = module.Assembly?.Name.PublicKey ?? [];
        string outputPath = options.OutputPath ?? options.AssemblyPath;

        module.Write(outputPath, writerParameters);

        if (!StrongNameSigner.CheckPublicKeyUnchanged(publicKey, module, logger))
            return false;

        logger.LogInfo($"Wrote the woven assembly to '{outputPath}'.");
        return true;
    }

    /// <summary>
    /// Reads the module and its debug symbols, if it has any and they can be read.
    /// </summary>
    private static ModuleDefinition ReadModule(string assemblyPath, IAssemblyResolver assemblyResolver, IWeaverLogger logger, out bool hasSymbols)
    {
        var readerParameters = new ReaderParameters { AssemblyResolver = assemblyResolver, InMemory = true };
        var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters);
        hasSymbols = false;

        if (!ShouldReadSymbols(module, logger))
            return module;

        // Mono.Cecil.Pdb's provider handles embedded, portable and Windows PDBs. Cecil's default provider only reaches Windows PDB support by loading
        // Mono.Cecil.Pdb by name, which doesn't work reliably when running inside MSBuild.

        ISymbolReader? reader = null;

        try
        {
            reader = new PdbReaderProvider().GetSymbolReader(module, module.FileName);
            module.ReadSymbols(reader, throwIfSymbolsAreNotMaching: true);
            hasSymbols = true;
            return module;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                WeaverWarningKind.DebugSymbols, $"The debug symbols could not be read, so the woven assembly will be written without them: {ex.Message}");

            // A failed read can leave the module holding the symbol reader (which it then disposes), so start over without symbols.

            if (!module.HasSymbols)
                reader?.Dispose();

            module.Dispose();
            return ModuleDefinition.ReadModule(assemblyPath, readerParameters);
        }
    }

    private static bool ShouldReadSymbols(ModuleDefinition module, IWeaverLogger logger)
    {
        var debugEntryTypes = module.HasDebugHeader ? module.GetDebugHeader().Entries.Select(e => e.Directory.Type).ToList() : [];

        if (debugEntryTypes.Contains(ImageDebugType.EmbeddedPortablePdb))
            return true;

        // Decide from the debug directory rather than the PDB file, which can be left over from a build with different settings.

        if (!debugEntryTypes.Contains(ImageDebugType.CodeView))
        {
            logger.LogInfo("The assembly has no debug symbols.");
            return false;
        }

        string pdbPath = Path.ChangeExtension(module.FileName, ".pdb");

        if (!File.Exists(pdbPath))
        {
            logger.LogWarning(
                WeaverWarningKind.DebugSymbols, $"The debug symbols were not found at '{pdbPath}', so the woven assembly will be written without them.");

            return false;
        }

        return true;
    }

    private static string GetErrorMessage(Exception exception, AssemblyWeaverOptions options)
    {
        // I/O errors (e.g. a file locked by another process) and RuntimeNullablesExceptions (problems with the input, possibly wrapped with context) are
        // described well enough by their messages. Anything else is unexpected and gets the full details.

        if (exception is IOException or UnauthorizedAccessException)
        {
            return exception.Message.Contains(Path.GetFileName(options.AssemblyPath)) ?
                exception.Message : $"{exception.Message} (weaving '{options.AssemblyPath}')";
        }

        var exceptionChain = new List<Exception>();

        for (var current = exception; current != null; current = current.InnerException)
            exceptionChain.Add(current);

        if (exceptionChain.All(e => e is RuntimeNullablesException))
            return string.Join(" ", exceptionChain.Select(e => e.Message));

        return $"Unexpected error while weaving '{options.AssemblyPath}': {exception}";
    }

    private sealed class ErrorTrackingLogger(IWeaverLogger logger) : IWeaverLogger
    {
        public bool HasErrors { get; private set; }

        public void LogInfo(string message) => logger.LogInfo(message);

        public void LogWarning(WeaverWarningKind kind, string message) => logger.LogWarning(kind, message);

        public void LogError(string message)
        {
            HasErrors = true;
            logger.LogError(message);
        }
    }
}
