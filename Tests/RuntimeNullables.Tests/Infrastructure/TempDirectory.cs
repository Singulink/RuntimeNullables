using System;
using System.IO;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// A uniquely named temporary directory that is deleted when disposed.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RuntimeNullables.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string GetFilePath(string fileName) => System.IO.Path.Combine(Path, fileName);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: a file may still be in use (e.g. by a test runner or antivirus scan).
        }
    }
}
