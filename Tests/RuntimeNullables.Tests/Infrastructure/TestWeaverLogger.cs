using System;
using System.Collections.Generic;
using System.Linq;
using RuntimeNullables.Weaver;

namespace RuntimeNullables.Tests.Infrastructure;

/// <summary>
/// Collects weaving messages for assertions.
/// </summary>
internal sealed class TestWeaverLogger : IWeaverLogger
{
    private readonly List<string> _messages = [];

    public List<(WeaverWarningKind Kind, string Message)> Warnings { get; } = [];

    public List<string> Errors { get; } = [];

    public void LogInfo(string message) => _messages.Add("info: " + message);

    public void LogWarning(WeaverWarningKind kind, string message)
    {
        Warnings.Add((kind, message));
        _messages.Add($"warning ({kind}): {message}");
    }

    public void LogError(string message)
    {
        Errors.Add(message);
        _messages.Add("error: " + message);
    }

    public bool HasErrorContaining(string text) => Errors.Any(e => e.Contains(text, StringComparison.Ordinal));

    public override string ToString() => string.Join(Environment.NewLine, _messages);
}
