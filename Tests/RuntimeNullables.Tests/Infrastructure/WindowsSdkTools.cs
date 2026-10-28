using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Locates .NET Framework tools such as <c>ildasm.exe</c> and <c>sn.exe</c> in the Windows SDK.
/// </summary>
internal static class WindowsSdkTools
{
    private static readonly ConcurrentDictionary<string, string?> _toolPaths = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryFind(string toolName, [NotNullWhen(true)] out string? path)
    {
        path = _toolPaths.GetOrAdd(toolName, Find);
        return path != null;
    }

    private static string? Find(string toolName)
    {
        string sdkDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft SDKs", "Windows");

        if (!Directory.Exists(sdkDirectory))
            return null;

        return Directory.EnumerateFiles(sdkDirectory, toolName + ".exe", SearchOption.AllDirectories)
            .Where(p => !p.Contains("x64", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(GetFileVersion)
            .FirstOrDefault();
    }

    private static Version GetFileVersion(string path)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        return new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart);
    }
}
