using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RuntimeNullables.Weaver;

namespace RuntimeNullables.Tasks;

/// <summary>
/// Forwards weaving messages to the MSBuild log, attributed to the project file.
/// </summary>
internal sealed class TaskWeaverLogger(TaskLoggingHelper log, string projectPath, ISet<string> warningsAsErrors) : IWeaverLogger
{
    private const string MessagePrefix = "RuntimeNullables: ";

    public void LogInfo(string message) => log.LogMessage(MessageImportance.Low, MessagePrefix + message);

    public void LogWarning(WeaverWarningKind kind, string message)
    {
        string code = kind switch
        {
            WeaverWarningKind.DebugSymbols => DiagnosticCodes.DebugSymbols,
            _ => DiagnosticCodes.ChecksSkipped,
        };

        if (warningsAsErrors.Contains(code))
            LogError(code, message);
        else
            log.LogWarning(null, code, null, projectPath, 0, 0, 0, 0, MessagePrefix + message);
    }

    public void LogError(string message) => LogError(DiagnosticCodes.WeavingError, message);

    private void LogError(string code, string message) => log.LogError(null, code, null, projectPath, 0, 0, 0, 0, MessagePrefix + message);
}
