using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RuntimeNullables.Tasks;

/// <summary>
/// Reads the settings that aren't set as properties from the <c>&lt;RuntimeNullables&gt;</c> element of Fody's configuration, which configured
/// RuntimeNullables when it was a Fody add-in, and asks the user to migrate them.
/// </summary>
public sealed class ReadLegacyFodyConfiguration : Task
{
    private const string MessagePrefix = "RuntimeNullables: ";
    private const string PropertyPrefix = "RuntimeNullables";
    private const string PropertyLocation = "the WeaverConfiguration property";

    /// <summary>
    /// Gets or sets the path of the project file, which messages about the <c>WeaverConfiguration</c> property are attributed to.
    /// </summary>
    [Required]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <c>FodyWeavers.xml</c> files in the order Fody reads them: in the solution directory, then in the project directory.
    /// </summary>
    public ITaskItem[] FodyWeaversFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets Fody's <c>WeaverConfiguration</c> property, which takes precedence over <see cref="FodyWeaversFiles"/>.
    /// </summary>
    public string? WeaverConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>RuntimeNullablesCheckOutputs</c> property, which overrides the legacy setting if it isn't empty.
    /// </summary>
    public string? CheckOutputs { get; set; }

    /// <summary>
    /// Gets or sets the value of the <c>RuntimeNullablesCheckNonPublic</c> property, which overrides the legacy setting if it isn't empty.
    /// </summary>
    public string? CheckNonPublic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Fody runs in this project (for other add-ins). Fody fails the build while the project's
    /// <c>FodyWeavers.xml</c> lists an add-in that isn't installed, so that is reported as an error up front.
    /// </summary>
    public bool FodyEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the warning about unreadable configuration (RN0005) is reported as an error, unless
    /// <see cref="NoWarn"/> or <see cref="WarningsNotAsErrors"/> list it.
    /// </summary>
    public bool TreatWarningsAsErrors { get; set; }

    /// <summary>
    /// Gets or sets the codes of warnings that are suppressed (<c>$(NoWarn)</c> and <c>$(MSBuildWarningsAsMessages)</c>).
    /// </summary>
    public string? NoWarn { get; set; }

    /// <summary>
    /// Gets or sets the codes of warnings that are not reported as errors (<c>$(WarningsNotAsErrors)</c> and <c>$(MSBuildWarningsNotAsErrors)</c>).
    /// </summary>
    public string? WarningsNotAsErrors { get; set; }

    /// <summary>
    /// Gets the legacy <c>CheckOutputs</c> setting (<c>true</c> or <c>false</c>), or an empty string if it isn't set or the property overrides it.
    /// </summary>
    [Output]
    public string LegacyCheckOutputs { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the legacy <c>CheckNonPublic</c> setting (<c>true</c> or <c>false</c>), or an empty string if it isn't set or the property overrides it.
    /// </summary>
    [Output]
    public string LegacyCheckNonPublic { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public override bool Execute()
    {
        var configuration = LegacyFodyConfiguration.Find(FodyWeaversFiles.Select(f => f.GetMetadata("FullPath")), WeaverConfiguration, LogReadError);

        if (configuration != null)
        {
            var replacements = new List<string>();
            LegacyCheckOutputs = ReadSetting(configuration, "CheckOutputs", CheckOutputs, replacements);
            LegacyCheckNonPublic = ReadSetting(configuration, "CheckNonPublic", CheckNonPublic, replacements);
            ReportMigration(configuration, replacements);
        }

        return !Log.HasLoggedErrors;
    }

    /// <summary>
    /// Reads a setting from the element if its property isn't set, and adds the property that replaces it to <paramref name="replacements"/>.
    /// </summary>
    /// <returns>The setting (<c>true</c> or <c>false</c>), or an empty string if it isn't set, the property overrides it or it is invalid.</returns>
    private string ReadSetting(LegacyFodyConfiguration configuration, string attributeName, string? propertyValue, List<string> replacements)
    {
        if (!string.IsNullOrEmpty(propertyValue) || configuration.GetAttribute(attributeName) is not { } attribute)
            return string.Empty;

        string propertyName = PropertyPrefix + attributeName;

        if (!LegacyFodyConfiguration.TryParseBoolean(attribute.Value, out bool value))
        {
            string location = configuration.File == null ? $" in {PropertyLocation}" : string.Empty;
            string message = $"Invalid value '{attribute.Value}' for the {attributeName} attribute of the legacy Fody configuration element " +
                $"<{LegacyFodyConfiguration.ElementName}>{location}. Set the {propertyName} property to 'true' or 'false' instead and remove the element.";

            LogError(DiagnosticCodes.InvalidSetting, configuration.File, LegacyFodyConfiguration.GetPosition(attribute), message);
            return string.Empty;
        }

        string text = value ? "true" : "false";
        replacements.Add($"<{propertyName}>{text}</{propertyName}>");
        return text;
    }

    /// <summary>
    /// Asks to migrate the configuration: an error if Fody runs and fails while the element is there, otherwise a warning (unless an invalid value
    /// was reported, which already asks to migrate).
    /// </summary>
    private void ReportMigration(LegacyFodyConfiguration configuration, List<string> replacements)
    {
        bool inProjectFile = configuration.File != null && IsProjectFodyWeaversFile(configuration.File);
        string location = configuration.File == null ? PropertyLocation : "FodyWeavers.xml";
        string intro = $"The <{LegacyFodyConfiguration.ElementName}> element in {location} is legacy Fody configuration";
        string properties = string.Join(" ", replacements);

        if (inProjectFile && FodyEnabled)
        {
            // Fody's own error ("No weavers found for the configuration entries RuntimeNullables") suggests installing the old add-in again.
            string conflict = $"{intro}, and Fody fails the build while it is there since RuntimeNullables isn't a Fody add-in anymore. " +
                (replacements.Count > 0 ? $"Set these properties in the project file instead: {properties}. Then remove the element." : "Remove it.");

            LogError(DiagnosticCodes.FodyConflict, configuration.File, configuration.Position, conflict);
            return;
        }

        if (Log.HasLoggedErrors)
            return;

        string newLocation = configuration.File == null ? "the file that sets the WeaverConfiguration property"
            : inProjectFile ? "the project file"
            : "Directory.Build.props or the project files";

        string removal = configuration.File == null ? "the whole property"
            : inProjectFile ? "FodyWeavers.xml and FodyWeavers.xsd"
            : "FodyWeavers.xml";

        string message = replacements.Count > 0
            ? $"{intro}. Set these properties in {newLocation} instead: {properties}. Then remove the element (or delete {removal} if Fody isn't used " +
              "anymore)."
            : $"{intro} that isn't needed anymore. Remove it (or delete {removal} if Fody isn't used anymore).";

        LogWarning(DiagnosticCodes.LegacyConfiguration, configuration.File, configuration.Position, message);
    }

    private void LogReadError(string? file, Exception exception)
    {
        var position = file != null && exception is XmlException xmlException ? (xmlException.LineNumber, xmlException.LinePosition) : (0, 0);
        string location = file != null ? "FodyWeavers.xml" : PropertyLocation;
        string message = $"The Fody configuration in {location} can't be read, so any RuntimeNullables settings in it are ignored. Fix or remove it: " +
            exception.Message;

        string code = DiagnosticCodes.UnreadableLegacyConfiguration;
        bool isError = WarningPromotion.GetPromotedCodes(TreatWarningsAsErrors, NoWarn, WarningsNotAsErrors, code).Contains(code);

        if (isError)
            LogError(code, file, position, message);
        else
            LogWarning(code, file, position, message);
    }

    /// <summary>
    /// Logs an error at a position in a configuration file, or at the project file for the <c>WeaverConfiguration</c> property.
    /// </summary>
    private void LogError(string code, string? file, (int Line, int Column) position, string message)
    {
        Log.LogError(null, code, null, file ?? ProjectPath, position.Line, position.Column, 0, 0, MessagePrefix + message);
    }

    /// <summary>
    /// Logs a warning at a position in a configuration file, or at the project file for the <c>WeaverConfiguration</c> property.
    /// </summary>
    private void LogWarning(string code, string? file, (int Line, int Column) position, string message)
    {
        Log.LogWarning(null, code, null, file ?? ProjectPath, position.Line, position.Column, 0, 0, MessagePrefix + message);
    }

    private bool IsProjectFodyWeaversFile(string file)
    {
        string projectFile = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(ProjectPath))!, "FodyWeavers.xml");
        return string.Equals(Path.GetFullPath(file), projectFile, StringComparison.OrdinalIgnoreCase);
    }
}
