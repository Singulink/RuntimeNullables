using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using RuntimeNullables.Tests.Infrastructure;
using RuntimeNullables.Weaver;

namespace RuntimeNullables.Tests;

[TestClass]
public class AssemblyWeaverTests
{
    private const string AssemblyName = "TestAssemblyThrowHelpers";

    [TestMethod]
    public void WeavingWritesAssemblyInPlaceByDefault()
    {
        using var directory = new TempDirectory();
        string assemblyPath = directory.GetFilePath(AssemblyName + ".dll");
        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
        File.Copy(TestFiles.GetUnwovenAssemblyPath(AssemblyName), assemblyPath);
        File.Copy(Path.ChangeExtension(TestFiles.GetUnwovenAssemblyPath(AssemblyName), ".pdb"), pdbPath);
        byte[] unwovenPdb = File.ReadAllBytes(pdbPath);

        var logger = new TestWeaverLogger();

        Assert.IsTrue(AssemblyWeaver.Weave(new AssemblyWeaverOptions(assemblyPath, TestFiles.GetReferencePaths(AssemblyName)), logger), logger.ToString());
        Assert.IsTrue(TestFiles.CountThrowHelperCalls(assemblyPath) > 0, "The assembly was not woven.");
        CollectionAssert.AreNotEqual(unwovenPdb, File.ReadAllBytes(pdbPath), "The PDB was not written.");
        Assert.AreEqual(0, logger.Warnings.Count, logger.ToString());
    }

    [TestMethod]
    [DataRow("ThrowArgumentNull", "instance", "Throw helper methods must be static.")]
    [DataRow("ThrowOutputNull", "instance", "Throw helper methods must be static.")]
    [DataRow("ThrowArgumentNull", "returns object", "Throw helper method 'ThrowArgumentNull' must return 'System.Void'.")]
    [DataRow("GetAsyncResultNullException", "returns void", "Throw helper method 'GetAsyncResultNullException' must return 'System.Exception'.")]
    public void InvalidThrowHelperIsReportedWithoutExceptionDetails(string methodName, string change, string expectedError)
    {
        using var directory = new TempDirectory();
        string inputPath = directory.GetFilePath(AssemblyName + ".dll");
        string outputPath = directory.GetFilePath("Output.dll");

        TestFiles.WriteModifiedCopy(AssemblyName, inputPath, module => {
            var method = module.GetType("RuntimeNullables.ThrowHelpers").Methods.Single(m => m.Name == methodName);

            switch (change)
            {
                case "instance":
                    method.IsStatic = false;
                    method.HasThis = true;
                    break;
                case "returns object":
                    method.ReturnType = module.TypeSystem.Object;
                    break;
                case "returns void":
                    method.ReturnType = module.TypeSystem.Void;
                    break;
            }
        });

        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(inputPath, TestFiles.GetReferencePaths(AssemblyName)) { OutputPath = outputPath, CheckOutputs = true };

        Assert.IsFalse(AssemblyWeaver.Weave(options, logger));
        CollectionAssert.AreEqual(new[] { "Error injecting throw helpers. " + expectedError }, logger.Errors);
        Assert.IsFalse(File.Exists(outputPath));
    }

    [TestMethod]
    public void UnresolvableReferenceIsReportedWithoutExceptionDetails()
    {
        using var directory = new TempDirectory();
        string inputPath = directory.GetFilePath("TestAssembly.dll");

        // Make a method return Task<string> from an assembly that isn't one of the references, so the task type can't be resolved to check its result.

        TestFiles.WriteModifiedCopy("TestAssembly", inputPath, module => {
            var taskType = MissingAssembly.GetType(module, "System.Threading.Tasks", "Task`1", genericParameterCount: 1);
            var method = module.GetType("TestAssembly.TaskResults").Methods.Single(m => m.Name == "GoodReference");
            method.ReturnType = taskType.MakeGenericInstanceType(module.TypeSystem.String);
        });

        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(inputPath, TestFiles.GetReferencePaths("TestAssembly")) { CheckOutputs = true };

        Assert.IsFalse(AssemblyWeaver.Weave(options, logger));
        Assert.AreEqual(1, logger.Errors.Count, logger.ToString());
        StringAssert.Contains(logger.Errors[0], "Could not resolve 'System.Threading.Tasks.Task`1' from 'MissingAssembly'.");
        Assert.IsFalse(logger.Errors[0].Contains(" at ", System.StringComparison.Ordinal), "The error should not contain a stack trace.");
    }

    [TestMethod]
    [DataRow("base type")]
    [DataRow("interface")]
    [DataRow("explicit override")]
    public void UnresolvableReferenceInPublicApiIsReportedWithoutExceptionDetails(string reference)
    {
        using var directory = new TempDirectory();
        string inputPath = directory.GetFilePath("TestAssemblyNonPublic.dll");

        // Point an internal type at a type from an assembly that isn't one of the references. Finding out whether the internal type's methods are
        // part of the public API (only needed when members outside the public API aren't checked) needs that type.

        TestFiles.WriteModifiedCopy("TestAssemblyNonPublic", inputPath, module => {
            var missingType = MissingAssembly.GetType(module, "Missing", "MissingType");
            var subClass = module.GetType("TestAssemblyNonPublic.InternalSubClass");

            switch (reference)
            {
                case "base type":
                    subClass.BaseType = missingType;
                    break;
                case "interface":
                    module.GetType("TestAssemblyNonPublic.InternalImplicitImplementation").Interfaces.Add(new InterfaceImplementation(missingType));
                    break;
                case "explicit override":
                    var explicitImplementation = subClass.Methods.Single(m => m.Name.EndsWith(".InterfaceInternalMethod", System.StringComparison.Ordinal));
                    explicitImplementation.Overrides.Add(new MethodReference("Method", module.TypeSystem.Void, missingType) { HasThis = true });
                    break;
            }
        });

        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(inputPath, TestFiles.GetReferencePaths("TestAssemblyNonPublic")) { CheckNonPublic = false };

        Assert.IsFalse(AssemblyWeaver.Weave(options, logger));
        Assert.AreEqual(1, logger.Errors.Count, logger.ToString());
        StringAssert.Contains(logger.Errors[0], "Missing.MissingType");
        StringAssert.Contains(logger.Errors[0], "from 'MissingAssembly'.");
        Assert.IsFalse(logger.Errors[0].Contains(" at ", System.StringComparison.Ordinal), "The error should not contain a stack trace.");
    }

    [TestMethod]
    public void LockedOutputIsReportedWithoutExceptionDetails()
    {
        if (!System.OperatingSystem.IsWindows())
            Assert.Inconclusive("Relies on Windows file locking.");

        using var directory = new TempDirectory();
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(TestFiles.GetUnwovenAssemblyPath(AssemblyName), TestFiles.GetReferencePaths(AssemblyName))
        {
            OutputPath = outputPath,
        };

        using (File.Create(outputPath))
            Assert.IsFalse(AssemblyWeaver.Weave(options, logger));

        Assert.AreEqual(1, logger.Errors.Count, logger.ToString());
        StringAssert.Contains(logger.Errors[0], outputPath);
        Assert.IsFalse(logger.Errors[0].Contains(" at ", System.StringComparison.Ordinal), "The error should not contain a stack trace.");
    }
}
