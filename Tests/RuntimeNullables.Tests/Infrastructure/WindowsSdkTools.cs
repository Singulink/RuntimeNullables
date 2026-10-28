// Based on FodyHelpers' Testing/SdkToolFinder.cs (https://github.com/Fody/Fody/blob/master/FodyHelpers/Testing/SdkToolFinder.cs), used under this license:
//
// MIT License
//
// Copyright (c) The Fody Team and contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
