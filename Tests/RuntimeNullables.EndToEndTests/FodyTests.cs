using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.EndToEndTests.Infrastructure;

namespace RuntimeNullables.EndToEndTests;

/// <summary>
/// Tests projects that also use Fody, with the PropertyChanged.Fody add-in.
/// </summary>
[TestClass]
public class FodyTests
{
    private const string Fody = "Fody/6.9.3";
    private const string PropertyChangedFody = "PropertyChanged.Fody/4.1.0";

    // The message about the old package names the package that replaces it (the build file gets the ID from its own file name).
    private const string OldPackageInstalled = $"since the {TestEnvironment.PackageId} package replaces it.";

    // Fody's PropertyChanged add-in raises the event from the property setter, and RuntimeNullables checks the setter's argument.
    private const string PersonSource = """
        using System.ComponentModel;

        public class Person : INotifyPropertyChanged
        {
        #pragma warning disable CS0067 // Raised by code that Fody injects.
            public event PropertyChangedEventHandler? PropertyChanged;
        #pragma warning restore CS0067

            public string Name { get; set; } = string.Empty;

            public static string Test()
            {
                var person = new Person();
                string? changed = null;
                person.PropertyChanged += (_, e) => changed = e.PropertyName;
                person.Name = "Name";

                try
                {
                    person.Name = null!;
                    return $"{changed} none";
                }
                catch (System.ArgumentNullException)
                {
                    return $"{changed} ArgumentNullException";
                }
            }
        }
        """;

    [TestMethod]
    public void FodyAndRuntimeNullablesBothWeave()
    {
        using var project = CreateProject(nameof(FodyAndRuntimeNullablesBothWeave));

        var result = project.Build();
        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.AreEqual(0, result.ToolDiagnostics.Length, result.Output);
        Assert.AreEqual("Name ArgumentNullException", RunPersonTest(project));

        var writeTime = project.IntermediateAssemblyWriteTime;
        Assert.IsTrue(project.Build().Succeeded);
        Assert.AreEqual(writeTime, project.IntermediateAssemblyWriteTime, "A build without changes doesn't compile again.");
    }

    [TestMethod]
    public void FodyFailureMakesTheNextBuildCompileAgain()
    {
        // RuntimeNullables writes its stamp after Fody ran, so the build doesn't find the assembly that Fody didn't finish up to date.

        using var project = CreateProject(nameof(FodyFailureMakesTheNextBuildCompileAgain));
        Assert.IsTrue(project.Build().Succeeded);

        project.WriteFodyWeavers("FodyWeavers.xml", "<PropertyChanged />", "<NotInstalled />");

        for (int i = 0; i < 2; i++)
        {
            var result = project.Build();
            Assert.IsFalse(result.Succeeded, result.Output);
            CollectionAssert.AreEqual(new[] { "error Fody" }, result.ToolDiagnostics, result.Output);
        }

        project.WriteFodyWeavers("FodyWeavers.xml", "<PropertyChanged />");
        Assert.IsTrue(project.Build().Succeeded);
        Assert.AreEqual("Name ArgumentNullException", RunPersonTest(project));
    }

    [TestMethod]
    [DataRow("", "error RN1004")]
    [DataRow("-p:RuntimeNullablesEnabled=false", "error RN1004")]
    [DataRow("-p:DisableFody=true", "warning RN0002")]
    public void LegacyElementInTheProjectFodyWeaversIsAnErrorWhileFodyRuns(string arguments, string expected)
    {
        using var project = CreateProject(nameof(LegacyElementInTheProjectFodyWeaversIsAnErrorWhileFodyRuns));
        project.WriteFodyWeavers("FodyWeavers.xml", "<PropertyChanged />", "<RuntimeNullables />");

        var result = project.Build(arguments);
        Assert.AreEqual(expected.StartsWith("warning"), result.Succeeded, result.Output);
        CollectionAssert.AreEqual(new[] { expected }, result.ToolDiagnostics, result.Output);
    }

    [TestMethod]
    public void LegacyElementInWeaverConfigurationIsUsedWhileFodyRuns()
    {
        // Fody allows elements for add-ins that aren't installed in the WeaverConfiguration property.

        using var project = CreateProject(nameof(LegacyElementInWeaverConfigurationIsUsedWhileFodyRuns));

        var result = project.Build("-p:WeaverConfiguration=<Weavers><PropertyChanged /><RuntimeNullables CheckOutputs='true' /></Weavers>");
        Assert.IsTrue(result.Succeeded, result.Output);
        CollectionAssert.AreEqual(new[] { "warning RN0002" }, result.ToolDiagnostics, result.Output);
        Assert.AreEqual(AppResult.OutputsChecked, project.Run());
        Assert.AreEqual("Name ArgumentNullException", RunPersonTest(project), "Fody ran too.");
    }

    [TestMethod]
    [DataRow(true, "RuntimeNullables.Fody/2.1.0", false, OldPackageInstalled)]
    [DataRow(false, null, false, "The Fody package is installed without any Fody add-ins")]
    [DataRow(true, "RuntimeNullables.Fody/2.1.0", true, OldPackageInstalled)]
    public void FodySetupThatDoesNotWorkAfterMigratingIsReported(bool withPropertyChanged, string? otherPackage, bool withLegacyElement, string expected)
    {
        using var project = new TestProject(nameof(FodySetupThatDoesNotWorkAfterMigratingIsReported));
        string[] packages = new[] { Fody, withPropertyChanged ? PropertyChangedFody : null, otherPackage }.OfType<string>().ToArray();
        project.WriteProject(otherPackages: packages);

        // The old package is reported first, and the build stops there.
        if (withLegacyElement)
            project.WriteFodyWeavers("FodyWeavers.xml", "<PropertyChanged />", "<RuntimeNullables />");

        var result = project.Build();
        Assert.IsFalse(result.Succeeded, result.Output);
        CollectionAssert.AreEqual(new[] { "error RN1004" }, result.ToolDiagnostics, result.Output);
        StringAssert.Contains(result.Single("RN1004").Message, expected);
    }

    private static TestProject CreateProject(string name)
    {
        var project = new TestProject(name);
        project.WriteProject(otherPackages: [Fody, PropertyChangedFody]);
        project.WriteFodyWeavers("FodyWeavers.xml", "<PropertyChanged />");
        project.WriteFile("Person.cs", PersonSource);
        project.WriteFile("Program.cs", TestProject.ProgramSource.Replace(
            "Console.WriteLine(Check(() => Api.BadReturn()));", "Console.WriteLine(Check(() => Api.BadReturn()));\nConsole.WriteLine(Person.Test());"));

        return project;
    }

    private static string RunPersonTest(TestProject project) => project.RunLines(4)[3];
}
