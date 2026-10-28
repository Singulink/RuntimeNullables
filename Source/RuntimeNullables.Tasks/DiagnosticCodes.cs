namespace RuntimeNullables.Tasks;

/// <summary>
/// Codes for the warnings and errors RuntimeNullables reports to MSBuild, so they can be suppressed or promoted individually.
/// </summary>
/// <remarks>
/// <para>The build targets also report RN0003 (the package reference is missing <c>PrivateAssets="all"</c>), RN1002 for invalid properties, RN1003
/// (MSBuild is too old) and RN1004 for the old <c>RuntimeNullables.Fody</c> package or the Fody package without add-ins.</para>
/// </remarks>
internal static class DiagnosticCodes
{
    /// <summary>
    /// Null checks were skipped, e.g. because of conflicting nullable annotations.
    /// </summary>
    public const string ChecksSkipped = "RN0001";

    /// <summary>
    /// RuntimeNullables is configured with legacy Fody configuration that should be migrated to properties.
    /// </summary>
    /// <remarks>
    /// <para>Unlike RN0001, RN0004 and RN0005, the <c>TreatWarningsAsErrors</c> property doesn't make this one an error, and the build targets
    /// exclude it from <c>-warnaserror</c> (when NuGet imports them), so that projects keep building until they are migrated. Listing it in
    /// <c>WarningsAsErrors</c> still makes it an error.</para>
    /// </remarks>
    public const string LegacyConfiguration = "RN0002";

    /// <summary>
    /// The assembly's debug symbols could not be found or read, so the woven assembly was written without them.
    /// </summary>
    public const string DebugSymbols = "RN0004";

    /// <summary>
    /// Fody configuration can't be read (e.g. invalid XML), so any RuntimeNullables settings in it are ignored. <c>TreatWarningsAsErrors</c> makes
    /// this an error, since Fody failed the build in that case.
    /// </summary>
    public const string UnreadableLegacyConfiguration = "RN0005";

    /// <summary>
    /// An error that stopped the assembly from being woven.
    /// </summary>
    public const string WeavingError = "RN1001";

    /// <summary>
    /// A RuntimeNullables MSBuild property or legacy Fody configuration attribute has an invalid value.
    /// </summary>
    public const string InvalidSetting = "RN1002";

    /// <summary>
    /// The project's Fody setup doesn't work after migrating to RuntimeNullables: the old <c>RuntimeNullables.Fody</c> package is still installed (so
    /// the assembly would be woven twice), or Fody fails because it is installed without add-ins or the project's <c>FodyWeavers.xml</c> still
    /// lists RuntimeNullables while Fody runs for other add-ins.
    /// </summary>
    public const string FodyConflict = "RN1004";
}
