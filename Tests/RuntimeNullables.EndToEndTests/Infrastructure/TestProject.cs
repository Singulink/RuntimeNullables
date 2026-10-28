using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.EndToEndTests.Infrastructure;

/// <summary>
/// A console app that references the RuntimeNullables package, in its own directory (<see cref="SolutionDirectory"/>) so that tests can add files
/// next to it, e.g. a solution-level <c>FodyWeavers.xml</c>. Its program reports which null checks were injected (see <see cref="AppResult"/>).
/// </summary>
internal sealed class TestProject : IDisposable
{
    public const string ProgramSource = """
        using System;
        using RuntimeNullables;

        Console.WriteLine(Check(() => Api.Echo(null!)));
        Console.WriteLine(Check(() => Api.Unchecked(null!)));
        Console.WriteLine(Check(() => Api.BadReturn()));

        static string Check(Action action)
        {
            try
            {
                action();
                return "none";
            }
            catch (Exception ex)
            {
                return ex.GetType().Name;
            }
        }

        public static class Api
        {
            public static string Echo(string value) => value;

            [NullChecks(false)]
            public static string Unchecked(string value) => value;

            public static string BadReturn() => null!;
        }
        """;

    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(5);

    private string _targetFramework = "net10.0";

    public TestProject(string name)
    {
        SolutionDirectory = Path.Combine(TestEnvironment.RootDirectory, $"{name}-{Guid.NewGuid().ToString("N")[..8]}");
        ProjectDirectory = Path.Combine(SolutionDirectory, "App");
        Directory.CreateDirectory(ProjectDirectory);

        WriteProject();
        WriteFile("Program.cs", ProgramSource);
    }

    /// <summary>
    /// Gets the directory that contains the project's directory, which Fody treats as the solution directory by default.
    /// </summary>
    public string SolutionDirectory { get; }

    public string ProjectDirectory { get; }

    public string ProjectFile => Path.Combine(ProjectDirectory, "App.csproj");

    /// <summary>
    /// Gets the output directory of the project's first target framework.
    /// </summary>
    public string OutputDirectory => Path.Combine(ProjectDirectory, "bin", "Release", _targetFramework);

    public string IntermediateAssemblyFile => Path.Combine(ProjectDirectory, "obj", "Release", _targetFramework, "App.dll");

    /// <summary>
    /// Gets a hash of the compiled assembly in the intermediate output directory, or <see langword="null"/> if it doesn't exist, to compare its
    /// contents.
    /// </summary>
    public string? IntermediateAssemblyHash
    {
        get {
            return File.Exists(IntermediateAssemblyFile) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(IntermediateAssemblyFile))) : null;
        }
    }

    /// <summary>
    /// Gets the time the compiled assembly in the intermediate output directory was written, to tell whether a build compiled (and wove) it again.
    /// Its contents can't tell, since compiling and weaving are deterministic.
    /// </summary>
    public DateTime IntermediateAssemblyWriteTime => File.GetLastWriteTimeUtc(IntermediateAssemblyFile);

    /// <summary>
    /// Writes the project file with the given target frameworks (the app runs on the first one), other properties (as XML), a reference to the
    /// RuntimeNullables package and the given other packages (as "ID/version").
    /// </summary>
    public void WriteProject(string targetFrameworks = "net10.0", string otherProperties = "", bool privateAssets = true, params string[] otherPackages)
    {
        _targetFramework = targetFrameworks.Split(';')[0];

        string privateAssetsAttribute = privateAssets ? " PrivateAssets=\"all\"" : string.Empty;
        var references = new StringBuilder();
        references.AppendLine(
            $"""    <PackageReference Include="{TestEnvironment.PackageId}" Version="{TestEnvironment.PackageVersion}"{privateAssetsAttribute} />""");

        foreach (string package in otherPackages)
        {
            string[] parts = package.Split('/');
            references.AppendLine($"""    <PackageReference Include="{parts[0]}" Version="{parts[1]}" PrivateAssets="all" />""");
        }

        string frameworksProperty = targetFrameworks.Contains(';') ? "TargetFrameworks" : "TargetFramework";
        string projectFile = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <{frameworksProperty}>{targetFrameworks}</{frameworksProperty}>
                <Nullable>enable</Nullable>
                {otherProperties}
              </PropertyGroup>
              <ItemGroup>
            {references}  </ItemGroup>
            </Project>
            """;

        WriteFile("App.csproj", projectFile);
    }

    /// <summary>
    /// Writes a file, relative to the project's directory.
    /// </summary>
    public string WriteFile(string relativePath, string contents)
    {
        string path = Path.GetFullPath(Path.Combine(ProjectDirectory, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    /// <summary>
    /// Writes a Fody configuration file, relative to the project's directory, with the given elements on their own lines starting at line 2.
    /// </summary>
    /// <remarks>
    /// <para>The file refers to the schema like the files Fody creates. Otherwise Fody adds the reference when it runs, and the next build compiles
    /// again because the file changed.</para>
    /// </remarks>
    public string WriteFodyWeavers(string relativePath, params string[] elements)
    {
        const string Root = """<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">""";
        return WriteFile(relativePath, Root + "\n" + string.Concat(elements.Select(e => $"  {e}\n")) + "</Weavers>\n");
    }

    public void DeleteFile(string relativePath) => File.Delete(Path.Combine(ProjectDirectory, relativePath));

    /// <summary>
    /// Writes a <c>global.json</c> file next to the project's directory that makes builds use the given .NET SDK version.
    /// </summary>
    public void WriteGlobalJson(string sdkVersion)
    {
        string globalJson = $$"""
            {
              "sdk": {
                "version": "{{sdkVersion}}",
                "rollForward": "disable",
                "allowPrerelease": true
              }
            }
            """;

        File.WriteAllText(Path.Combine(SolutionDirectory, "global.json"), globalJson);
    }

    /// <summary>
    /// Builds the project in the Release configuration (so null checks for outputs and non-public members are off by default). Empty arguments are
    /// ignored.
    /// </summary>
    public BuildResult Build(params string[] arguments)
    {
        // Like "dotnet build", but without the summary that repeats the warnings and errors.
        string[] buildArguments = ["msbuild", ProjectFile, "-restore", "-p:Configuration=Release", "-nologo", "-v:m", "-clp:NoSummary", "-tl:off", "-nr:false"];
        var (exitCode, output) = RunProcess("dotnet", [.. buildArguments, .. arguments.Where(a => a.Length > 0)]);
        return new BuildResult(exitCode, output);
    }

    /// <summary>
    /// Builds the project like <see cref="Build"/>, with the given MSBuild executable instead of the .NET SDK's (e.g. Visual Studio's).
    /// </summary>
    public BuildResult BuildWith(string msbuildPath)
    {
        string[] buildArguments = [ProjectFile, "-restore", "-p:Configuration=Release", "-nologo", "-v:m", "-clp:NoSummary", "-nr:false"];
        var (exitCode, output) = RunProcess(msbuildPath, buildArguments);
        return new BuildResult(exitCode, output);
    }

    /// <summary>
    /// Runs the built app and returns what its null checks threw.
    /// </summary>
    public AppResult Run()
    {
        string[] lines = RunLines(3);
        return new AppResult(lines[0], lines[1], lines[2]);
    }

    /// <summary>
    /// Runs the built app and returns its output lines, checking that there are at least the given number.
    /// </summary>
    public string[] RunLines(int minimumCount)
    {
        var (exitCode, output) = RunProcess("dotnet", [Path.Combine(OutputDirectory, "App.dll")]);
        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.AreEqual(0, exitCode, output);
        Assert.IsTrue(lines.Length >= minimumCount, output);
        return lines;
    }

    public void Dispose() => TestEnvironment.TryDeleteDirectory(SolutionDirectory);

    private (int ExitCode, string Output) RunProcess(string fileName, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = ProjectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        // The test host can have MSBuild variables of its own build, which would affect these builds. Builds don't use the MSBuild server, which
        // keeps task assemblies loaded across builds, while the package is extracted to the same path by every test of a run.
        foreach (string variable in startInfo.Environment.Keys.Where(k => k.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToList())
            startInfo.Environment.Remove(variable);

        startInfo.Environment["NUGET_PACKAGES"] = TestEnvironment.PackagesDirectory;
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        var output = new StringBuilder();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => AppendLine(e.Data);
        process.ErrorDataReceived += (_, e) => AppendLine(e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // This waits for the output to be read too, which a process that inherited the output pipes could hold up, so it has a timeout as well.
        using var timeout = new CancellationTokenSource(ProcessTimeout);

        try
        {
            process.WaitForExitAsync(timeout.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"'{fileName} {string.Join(' ', startInfo.ArgumentList)}' didn't finish in {ProcessTimeout}:\n{GetOutput()}");
        }

        return (process.ExitCode, GetOutput());

        void AppendLine(string? line)
        {
            if (line == null)
                return;

            lock (output)
                output.AppendLine(line);
        }

        string GetOutput()
        {
            lock (output)
                return output.ToString();
        }
    }
}

/// <summary>
/// What the test app's null checks threw: an exception type name, or "none".
/// </summary>
/// <param name="Input">The result of passing null to a public method.</param>
/// <param name="Unchecked">The result of passing null to a method with <c>[NullChecks(false)]</c>.</param>
/// <param name="Output">The result of returning null from a public method.</param>
internal sealed record AppResult(string Input, string Unchecked, string Output)
{
    public static AppResult Default { get; } = new("ArgumentNullException", "none", "none");

    public static AppResult OutputsChecked { get; } = new("ArgumentNullException", "none", "NullReferenceException");

    public static AppResult NotWoven { get; } = new("none", "none", "none");
}
