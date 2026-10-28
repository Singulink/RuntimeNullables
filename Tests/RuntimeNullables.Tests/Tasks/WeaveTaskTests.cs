using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using RuntimeNullables.Tasks;
using RuntimeNullables.Tests.Infrastructure;

namespace RuntimeNullables.Tests;

[TestClass]
public class WeaveTaskTests
{
    [TestMethod]
    public void InvalidSettingsAreReported()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory, "TestAssemblyThrowHelpers");
        task.CheckOutputs = "1";
        task.CheckNonPublic = "maybe";

        Assert.IsFalse(task.Execute());
        Assert.AreEqual(2, engine.Errors.Count);
        Assert.IsTrue(engine.Errors.All(e => e.Code == "RN1002" && e.File == task.ProjectPath));
        StringAssert.Contains(engine.Errors[0].Message, "RuntimeNullablesCheckOutputs");
        StringAssert.Contains(engine.Errors[1].Message, "RuntimeNullablesCheckNonPublic");
    }

    [TestMethod]
    [DataRow("TRUE", "false", "True", "False")]
    [DataRow("yes", "no", "True", "False")]
    [DataRow("on", "Off", "True", "False")]
    [DataRow("!false", "!yes", "True", "False")]
    [DataRow("!no", "!ON", "True", "False")]
    public void SettingsAreParsedLikeMSBuildParsesThem(
        string checkOutputs, string checkNonPublic, string expectedCheckOutputs, string expectedCheckNonPublic)
    {
        // The build targets check the settings with MSBuild conditions, which accept these values, so the task has to accept them too.

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory, "TestAssemblyThrowHelpers");
        task.CheckOutputs = checkOutputs;
        task.CheckNonPublic = checkNonPublic;

        Assert.IsTrue(task.Execute(), string.Join(Environment.NewLine, engine.Errors.Select(e => e.Message)));

        // The weaver logs the settings it uses.
        string expected = $"RuntimeNullables: Starting injection - CheckOutputs: {expectedCheckOutputs}, CheckNonPublic: {expectedCheckNonPublic}.";
        Assert.IsTrue(engine.Messages.Any(m => m.Message == expected), expected);
    }

    [TestMethod]
    public void WeavesAssemblyAndReportsIt()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory, "TestAssemblyThrowHelpers");
        task.CheckOutputs = "true";

        Assert.IsTrue(task.Execute(), string.Join(Environment.NewLine, engine.Errors.Select(e => e.Message)));
        Assert.IsTrue(TestFiles.CountThrowHelperCalls(task.AssemblyPath) > 0);
        Assert.IsTrue(engine.Messages.Any(m =>
            m.Importance == MessageImportance.Normal && m.Message!.StartsWith("RuntimeNullables: Wove", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow(false, null, null, false)]
    [DataRow(true, null, null, true)]
    [DataRow(true, "CS1591;RN0001", null, false)]
    [DataRow(true, null, "RN0004, rn0001", false)]
    public void SkippedChecksWarningFollowsWarningSettings(bool treatWarningsAsErrors, string? noWarn, string? warningsNotAsErrors, bool expectError)
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory, "TestAssembly", AddConflictingAnnotations);
        task.TreatWarningsAsErrors = treatWarningsAsErrors;
        task.NoWarn = noWarn;
        task.WarningsNotAsErrors = warningsNotAsErrors;

        Assert.AreEqual(!expectError, task.Execute());

        var diagnostics = expectError ?
            engine.Errors.Select(e => (e.Code, e.File, e.Message)) : engine.Warnings.Select(w => (w.Code, w.File, w.Message));

        var diagnostic = diagnostics.Single();
        Assert.AreEqual("RN0001", diagnostic.Code);
        Assert.AreEqual(task.ProjectPath, diagnostic.File);
        StringAssert.Contains(diagnostic.Message, "conflicting preconditions");

        static void AddConflictingAnnotations(ModuleDefinition module)
        {
            var parameter = module.GetType("TestAssembly.InputParameters").Methods.Single(m => m.Name == "Reference").Parameters[0];
            parameter.CustomAttributes.Add(new CustomAttribute(module.ImportReference(typeof(AllowNullAttribute).GetConstructor(Type.EmptyTypes))));
            parameter.CustomAttributes.Add(new CustomAttribute(module.ImportReference(typeof(DisallowNullAttribute).GetConstructor(Type.EmptyTypes))));
        }
    }

    [TestMethod]
    public void MissingDebugSymbolsAreReportedWithTheirOwnCode()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory, "TestAssemblyThrowHelpers");
        File.Delete(Path.ChangeExtension(task.AssemblyPath, ".pdb"));

        Assert.IsTrue(task.Execute());
        Assert.AreEqual("RN0004", engine.Warnings.Single().Code);
    }

    [TestMethod]
    public void RelativeKeyFileIsResolvedAgainstTheProjectDirectory()
    {
        // The project is in a subdirectory of the assembly's directory, and the key name doesn't exist anywhere else (e.g. the current directory).

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory, "TestAssemblySigned");
        Directory.CreateDirectory(directory.GetFilePath(@"Project\Keys"));
        File.Copy(TestFiles.KeyFilePath, directory.GetFilePath(@"Project\Keys\ProjectKey.snk"));
        task.ProjectPath = directory.GetFilePath(@"Project\Project.csproj");
        task.KeyFile = @"Keys\ProjectKey.snk";

        Assert.IsTrue(task.Execute(), string.Join(Environment.NewLine, engine.Errors.Select(e => e.Message)));
    }

    /// <summary>
    /// Creates a task that weaves a copy of an unwoven test assembly (and its PDB) in the given directory, which also contains the project.
    /// </summary>
    private static (WeaveRuntimeNullables Task, BuildEngineStub Engine) CreateTask(
        TempDirectory directory, string assemblyName, Action<ModuleDefinition>? modifyAssembly = null)
    {
        string unwovenPath = TestFiles.GetUnwovenAssemblyPath(assemblyName);
        string assemblyPath = directory.GetFilePath(assemblyName + ".dll");

        if (modifyAssembly == null)
        {
            File.Copy(unwovenPath, assemblyPath);
            File.Copy(Path.ChangeExtension(unwovenPath, ".pdb"), Path.ChangeExtension(assemblyPath, ".pdb"));
        }
        else
        {
            using var module = ModuleDefinition.ReadModule(unwovenPath, new ReaderParameters { InMemory = true, ReadSymbols = true });
            modifyAssembly(module);
            module.Write(assemblyPath, new WriterParameters { WriteSymbols = true });
        }

        var engine = new BuildEngineStub();
        var task = new WeaveRuntimeNullables
        {
            BuildEngine = engine,
            AssemblyPath = assemblyPath,
            ProjectPath = directory.GetFilePath("Project.csproj"),
            References = TestFiles.GetReferencePaths(assemblyName).Select(p => (ITaskItem)new TaskItem(p)).ToArray(),
            DefineConstants = "TRACE;DEBUG",
        };

        return (task, engine);
    }
}
