using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tasks;
using RuntimeNullables.Tests.Infrastructure;

namespace RuntimeNullables.Tests;

/// <summary>
/// Tests reading the <c>&lt;RuntimeNullables&gt;</c> element of Fody's configuration. The project is in the <c>Solution\Project</c> directory.
/// </summary>
[TestClass]
public class LegacyConfigurationTests
{
    private const string SolutionFile = @"Solution\FodyWeavers.xml";
    private const string ProjectFile = @"Solution\Project\FodyWeavers.xml";

    [TestMethod]
    public void LegacySettingsAreReadWithAMigrationWarning()
    {
        const string Configuration = """
            <Weavers>
              <RuntimeNullables CheckOutputs="false" CheckNonPublic=" 0 " />
            </Weavers>
            """;

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        string file = WriteFile(directory, ProjectFile, Configuration);
        task.FodyWeaversFiles = [new TaskItem(file)];

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: "false", checkNonPublic: "false");

        var warning = engine.Warnings.Single();
        Assert.AreEqual("RN0002", warning.Code);
        Assert.AreEqual(file, warning.File);
        Assert.AreEqual(2, warning.LineNumber);
        Assert.AreEqual(4, warning.ColumnNumber);
        Assert.AreEqual(
            "RuntimeNullables: The <RuntimeNullables> element in FodyWeavers.xml is legacy Fody configuration. Set these properties in the project file " +
            "instead: <RuntimeNullablesCheckOutputs>false</RuntimeNullablesCheckOutputs> " +
            "<RuntimeNullablesCheckNonPublic>false</RuntimeNullablesCheckNonPublic>. " +
            "Then remove the element (or delete FodyWeavers.xml and FodyWeavers.xsd if Fody isn't used anymore).",
            warning.Message);
    }

    [TestMethod]
    public void SolutionLevelSettingsAreSuggestedForDirectoryBuildProps()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyWeaversFiles = [WriteConfig(directory, SolutionFile, """<RuntimeNullables CheckOutputs="1" />""")];

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: "true", checkNonPublic: null);

        // Fody only creates FodyWeavers.xsd next to the project's file.
        string message = engine.Warnings.Single().Message!;
        StringAssert.Contains(message, "Set these properties in Directory.Build.props or the project files instead");
        StringAssert.EndsWith(message, "(or delete FodyWeavers.xml if Fody isn't used anymore).");
    }

    [TestMethod]
    public void PropertiesOverrideLegacySettings()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyWeaversFiles = [WriteConfig(directory, ProjectFile, """<RuntimeNullables CheckOutputs="false" CheckNonPublic="false" />""")];
        task.CheckOutputs = "TRUE";

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: null, checkNonPublic: "false");

        string message = engine.Warnings.Single().Message!;
        StringAssert.Contains(message, "instead: <RuntimeNullablesCheckNonPublic>false</RuntimeNullablesCheckNonPublic>. Then remove the element");
        Assert.IsFalse(message.Contains("RuntimeNullablesCheckOutputs"), message);
    }

    [TestMethod]
    [DataRow("<RuntimeNullables />", null)]
    [DataRow("""<RuntimeNullables Unknown="x" />""", null)]
    [DataRow("""<RuntimeNullables CheckOutputs="yes" CheckNonPublic="true" />""", "false")]
    public void LegacyElementWithoutUsedSettingsIsReportedAsNotNeeded(string element, string? properties)
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyWeaversFiles = [WriteConfig(directory, ProjectFile, element)];
        task.CheckOutputs = properties;
        task.CheckNonPublic = properties;

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: null, checkNonPublic: null);

        var warning = engine.Warnings.Single();
        Assert.AreEqual("RN0002", warning.Code);
        StringAssert.Contains(warning.Message, "is legacy Fody configuration that isn't needed anymore. Remove it");
    }

    [TestMethod]
    public void LaterConfigurationReplacesTheWholeElement()
    {
        // Fody doesn't merge elements: the project's element replaces the solution's, including the settings it doesn't set.

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        var solutionFile = WriteConfig(directory, SolutionFile, """<RuntimeNullables CheckOutputs="false" CheckNonPublic="false" />""");
        var projectFile = WriteConfig(directory, ProjectFile, """<RuntimeNullables CheckNonPublic="true" />""");
        task.FodyWeaversFiles = [solutionFile, projectFile];

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: null, checkNonPublic: "true");
        Assert.AreEqual(projectFile.ItemSpec, engine.Warnings.Single().File);
    }

    [TestMethod]
    public void ConfigurationWithoutTheElementDoesNotReplaceEarlierConfiguration()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        var solutionFile = WriteConfig(directory, SolutionFile, """<RuntimeNullables CheckOutputs="false" />""");
        var projectFile = WriteConfig(directory, ProjectFile, "<PropertyChanged />");
        task.FodyWeaversFiles = [solutionFile, projectFile];
        task.WeaverConfiguration = "<Weavers><PropertyChanged /></Weavers>";

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: "false", checkNonPublic: null);
        Assert.AreEqual(solutionFile.ItemSpec, engine.Warnings.Single().File);
    }

    [TestMethod]
    public void WeaverConfigurationPropertyTakesPrecedenceOverFiles()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyWeaversFiles = [WriteConfig(directory, ProjectFile, """<RuntimeNullables CheckOutputs="false" />""")];
        task.WeaverConfiguration = """<Weavers><RuntimeNullables CheckNonPublic="0" /></Weavers>""";

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: null, checkNonPublic: "false");

        var warning = engine.Warnings.Single();
        Assert.AreEqual(task.ProjectPath, warning.File);
        Assert.AreEqual(0, warning.LineNumber);
        Assert.AreEqual(
            "RuntimeNullables: The <RuntimeNullables> element in the WeaverConfiguration property is legacy Fody configuration. Set these properties in " +
            "the file that sets the WeaverConfiguration property instead: <RuntimeNullablesCheckNonPublic>false</RuntimeNullablesCheckNonPublic>. Then " +
            "remove the element (or delete the whole property if Fody isn't used anymore).",
            warning.Message);
    }

    [TestMethod]
    public void LastElementIsMatchedCaseInsensitivelyIgnoringNamespaces()
    {
        const string Configuration = """
            <Weavers xmlns="urn:test">
              <RuntimeNullables CheckOutputs="true" />
              <runtimenullables CheckOutputs="false" />
            </Weavers>
            """;

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyWeaversFiles = [new TaskItem(WriteFile(directory, ProjectFile, Configuration))];

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: "false", checkNonPublic: null);
        Assert.AreEqual(3, engine.Warnings.Single().LineNumber);
    }

    [TestMethod]
    public void OnlyChildrenOfTheRootAreMatched()
    {
        const string Configuration = """<RuntimeNullables CheckOutputs="false"><X><RuntimeNullables CheckOutputs="false" /></X></RuntimeNullables>""";

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyWeaversFiles = [new TaskItem(WriteFile(directory, ProjectFile, Configuration))];

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: null, checkNonPublic: null);
        Assert.AreEqual(0, engine.Warnings.Count);
    }

    [TestMethod]
    [DataRow("True")]
    [DataRow("yes")]
    [DataRow("")]
    public void InvalidLegacyValueIsAnErrorAtTheAttribute(string value)
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        var file = WriteConfig(directory, ProjectFile, $"""<RuntimeNullables CheckOutputs="{value}" />""");
        task.FodyWeaversFiles = [file];

        Assert.IsFalse(task.Execute());

        var error = engine.Errors.Single();
        Assert.AreEqual("RN1002", error.Code);
        Assert.AreEqual(file.ItemSpec, error.File);
        Assert.AreEqual(2, error.LineNumber);
        Assert.AreEqual(21, error.ColumnNumber);
        StringAssert.Contains(error.Message, $"Invalid value '{value}' for the CheckOutputs attribute");
        StringAssert.EndsWith(error.Message, "Set the RuntimeNullablesCheckOutputs property to 'true' or 'false' instead and remove the element.");
        Assert.AreEqual(0, engine.Warnings.Count);
    }

    [TestMethod]
    public void EveryInvalidLegacyValueIsReported()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.WeaverConfiguration = """<Weavers><RuntimeNullables CheckOutputs="yes" CheckNonPublic="no" /></Weavers>""";

        Assert.IsFalse(task.Execute());
        Assert.AreEqual(2, engine.Errors.Count);
        Assert.IsTrue(engine.Errors.All(e => e.Code == "RN1002" && e.File == task.ProjectPath && e.LineNumber == 0), "Errors are reported at the project.");
        const string Location = "attribute of the legacy Fody configuration element <RuntimeNullables> in the WeaverConfiguration property.";
        StringAssert.Contains(engine.Errors[0].Message, "CheckOutputs " + Location);
        StringAssert.Contains(engine.Errors[1].Message, "CheckNonPublic " + Location);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UnreadableConfigurationIsReportedAndIgnored(bool inProperty)
    {
        const string Configuration = """
            <Weavers>
              <RuntimeNullables CheckOutputs="false">
            </Weavers>
            """;

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        string? file = null;

        if (inProperty)
        {
            task.WeaverConfiguration = Configuration;
        }
        else
        {
            file = WriteFile(directory, ProjectFile, Configuration);
            task.FodyWeaversFiles = [new TaskItem(file)];
        }

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: null, checkNonPublic: null);

        var warning = engine.Warnings.Single();
        Assert.AreEqual("RN0005", warning.Code);
        Assert.AreEqual(file ?? task.ProjectPath, warning.File);
        Assert.AreEqual(inProperty ? 0 : 3, warning.LineNumber);
        StringAssert.Contains(warning.Message, "can't be read, so any RuntimeNullables settings in it are ignored. Fix or remove it: ");
    }

    [TestMethod]
    [DataRow(false, null, null, false)]
    [DataRow(true, null, null, true)]
    [DataRow(true, "RN0005", null, false)]
    [DataRow(true, null, "CS0168, rn0005", false)]
    public void UnreadableConfigurationWarningFollowsWarningSettings(bool treatWarningsAsErrors, string? noWarn, string? warningsNotAsErrors, bool expectError)
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.WeaverConfiguration = "<Weavers>";
        task.TreatWarningsAsErrors = treatWarningsAsErrors;
        task.NoWarn = noWarn;
        task.WarningsNotAsErrors = warningsNotAsErrors;

        Assert.AreEqual(!expectError, task.Execute());
        Assert.AreEqual("RN0005", expectError ? engine.Errors.Single().Code : engine.Warnings.Single().Code);
    }

    [TestMethod]
    public void LockedFileIsReportedAsUnreadable()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        var file = WriteConfig(directory, ProjectFile, """<RuntimeNullables CheckOutputs="false" />""");
        task.FodyWeaversFiles = [file];

        using (new FileStream(file.ItemSpec, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            AssertSucceeds(task, engine);

        AssertSettings(task, checkOutputs: null, checkNonPublic: null);

        var warning = engine.Warnings.Single();
        Assert.AreEqual("RN0005", warning.Code);
        Assert.AreEqual(file.ItemSpec, warning.File);
        Assert.AreEqual(0, warning.LineNumber);
    }

    [TestMethod]
    public void FileOpenForWritingIsRead()
    {
        // Fody allows other processes to write the file while it reads it, e.g. Fody in a parallel build updating its schema reference.

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        var file = WriteConfig(directory, ProjectFile, """<RuntimeNullables CheckOutputs="false" />""");
        task.FodyWeaversFiles = [file];

        using (new FileStream(file.ItemSpec, FileMode.Open, FileAccess.Write, FileShare.Read))
            AssertSucceeds(task, engine);

        AssertSettings(task, checkOutputs: "false", checkNonPublic: null);
    }

    [TestMethod]
    public void MissingFilesAndWhitespacePropertyAreIgnored()
    {
        // Files can be deleted after the build found them, e.g. when Visual Studio reuses an earlier evaluation of the project.

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyWeaversFiles = [new TaskItem(directory.GetFilePath(SolutionFile)), new TaskItem(directory.GetFilePath(@"Missing\FodyWeavers.xml"))];
        task.WeaverConfiguration = " \r\n ";

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: null, checkNonPublic: null);
        Assert.AreEqual(0, engine.Warnings.Count);
    }

    [TestMethod]
    [DataRow("UTF-8 declared as UTF-16", true, false)]
    [DataRow("UTF-8 with a byte order mark declared as UTF-16", true, true)]
    [DataRow("Latin-1 without a declaration", false, false)]
    public void FilesAreDecodedLikeFodyDecodesThem(string description, bool declareUtf16, bool byteOrderMark)
    {
        // Fody reads files as UTF-8 unless they have a byte order mark, ignoring the encoding in the XML declaration.

        const string NonAsciiText = "Caf\u00E9";
        string declaration = declareUtf16 ? """<?xml version="1.0" encoding="utf-16"?>""" : string.Empty;
        string text = $"""{declaration}<Weavers><!-- {NonAsciiText} --><RuntimeNullables CheckOutputs="false" /></Weavers>""";
        var encoding = declareUtf16 ? new UTF8Encoding(byteOrderMark) : Encoding.GetEncoding("ISO-8859-1");

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        string file = WriteFile(directory, ProjectFile, string.Empty);
        File.WriteAllBytes(file, [.. encoding.GetPreamble(), .. encoding.GetBytes(text)]);
        task.FodyWeaversFiles = [new TaskItem(file)];

        AssertSucceeds(task, engine);
        AssertSettings(task, checkOutputs: "false", checkNonPublic: null);
        Assert.AreEqual("RN0002", engine.Warnings.Single().Code, description);
    }

    [TestMethod]
    [DataRow("<RuntimeNullables />", "Remove it.")]
    [DataRow(
        """<RuntimeNullables CheckOutputs="true" />""",
        "Set these properties in the project file instead: <RuntimeNullablesCheckOutputs>true</RuntimeNullablesCheckOutputs>. Then remove the element.")]
    public void ProjectLevelElementIsAnErrorWhileFodyRuns(string element, string expectedEnd)
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        var file = WriteConfig(directory, ProjectFile, element);
        task.FodyWeaversFiles = [file];
        task.FodyEnabled = true;

        Assert.IsFalse(task.Execute());

        var error = engine.Errors.Single();
        Assert.AreEqual("RN1004", error.Code);
        Assert.AreEqual(file.ItemSpec, error.File);
        Assert.AreEqual(2, error.LineNumber);
        StringAssert.Contains(error.Message, "Fody fails the build while it is there since RuntimeNullables isn't a Fody add-in anymore.");
        StringAssert.EndsWith(error.Message, expectedEnd);
        Assert.AreEqual(0, engine.Warnings.Count);
    }

    [TestMethod]
    public void ConflictIsReportedAlongWithInvalidValues()
    {
        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyWeaversFiles = [WriteConfig(directory, ProjectFile, """<RuntimeNullables CheckOutputs="yes" />""")];
        task.FodyEnabled = true;

        Assert.IsFalse(task.Execute());
        CollectionAssert.AreEqual(new[] { "RN1002", "RN1004" }, engine.Errors.Select(e => e.Code).ToArray());
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void ElementOutsideTheProjectFileIsOnlyAWarningWhileFodyRuns(bool inProperty, bool alsoInProjectFile)
    {
        // Fody allows elements for add-ins that aren't installed in the solution's FodyWeavers.xml and in the WeaverConfiguration property, which
        // takes precedence over the project's file.

        using var directory = new TempDirectory();
        var (task, engine) = CreateTask(directory);
        task.FodyEnabled = true;

        if (inProperty)
            task.WeaverConfiguration = "<Weavers><RuntimeNullables /></Weavers>";
        else
            task.FodyWeaversFiles = [WriteConfig(directory, SolutionFile, "<RuntimeNullables />")];

        if (alsoInProjectFile)
            task.FodyWeaversFiles = [.. task.FodyWeaversFiles, WriteConfig(directory, ProjectFile, "<RuntimeNullables />")];

        AssertSucceeds(task, engine);
        Assert.AreEqual("RN0002", engine.Warnings.Single().Code);
    }

    private static (ReadLegacyFodyConfiguration Task, BuildEngineStub Engine) CreateTask(TempDirectory directory)
    {
        var engine = new BuildEngineStub();
        var task = new ReadLegacyFodyConfiguration
        {
            BuildEngine = engine,
            ProjectPath = directory.GetFilePath(@"Solution\Project\Project.csproj"),
        };

        return (task, engine);
    }

    private static void AssertSucceeds(ReadLegacyFodyConfiguration task, BuildEngineStub engine)
    {
        Assert.IsTrue(task.Execute(), string.Join(Environment.NewLine, engine.Errors.Select(e => e.Message)));
        Assert.AreEqual(0, engine.Errors.Count);
    }

    /// <summary>
    /// Checks the legacy settings the task returned, where <see langword="null"/> means not set.
    /// </summary>
    private static void AssertSettings(ReadLegacyFodyConfiguration task, string? checkOutputs, string? checkNonPublic)
    {
        Assert.AreEqual(checkOutputs ?? string.Empty, task.LegacyCheckOutputs, nameof(task.LegacyCheckOutputs));
        Assert.AreEqual(checkNonPublic ?? string.Empty, task.LegacyCheckNonPublic, nameof(task.LegacyCheckNonPublic));
    }

    /// <summary>
    /// Writes a Fody configuration file with one element in the second line.
    /// </summary>
    private static ITaskItem WriteConfig(TempDirectory directory, string relativePath, string element)
    {
        return new TaskItem(WriteFile(directory, relativePath, $"<Weavers>{Environment.NewLine}  {element}{Environment.NewLine}</Weavers>"));
    }

    private static string WriteFile(TempDirectory directory, string relativePath, string contents)
    {
        string path = directory.GetFilePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }
}
