using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.EndToEndTests.Infrastructure;

namespace RuntimeNullables.EndToEndTests;

/// <summary>
/// Tests that the build finds legacy Fody configuration and uses its settings.
/// </summary>
[TestClass]
public class LegacyConfigurationTests
{
    [TestMethod]
    public void ProjectFodyWeaversSettingsAreUsedUntilRemoved()
    {
        using var project = new TestProject(nameof(ProjectFodyWeaversSettingsAreUsedUntilRemoved));
        string file = project.WriteFodyWeavers("FodyWeavers.xml", """<RuntimeNullables CheckOutputs="true" />""");

        var result = project.Build();
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(AppResult.OutputsChecked, project.Run());

        var warning = result.Single("RN0002");
        Assert.AreEqual((file, 2, 4), (warning.File, warning.Line, warning.Column));
        StringAssert.Contains(warning.Message, "<RuntimeNullablesCheckOutputs>true</RuntimeNullablesCheckOutputs>");

        // The warning is reported by every build, which doesn't compile again unless a setting changed.
        var writeTime = project.IntermediateAssemblyWriteTime;
        result = project.Build();
        Assert.AreEqual("RN0002", result.Diagnostics.Single().Code, result.Output);
        Assert.AreEqual(writeTime, project.IntermediateAssemblyWriteTime);

        // Settings are compared by value, so an edit is noticed even if the file keeps an old timestamp.
        project.WriteFodyWeavers("FodyWeavers.xml", """<RuntimeNullables CheckOutputs="0" />""");
        File.SetLastWriteTimeUtc(file, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.IsTrue(project.Build().Succeeded);
        Assert.AreEqual(AppResult.Default, project.Run());

        project.WriteFodyWeavers("FodyWeavers.xml", """<RuntimeNullables CheckOutputs="1" />""");
        Assert.IsTrue(project.Build().Succeeded);
        Assert.AreEqual(AppResult.OutputsChecked, project.Run());

        project.DeleteFile("FodyWeavers.xml");
        result = project.Build();
        Assert.AreEqual(0, result.Diagnostics.Count, result.Output);
        Assert.AreEqual(AppResult.Default, project.Run());
    }

    [TestMethod]
    [DataRow("", "..", true)]
    [DataRow("-p:SolutionDir={0}/Other/", "../Other", true)]
    [DataRow("-p:SolutionDir={0}/Other/;NCrunchOriginalSolutionDir={0}/NCrunch/", "../NCrunch", true)]
    [DataRow("-p:SolutionDir=*Undefined*", "..", true)]
    [DataRow("-p:SolutionDir={0}/App/", ".", true)]
    [DataRow("-p:SolutionDir={0}/Missing/", "..", false)]
    [DataRow("-p:SolutionDir={0}/Inva|id/", "..", false)]
    public void SolutionFodyWeaversIsFoundLikeFodyFindsIt(string argument, string directory, bool expectFound)
    {
        // The last two rows set a solution directory that doesn't exist (or can't), so the file in the default one (the parent) isn't used either.

        using var project = new TestProject(nameof(SolutionFodyWeaversIsFoundLikeFodyFindsIt));
        string file = project.WriteFodyWeavers(Path.Combine(directory, "FodyWeavers.xml"), """<RuntimeNullables CheckOutputs="true" />""");

        var result = project.Build(string.Format(argument, project.SolutionDirectory));
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(expectFound ? AppResult.OutputsChecked : AppResult.Default, project.Run());

        if (expectFound)
            Assert.AreEqual(file, result.Single("RN0002").File);
        else
            Assert.AreEqual(0, result.Diagnostics.Count, result.Output);
    }

    [TestMethod]
    public void WeaverConfigurationPropertyTakesPrecedence()
    {
        using var project = new TestProject(nameof(WeaverConfigurationPropertyTakesPrecedence));
        project.WriteFodyWeavers("FodyWeavers.xml", """<RuntimeNullables CheckOutputs="false" />""");

        var result = project.Build("-p:WeaverConfiguration=<Weavers><RuntimeNullables CheckOutputs='true' /></Weavers>");
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(AppResult.OutputsChecked, project.Run());

        var warning = result.Single("RN0002");
        Assert.AreEqual(project.ProjectFile, warning.File);
        StringAssert.Contains(warning.Message, "in the WeaverConfiguration property");
    }

    [TestMethod]
    public void PropertyOverridesLegacySetting()
    {
        using var project = new TestProject(nameof(PropertyOverridesLegacySetting));
        project.WriteFodyWeavers("FodyWeavers.xml", """<RuntimeNullables CheckOutputs="true" />""");

        var result = project.Build("-p:RuntimeNullablesCheckOutputs=false");
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(AppResult.Default, project.Run());
        StringAssert.Contains(result.Single("RN0002").Message, "isn't needed anymore");
    }

    [TestMethod]
    [DataRow("-warnaserror", true, "warning RN0002")]
    [DataRow("-p:TreatWarningsAsErrors=true", true, "warning RN0002")]
    [DataRow("-p:WarningsAsErrors=RN0002", false, "error RN0002")]
    public void MigrationWarningOnlyFailsTheBuildWhenListedInWarningsAsErrors(string argument, bool expectSuccess, string expected)
    {
        using var project = new TestProject(nameof(MigrationWarningOnlyFailsTheBuildWhenListedInWarningsAsErrors));
        project.WriteFodyWeavers("FodyWeavers.xml", "<RuntimeNullables />");

        var result = project.Build(argument);
        Assert.AreEqual(expectSuccess, result.Succeeded, result.Output);
        CollectionAssert.AreEqual(new[] { expected }, result.ToolDiagnostics, result.Output);
    }

    [TestMethod]
    public void MigrationWarningIsReportedForEachTargetFramework()
    {
        using var project = new TestProject(nameof(MigrationWarningIsReportedForEachTargetFramework));
        project.WriteProject(targetFrameworks: "net10.0;net11.0");
        project.WriteFodyWeavers("FodyWeavers.xml", "<RuntimeNullables />");

        var result = project.Build();
        Assert.IsTrue(result.Succeeded, result.Output);
        CollectionAssert.AreEqual(new[] { "warning RN0002", "warning RN0002" }, result.ToolDiagnostics, result.Output);
    }

    [TestMethod]
    public void InvalidLegacyValueFailsBeforeCompiling()
    {
        using var project = new TestProject(nameof(InvalidLegacyValueFailsBeforeCompiling));
        string file = project.WriteFodyWeavers("FodyWeavers.xml", """<RuntimeNullables CheckOutputs="yes" />""");

        var result = project.Build();
        Assert.IsFalse(result.Succeeded, result.Output);

        var error = result.Single("RN1002");
        Assert.AreEqual((file, 2, 21), (error.File, error.Line, error.Column));
        Assert.IsNull(project.IntermediateAssemblyHash);
    }

    [TestMethod]
    public void UnreadableFodyWeaversIsReported()
    {
        using var project = new TestProject(nameof(UnreadableFodyWeaversIsReported));
        string file = project.WriteFile("FodyWeavers.xml", "<Weavers>\n  <RuntimeNullables CheckOutputs=\"true\">\n</Weavers>\n");

        var result = project.Build();
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(AppResult.Default, project.Run());
        var warning = result.Single("RN0005");
        Assert.AreEqual((file, 3, 3), (warning.File, warning.Line, warning.Column));

        result = project.Build("-p:TreatWarningsAsErrors=true");
        Assert.IsFalse(result.Succeeded, result.Output);
        CollectionAssert.AreEqual(new[] { "error RN0005" }, result.ToolDiagnostics, result.Output);
    }

    [TestMethod]
    public void LiveUnitTestingBuildUsesLegacySettings()
    {
        using var project = new TestProject(nameof(LiveUnitTestingBuildUsesLegacySettings));
        project.WriteFodyWeavers("FodyWeavers.xml", """<RuntimeNullables CheckOutputs="true" CheckNonPublic="false" />""");

        var result = project.Build("-v:d", "-p:DesignTimeBuild=true", "-p:BuildingForLiveUnitTesting=true");
        Assert.IsTrue(result.Succeeded, result.Output);
        StringAssert.Contains(result.Output, "Starting injection - CheckOutputs: True, CheckNonPublic: False.");
    }
}
