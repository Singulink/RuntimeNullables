using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.EndToEndTests.Infrastructure;

/// <summary>
/// Sets up the directory the test projects are created in for this test run, isolated from the user's NuGet and MSBuild configuration: the
/// RuntimeNullables package comes from a feed that only contains the package built from this repository, other packages from nuget.org, and
/// packages are extracted to a folder of the run.
/// </summary>
[TestClass]
public static class TestEnvironment
{
    /// <summary>
    /// The ID of the package built from this repository.
    /// </summary>
    public const string PackageId = "Singulink.RuntimeNullables";

    private static readonly string BaseDirectory = Path.Combine(Path.GetTempPath(), "RuntimeNullables.EndToEndTests");

    private static string? _rootDirectory;

    /// <summary>
    /// Gets the version of the RuntimeNullables package.
    /// </summary>
    public static string PackageVersion { get; } = typeof(NullChecksAttribute).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

    /// <summary>
    /// Gets the directory the test projects are created in.
    /// </summary>
    public static string RootDirectory => _rootDirectory ?? throw new InvalidOperationException("The test environment isn't set up.");

    /// <summary>
    /// Gets the NuGet global packages folder of this run (<c>NUGET_PACKAGES</c>, which takes precedence over NuGet's configuration), which keeps the
    /// package built from this repository out of the user's and out of other runs.
    /// </summary>
    public static string PackagesDirectory => Path.Combine(RootDirectory, "packages");

    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        // Runs that were killed don't clean up after themselves.
        if (Directory.Exists(BaseDirectory))
        {
            foreach (var directory in new DirectoryInfo(BaseDirectory).GetDirectories().Where(d => d.CreationTimeUtc < DateTime.UtcNow.AddDays(-1)))
                TryDeleteDirectory(directory.FullName);
        }

        _rootDirectory = Path.Combine(BaseDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]);
        string feedDirectory = Path.Combine(_rootDirectory, "feed");
        Directory.CreateDirectory(feedDirectory);

        // Only this package, since the package directory can also contain older packages (e.g. RuntimeNullables.Fody).
        string packageFile = Path.Combine(GetPackageDirectory(), $"{PackageId}.{PackageVersion}.nupkg");
        File.Copy(packageFile, Path.Combine(feedDirectory, Path.GetFileName(packageFile)));

        // NuGet merges this with the user's configuration, so clear anything that could get in the way. Source mapping keeps a published package
        // with the same version from being used.
        string nugetConfig = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <config>
                <add key="signatureValidationMode" value="accept" />
              </config>
              <packageSources>
                <clear />
                <add key="local" value="{feedDirectory}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <disabledPackageSources>
                <clear />
              </disabledPackageSources>
              <packageSourceMapping>
                <clear />
                <packageSource key="local">
                  <package pattern="{PackageId}" />
                </packageSource>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """;

        File.WriteAllText(Path.Combine(_rootDirectory, "nuget.config"), nugetConfig);

        // Keeps the test projects from using files in directories above them, and turns off warnings that depend on the SDK or the network rather than
        // on the test (e.g. when a target framework of the test projects reaches end of support). The test projects don't need an app host or the
        // ASP.NET Core and Windows Desktop reference packs either, which saves downloads.
        string buildProps = """
            <Project>
              <PropertyGroup>
                <CheckEolTargetFramework>false</CheckEolTargetFramework>
                <NuGetAudit>false</NuGetAudit>
                <UseAppHost>false</UseAppHost>
                <DisableTransitiveFrameworkReferenceDownloads>true</DisableTransitiveFrameworkReferenceDownloads>
              </PropertyGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(_rootDirectory, "Directory.Build.props"), buildProps);
        File.WriteAllText(Path.Combine(_rootDirectory, "Directory.Build.targets"), "<Project />");
        File.WriteAllText(Path.Combine(_rootDirectory, "Directory.Packages.props"), "<Project />");
    }

    [AssemblyCleanup]
    public static void Cleanup()
    {
        if (_rootDirectory != null)
            TryDeleteDirectory(_rootDirectory);
    }

    /// <summary>
    /// Deletes a directory if it exists, retrying for a while if files are still in use (e.g. by build processes that are exiting).
    /// </summary>
    public static void TryDeleteDirectory(string path)
    {
        for (int attempt = 1; Directory.Exists(path) && attempt <= 10; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(500);
            }
        }
    }

    private static string GetPackageDirectory()
    {
        return typeof(TestEnvironment).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "PackageDirectory").Value!;
    }
}
