using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using RuntimeNullables.Tests.Infrastructure;
using RuntimeNullables.Weaver;
using TestAssembly;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;

namespace RuntimeNullables.Tests;

[TestClass]
public class DebugSymbolTests
{
    private const string AssemblyName = "TestAssemblyThrowHelpers";

    [TestMethod]
    [DynamicData(nameof(TestAssemblies.All), typeof(TestAssemblies))]
    public void PdbMatchesWovenAssembly(string assemblyName)
    {
        string assemblyPath = TestFiles.GetWovenAssemblyPath(assemblyName);

        using var peReader = new PEReader(File.OpenRead(assemblyPath));
        var codeViewEntry = peReader.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.CodeView);
        var codeViewData = peReader.ReadCodeViewDebugDirectoryData(codeViewEntry);

        using var pdbProvider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.ChangeExtension(assemblyPath, ".pdb")));
        var pdbId = new BlobContentId(pdbProvider.GetMetadataReader().DebugMetadataHeader!.Id);

        Assert.AreEqual(codeViewData.Guid, pdbId.Guid);
    }

    [TestMethod]
    public void StackTraceHasSourceLineOfWovenMethod()
    {
        // Invoked through reflection so that the woven method has its own frame (it can't be inlined into the caller). The injected null check calls
        // the throw helper, so the woven method's frame is the one below the helper's.

        var method = typeof(InputParameters).GetMethod(nameof(InputParameters.Reference))!;
        var ex = Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(null, [null]));
        var frame = new StackTrace(ex.InnerException!, fNeedFileInfo: true).GetFrames().First(f => f.GetMethod() == method);

        StringAssert.EndsWith(frame.GetFileName(), "InputParameters.cs");
        Assert.IsTrue(frame.GetFileLineNumber() > 0);
    }

    [TestMethod]
    public void EmbeddedPdbIsKept()
    {
        using var directory = new TempDirectory();
        string assemblyPath = directory.GetFilePath(AssemblyName + ".dll");
        WriteUnwovenAssembly(assemblyPath, new EmbeddedPortablePdbWriterProvider());

        var logger = Weave(assemblyPath);

        Assert.AreEqual(0, logger.Warnings.Count, logger.ToString());
        Assert.IsFalse(File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")));

        using var module = ModuleDefinition.ReadModule(assemblyPath);
        module.ReadSymbols(new EmbeddedPortablePdbReaderProvider().GetSymbolReader(module, assemblyPath), throwIfSymbolsAreNotMaching: true);
        Assert.IsTrue(module.HasSymbols);
    }

    [TestMethod]
    public void WindowsPdbIsKept()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Windows PDBs can only be written on Windows.");

        using var directory = new TempDirectory();
        string assemblyPath = directory.GetFilePath(AssemblyName + ".dll");
        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
        WriteUnwovenAssembly(assemblyPath, new NativePdbWriterProvider());

        var logger = Weave(assemblyPath);

        Assert.AreEqual(0, logger.Warnings.Count, logger.ToString());
        StringAssert.StartsWith(Encoding.ASCII.GetString(File.ReadAllBytes(pdbPath), 0, 19), "Microsoft C/C++ MSF");

        using var module = ModuleDefinition.ReadModule(assemblyPath);
        module.ReadSymbols(new NativePdbReaderProvider().GetSymbolReader(module, assemblyPath), throwIfSymbolsAreNotMaching: true);
        Assert.IsTrue(module.HasSymbols);
    }

    [TestMethod]
    public void MissingPdbIsReported()
    {
        using var directory = new TempDirectory();
        string assemblyPath = directory.GetFilePath(AssemblyName + ".dll");
        File.Copy(TestFiles.GetUnwovenAssemblyPath(AssemblyName), assemblyPath);

        var logger = Weave(assemblyPath);

        Assert.AreEqual(1, logger.Warnings.Count, logger.ToString());
        Assert.AreEqual(WeaverWarningKind.DebugSymbols, logger.Warnings[0].Kind);
        StringAssert.Contains(logger.Warnings[0].Message, "were not found");
    }

    [TestMethod]
    public void MismatchedPdbIsReported()
    {
        using var directory = new TempDirectory();
        string assemblyPath = directory.GetFilePath(AssemblyName + ".dll");
        File.Copy(TestFiles.GetUnwovenAssemblyPath(AssemblyName), assemblyPath);
        File.Copy(Path.ChangeExtension(TestFiles.GetUnwovenAssemblyPath("TestAssemblyPointers"), ".pdb"), Path.ChangeExtension(assemblyPath, ".pdb"));

        var logger = Weave(assemblyPath);

        Assert.AreEqual(1, logger.Warnings.Count, logger.ToString());
        Assert.AreEqual(WeaverWarningKind.DebugSymbols, logger.Warnings[0].Kind);
        StringAssert.Contains(logger.Warnings[0].Message, "could not be read");
    }

    [TestMethod]
    public void LeftoverPdbIsIgnoredWhenAssemblyHasNoDebugInformation()
    {
        // E.g. after switching to DebugType=none without cleaning.

        using var directory = new TempDirectory();
        string assemblyPath = directory.GetFilePath(AssemblyName + ".dll");
        WriteUnwovenAssembly(assemblyPath, symbolWriterProvider: null);
        File.Copy(Path.ChangeExtension(TestFiles.GetUnwovenAssemblyPath(AssemblyName), ".pdb"), Path.ChangeExtension(assemblyPath, ".pdb"));

        using (var peReader = new PEReader(File.OpenRead(assemblyPath)))
            Assert.IsFalse(peReader.ReadDebugDirectory().Any(e => e.Type == DebugDirectoryEntryType.CodeView), "Test setup: the assembly has a PDB reference.");

        var logger = Weave(assemblyPath);

        Assert.AreEqual(0, logger.Warnings.Count, logger.ToString());
    }

    private static void WriteUnwovenAssembly(string path, ISymbolWriterProvider? symbolWriterProvider)
    {
        var readerParameters = new ReaderParameters { InMemory = true, ReadSymbols = symbolWriterProvider != null };
        using var module = ModuleDefinition.ReadModule(TestFiles.GetUnwovenAssemblyPath(AssemblyName), readerParameters);

        module.Write(path, new WriterParameters { WriteSymbols = symbolWriterProvider != null, SymbolWriterProvider = symbolWriterProvider });
    }

    private static TestWeaverLogger Weave(string assemblyPath)
    {
        var logger = new TestWeaverLogger();

        Assert.IsTrue(AssemblyWeaver.Weave(new AssemblyWeaverOptions(assemblyPath, TestFiles.GetReferencePaths(AssemblyName)), logger), logger.ToString());
        Assert.IsTrue(TestFiles.CountThrowHelperCalls(assemblyPath) > 0, "The assembly was not woven.");

        return logger;
    }
}
