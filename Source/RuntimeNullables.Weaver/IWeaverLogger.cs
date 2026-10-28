namespace RuntimeNullables.Weaver;

/// <summary>
/// Receives messages produced while weaving.
/// </summary>
public interface IWeaverLogger
{
    /// <summary>
    /// Logs a low importance diagnostic message.
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Logs a warning.
    /// </summary>
    void LogWarning(WeaverWarningKind kind, string message);

    /// <summary>
    /// Logs an error.
    /// </summary>
    void LogError(string message);
}
