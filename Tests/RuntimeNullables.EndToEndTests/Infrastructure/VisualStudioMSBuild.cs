using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RuntimeNullables.EndToEndTests.Infrastructure;

/// <summary>
/// The MSBuild of a Visual Studio installation, which runs on .NET Framework rather than on .NET like the .NET SDK's, and the newest installed .NET SDK
/// it can use (<see langword="null"/> if none can target .NET 8).
/// </summary>
internal sealed partial record VisualStudioMSBuild(string DisplayName, string Path, Version Version, string? SdkVersion)
{
    private const int MinimumSdkMajorVersion = 8;

    /// <summary>
    /// Finds the MSBuild of every Visual Studio installation, or none if the Visual Studio installer isn't there (e.g. on Linux).
    /// </summary>
    public static IReadOnlyList<VisualStudioMSBuild> FindAll()
    {
        string vswherePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");

        if (!OperatingSystem.IsWindows() || !File.Exists(vswherePath))
            return [];

        string vswhereOutput = Run(vswherePath, "-all", "-prerelease", "-products", "*", "-requires", "Microsoft.Component.MSBuild", "-format", "json");
        var sdks = FindSdks();
        var result = new List<VisualStudioMSBuild>();

        foreach (var installation in JsonDocument.Parse(vswhereOutput).RootElement.EnumerateArray())
        {
            string msbuildPath = System.IO.Path.Combine(installation.GetProperty("installationPath").GetString()!, "MSBuild", "Current", "Bin", "MSBuild.exe");

            if (!File.Exists(msbuildPath))
                continue;

            var fileVersion = FileVersionInfo.GetVersionInfo(msbuildPath);
            var version = new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart);

            // Prefer release SDKs, like Visual Studio does unless previews are turned on.
            string? sdkVersion = sdks
                .Where(s => s.MinimumMSBuildVersion <= version)
                .OrderBy(s => s.IsPrerelease ? 0 : 1)
                .ThenBy(s => s.Version)
                .LastOrDefault()?.Name;

            result.Add(new VisualStudioMSBuild(installation.GetProperty("displayName").GetString()!, msbuildPath, version, sdkVersion));
        }

        return result;
    }

    public override string ToString() => $"{DisplayName} (MSBuild {Version}, .NET SDK {SdkVersion ?? "none"})";

    /// <summary>
    /// Finds the installed .NET SDKs that can target .NET 8, with the oldest MSBuild version each one supports.
    /// </summary>
    private static List<Sdk> FindSdks()
    {
        var sdks = new List<Sdk>();

        foreach (string line in Run("dotnet", "--list-sdks").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // E.g. "10.0.401 [C:\Program Files\dotnet\sdk]" or "11.0.100-rc.2.26504.105 [C:\Program Files\dotnet\sdk]".
            var match = SdkLineRegex().Match(line);

            if (!match.Success || Version.Parse(match.Groups["version"].Value) is not { Major: >= MinimumSdkMajorVersion } version)
                continue;

            string name = match.Groups["name"].Value;
            string minimumMSBuildVersionFile = System.IO.Path.Combine(match.Groups["directory"].Value, name, "minimumMSBuildVersion");

            if (File.Exists(minimumMSBuildVersionFile))
            {
                var minimumMSBuildVersion = Version.Parse(File.ReadAllText(minimumMSBuildVersionFile).Trim());
                sdks.Add(new Sdk(name, version, match.Groups["prerelease"].Success, minimumMSBuildVersion));
            }
        }

        return sdks;
    }

    private static string Run(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName) { RedirectStandardOutput = true, UseShellExecute = false };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }

    [GeneratedRegex(@"^(?<name>(?<version>\d+\.\d+\.\d+)(?<prerelease>-\S+)?) \[(?<directory>.+)\]$")]
    private static partial Regex SdkLineRegex();

    private sealed record Sdk(string Name, Version Version, bool IsPrerelease, Version MinimumMSBuildVersion);
}
