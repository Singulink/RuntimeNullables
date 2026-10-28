using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.EndToEndTests.Infrastructure;

namespace RuntimeNullables.EndToEndTests;

[TestClass]
public class BuildTests
{
    private const string ConflictingAnnotationsSource = """
        using System.Diagnostics.CodeAnalysis;

        public static class Conflicting
        {
            public static void Method([AllowNull, DisallowNull] string value) { }
        }
        """;

    private const string InvalidThrowHelpersSource = """
        namespace RuntimeNullables;

        internal class ThrowHelpers
        {
            internal void ThrowArgumentNull(string paramName) { }
        }
        """;

    [TestMethod]
    public void BuildWeavesAndBuildWithoutChangesDoesNotCompileAgain()
    {
        using var project = new TestProject(nameof(BuildWeavesAndBuildWithoutChangesDoesNotCompileAgain));

        var result = project.Build();
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(0, result.Diagnostics.Count, result.Output);
        Assert.AreEqual(AppResult.Default, project.Run());

        // The woven assembly doesn't reference RuntimeNullables.dll, so it isn't part of the app.
        Assert.IsFalse(File.Exists(Path.Combine(project.OutputDirectory, "RuntimeNullables.dll")));
        Assert.IsFalse(File.ReadAllText(Path.Combine(project.OutputDirectory, "App.deps.json")).Contains("RuntimeNullables"));

        var writeTime = project.IntermediateAssemblyWriteTime;
        result = project.Build();
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(writeTime, project.IntermediateAssemblyWriteTime, "A build without changes doesn't compile again.");
    }

    [TestMethod]
    public void ChangingASettingWeavesAgain()
    {
        using var project = new TestProject(nameof(ChangingASettingWeavesAgain));
        Assert.IsTrue(project.Build().Succeeded);
        string? defaultHash = project.IntermediateAssemblyHash;

        var result = project.Build("-p:RuntimeNullablesCheckOutputs=true");
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(AppResult.OutputsChecked, project.Run());

        var writeTime = project.IntermediateAssemblyWriteTime;
        Assert.IsTrue(project.Build("-p:RuntimeNullablesCheckOutputs=true").Succeeded);
        Assert.AreEqual(writeTime, project.IntermediateAssemblyWriteTime, "The same setting doesn't compile again.");

        Assert.IsTrue(project.Build().Succeeded);
        Assert.AreEqual(AppResult.Default, project.Run());
        Assert.AreEqual(defaultHash, project.IntermediateAssemblyHash);
    }

    [TestMethod]
    [DataRow("yes", true)]
    [DataRow("!no", true)]
    [DataRow("Off", false)]
    public void SettingsAcceptMSBuildBooleanValues(string value, bool checkOutputs)
    {
        // The settings are checked before compiling with MSBuild conditions, which accept these values, so weaving has to accept them too.

        using var project = new TestProject(nameof(SettingsAcceptMSBuildBooleanValues));

        var result = project.Build("-p:RuntimeNullablesCheckOutputs=" + value);
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(checkOutputs ? AppResult.OutputsChecked : AppResult.Default, project.Run());
    }

    [TestMethod]
    public void WeavingInASeparateProcessWorks()
    {
        using var project = new TestProject(nameof(WeavingInASeparateProcessWorks));

        var result = project.Build("-p:RuntimeNullablesUseTaskHost=true");
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(AppResult.Default, project.Run());
    }

    [TestMethod]
    public void TurningWeavingOffAndOnAgain()
    {
        using var project = new TestProject(nameof(TurningWeavingOffAndOnAgain));

        var result = project.Build("-p:RuntimeNullablesEnabled=false");
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(AppResult.NotWoven, project.Run());
        Assert.IsTrue(File.Exists(Path.Combine(project.OutputDirectory, "RuntimeNullables.dll")), "The unwoven assembly needs RuntimeNullables.dll.");

        // The stamp is written even though nothing was woven, so a second build doesn't compile again.
        var writeTime = project.IntermediateAssemblyWriteTime;
        Assert.IsTrue(project.Build("-p:RuntimeNullablesEnabled=false").Succeeded);
        Assert.AreEqual(writeTime, project.IntermediateAssemblyWriteTime);

        Assert.IsTrue(project.Build().Succeeded);
        Assert.AreEqual(AppResult.Default, project.Run());
        Assert.IsFalse(File.Exists(Path.Combine(project.OutputDirectory, "RuntimeNullables.dll")));
    }

    [TestMethod]
    public void PublishedAppDoesNotIncludeRuntimeNullables()
    {
        using var project = new TestProject(nameof(PublishedAppDoesNotIncludeRuntimeNullables));

        var result = project.Build("-t:Publish");
        Assert.IsTrue(result.Succeeded, result.Output);

        string publishDirectory = Path.Combine(project.OutputDirectory, "publish");
        Assert.IsTrue(File.Exists(Path.Combine(publishDirectory, "App.dll")), result.Output);
        Assert.IsFalse(File.Exists(Path.Combine(publishDirectory, "RuntimeNullables.dll")));
        Assert.IsFalse(File.ReadAllText(Path.Combine(publishDirectory, "App.deps.json")).Contains("RuntimeNullables"));
    }

    [TestMethod]
    public void InvalidSettingFailsBeforeCompiling()
    {
        using var project = new TestProject(nameof(InvalidSettingFailsBeforeCompiling));

        var result = project.Build("-p:RuntimeNullablesCheckOutputs=bogus");
        Assert.IsFalse(result.Succeeded, result.Output);
        CollectionAssert.AreEqual(new[] { "error RN1002" }, result.ToolDiagnostics, result.Output);
        Assert.AreEqual(project.ProjectFile, result.Single("RN1002").File);
        Assert.IsNull(project.IntermediateAssemblyHash);
    }

    [TestMethod]
    public void WeavingErrorFailsEveryBuildUntilFixed()
    {
        using var project = new TestProject(nameof(WeavingErrorFailsEveryBuildUntilFixed));
        project.WriteFile("ThrowHelpers.cs", InvalidThrowHelpersSource);

        for (int i = 0; i < 2; i++)
        {
            var result = project.Build();
            Assert.IsFalse(result.Succeeded, result.Output);
            StringAssert.Contains(result.Single("RN1001").Message, "Throw helper methods must be static.");
        }

        project.DeleteFile("ThrowHelpers.cs");
        Assert.IsTrue(project.Build().Succeeded);
        Assert.AreEqual(AppResult.Default, project.Run());
    }

    [TestMethod]
    public void InterruptedWeavingIsDoneByTheNextBuild()
    {
        // Compiling without running the targets that compiling triggers is what a build cancelled between compiling and weaving leaves behind.

        using var project = new TestProject(nameof(InterruptedWeavingIsDoneByTheNextBuild));
        Assert.IsTrue(project.Build("-p:TargetsTriggeredByCompilation=").Succeeded);
        Assert.AreEqual(AppResult.NotWoven, project.Run());

        Assert.IsTrue(project.Build().Succeeded);
        Assert.AreEqual(AppResult.Default, project.Run());
    }

    [TestMethod]
    [DataRow("", "warning RN0001")]
    [DataRow("-p:TreatWarningsAsErrors=true", "error RN0001")]
    [DataRow("-p:TreatWarningsAsErrors=true;NoWarn=RN0001", null)]
    [DataRow("-p:TreatWarningsAsErrors=true;RuntimeNullablesTreatWarningsAsErrors=false", "warning RN0001")]
    [DataRow("-p:WarningsAsErrors=RN0001", "error RN0001")]
    [DataRow("-warnaserror", "error RN0001")]
    public void WeavingWarningFollowsWarningSettings(string argument, string? expected)
    {
        using var project = new TestProject(nameof(WeavingWarningFollowsWarningSettings));
        project.WriteFile("Conflicting.cs", ConflictingAnnotationsSource);

        var result = project.Build(argument);
        Assert.AreEqual(expected?.StartsWith("error") != true, result.Succeeded, result.Output);
        CollectionAssert.AreEqual(expected != null ? new[] { expected } : [], result.ToolDiagnostics, result.Output);
    }

    [TestMethod]
    [DataRow("", "warning RN0003")]
    [DataRow("-warnaserror", "error RN0003")]
    public void PackageReferenceWithoutPrivateAssetsIsReported(string argument, string expected)
    {
        using var project = new TestProject(nameof(PackageReferenceWithoutPrivateAssetsIsReported));
        project.WriteProject(privateAssets: false);

        var result = project.Build(argument);
        Assert.AreEqual(expected.StartsWith("warning"), result.Succeeded, result.Output);
        CollectionAssert.AreEqual(new[] { expected }, result.ToolDiagnostics, result.Output);

        // The message names the package (the build file gets the ID from its own file name).
        StringAssert.Contains(result.Single("RN0003").Message, $"The {TestEnvironment.PackageId} package reference should have PrivateAssets=\"all\"");
    }

    [TestMethod]
    public void DesignTimeBuildDoesNotWeave()
    {
        using var project = new TestProject(nameof(DesignTimeBuildDoesNotWeave));
        Assert.IsTrue(project.Build().Succeeded);
        var writeTime = project.IntermediateAssemblyWriteTime;

        project.WriteFile("Program.cs", TestProject.ProgramSource + "\n// Changed\n");
        var result = project.Build("-t:CoreCompile", "-p:DesignTimeBuild=true", "-p:SkipCompilerExecution=true", "-p:ProvideCommandLineArgs=true");

        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(0, result.Diagnostics.Count, result.Output);
        Assert.AreEqual(writeTime, project.IntermediateAssemblyWriteTime);
    }
}
