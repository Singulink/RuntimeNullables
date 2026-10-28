namespace RuntimeNullables.Weaver;

/// <summary>
/// Kinds of weaving warnings, so that hosts can report them with different codes.
/// </summary>
public enum WeaverWarningKind
{
    /// <summary>
    /// Null checks were skipped, e.g. because of conflicting nullable annotations.
    /// </summary>
    ChecksSkipped,

    /// <summary>
    /// The assembly's debug symbols could not be found or read, so the woven assembly is written without them.
    /// </summary>
    DebugSymbols,
}
