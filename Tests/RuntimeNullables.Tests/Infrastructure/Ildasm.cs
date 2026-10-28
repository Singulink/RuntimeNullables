using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Disassembles types with the Windows SDK's <c>ildasm.exe</c>. The output is filtered like FodyHelpers' <c>Ildasm.Decompile</c>, so the IL
/// snapshots recorded with it before the move away from Fody still match.
/// </summary>
internal static class Ildasm
{
    // Before .NET 10, the framework doesn't have the nullable and extension marker attributes, so the compiler adds its own copies of them to the
    // assembly. The framework's are shown like the copies, without the assembly name (other attributes from other assemblies are left out), so every
    // target framework has the same snapshots.
    private static readonly Regex _frameworkAttributePrefix = new(
        @"^\.custom instance void \[[^\]]+\](?=System\.Runtime\.CompilerServices\.(Nullable|ExtensionMarker))", RegexOptions.CultureInvariant);

    private static readonly Lazy<string> _ildasmPath = new(() => WindowsSdkTools.TryFind("ildasm", out string? path) ?
        path : throw new FileNotFoundException("Could not find ildasm.exe in the Windows SDK."));

    public static string Decompile(string assemblyPath, string typeName)
    {
        var startInfo = new ProcessStartInfo(_ildasmPath.Value, $"\"{assemblyPath}\" /text /linenum /source /item:{typeName}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start ildasm.exe.");
        var output = new StringBuilder();

        while (process.StandardOutput.ReadLine() is { } rawLine)
        {
            string line = _frameworkAttributePrefix.Replace(rawLine.Trim(), ".custom instance void ");

            if (line.Length == 0 || line.Contains(".line ", StringComparison.Ordinal) || line.Contains(".custom instance void [", StringComparison.Ordinal) ||
                line.StartsWith("// ", StringComparison.Ordinal) || line.StartsWith("//0", StringComparison.Ordinal) ||
                line.Contains(".language '", StringComparison.Ordinal))
            {
                continue;
            }

            output.AppendLine(line.StartsWith("} // end of ", StringComparison.Ordinal) ? "}" : line);
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"ildasm.exe failed with exit code {process.ExitCode}.");

        return output.ToString();
    }
}
