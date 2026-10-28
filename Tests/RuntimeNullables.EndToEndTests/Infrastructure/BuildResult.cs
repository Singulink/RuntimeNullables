using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.EndToEndTests.Infrastructure;

/// <summary>
/// The result of building a test project.
/// </summary>
internal sealed partial class BuildResult
{
    public BuildResult(int exitCode, string output)
    {
        ExitCode = exitCode;
        Output = output;
        Diagnostics = output.Split('\n')
            .Select(l => DiagnosticRegex().Match(l.TrimEnd('\r')))
            .Where(m => m.Success)
            .Select(m => new Diagnostic(m))
            .ToList();
    }

    public int ExitCode { get; }

    public bool Succeeded => ExitCode == 0;

    /// <summary>
    /// Gets the build output, for assertion messages.
    /// </summary>
    public string Output { get; }

    /// <summary>
    /// Gets the warnings and errors, in the order they were reported.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the warnings and errors that RuntimeNullables or Fody reported (not the compiler's) as "kind code", sorted, e.g. "warning RN0002" or
    /// "error Fody" (Fody's errors have no code).
    /// </summary>
    public string[] ToolDiagnostics => Diagnostics
        .Where(d => d.Code.StartsWith("RN", StringComparison.Ordinal) || d.Message.StartsWith("Fody", StringComparison.Ordinal))
        .Select(d => $"{d.Kind} {(d.Code.Length > 0 ? d.Code : "Fody")}")
        .Order()
        .ToArray();

    /// <summary>
    /// Gets the only warning or error with the given code, and fails the test if there isn't exactly one.
    /// </summary>
    public Diagnostic Single(string code)
    {
        var diagnostics = Diagnostics.Where(d => d.Code == code).ToList();
        Assert.AreEqual(1, diagnostics.Count, $"Expected one {code}:\n{Output}");
        return diagnostics[0];
    }

    public override string ToString() => Output;

    // E.g. "C:\App\FodyWeavers.xml(2,4): warning RN0002: Message [C:\App\App.csproj]" or "MSBUILD : error : Fody: Message".
    [GeneratedRegex(
        @"^(?<file>.*?)(?:\((?<line>\d+),(?<column>\d+)\))?\s*:\s*(?<kind>warning|error)\s*(?<code>[A-Za-z]+\d+)?\s*:\s*(?<message>.*?)(?:\s+\[[^\[\]]*\])?$")]
    private static partial Regex DiagnosticRegex();
}

/// <summary>
/// A warning or error from the build output.
/// </summary>
internal sealed record Diagnostic(string File, int Line, int Column, string Kind, string Code, string Message)
{
    public Diagnostic(Match match)
        : this(
            match.Groups["file"].Value.Trim(),
            match.Groups["line"].Success ? int.Parse(match.Groups["line"].Value) : 0,
            match.Groups["column"].Success ? int.Parse(match.Groups["column"].Value) : 0,
            match.Groups["kind"].Value,
            match.Groups["code"].Value,
            match.Groups["message"].Value)
    {
    }
}
