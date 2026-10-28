using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RuntimeNullables.Weaver;

namespace RuntimeNullables.Tasks;

/// <summary>
/// Weaves null checks into a compiled assembly in place.
/// </summary>
public sealed class WeaveRuntimeNullables : Task
{
    /// <summary>
    /// Gets or sets the path of the assembly to weave. Its debug symbols are read from and written next to it.
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path of the project file, which messages are attributed to and a relative key file path is looked up against first.
    /// </summary>
    [Required]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assemblies the assembly was compiled against (<c>@(ReferencePath)</c>).
    /// </summary>
    public ITaskItem[] References { get; set; } = [];

    /// <summary>
    /// Gets or sets the preprocessor symbols the assembly was compiled with (<c>$(DefineConstants)</c>).
    /// </summary>
    public string? DefineConstants { get; set; }

    /// <summary>
    /// Gets or sets the <c>RuntimeNullablesCheckOutputs</c> setting: an MSBuild boolean (e.g. <c>true</c> or <c>false</c>), or empty for the default.
    /// </summary>
    public string? CheckOutputs { get; set; }

    /// <summary>
    /// Gets or sets the <c>RuntimeNullablesCheckNonPublic</c> setting: an MSBuild boolean (e.g. <c>true</c> or <c>false</c>), or empty for the default.
    /// </summary>
    public string? CheckNonPublic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the assembly was public signed (<c>$(PublicSign)</c>).
    /// </summary>
    public bool PublicSign { get; set; }

    /// <summary>
    /// Gets or sets the strong name key file the assembly was signed with (<c>$(KeyOriginatorFile)</c>).
    /// </summary>
    public string? KeyFile { get; set; }

    /// <summary>
    /// Gets or sets the strong name key container the assembly was signed with (<c>$(KeyContainerName)</c>).
    /// </summary>
    public string? KeyContainer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether weaving warnings are reported as errors.
    /// </summary>
    /// <remarks>
    /// <para>Applies to the weaving warnings (RN0001 and RN0004) that <see cref="NoWarn"/> and <see cref="WarningsNotAsErrors"/> don't list (see
    /// <see cref="WarningPromotion"/>). Unlike warnings that MSBuild reports as errors, these make weaving fail, so nothing is written.</para>
    /// </remarks>
    public bool TreatWarningsAsErrors { get; set; }

    /// <summary>
    /// Gets or sets the codes of warnings that are suppressed (<c>$(NoWarn)</c> and <c>$(MSBuildWarningsAsMessages)</c>).
    /// </summary>
    public string? NoWarn { get; set; }

    /// <summary>
    /// Gets or sets the codes of warnings that are not reported as errors (<c>$(WarningsNotAsErrors)</c> and <c>$(MSBuildWarningsNotAsErrors)</c>).
    /// </summary>
    public string? WarningsNotAsErrors { get; set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        // Non-short-circuiting so that every invalid setting is reported.

        if (!TryParseSetting(CheckOutputs, "RuntimeNullablesCheckOutputs", out bool? checkOutputs) |
            !TryParseSetting(CheckNonPublic, "RuntimeNullablesCheckNonPublic", out bool? checkNonPublic))
        {
            return false;
        }

        var options = new AssemblyWeaverOptions(AssemblyPath, References.Select(r => r.GetMetadata("FullPath")))
        {
            CheckOutputs = checkOutputs,
            CheckNonPublic = checkNonPublic,
            DefineConstants = new HashSet<string>(MSBuildValues.SplitList(DefineConstants), StringComparer.Ordinal),
            PublicSign = PublicSign,
            KeyFile = KeyFile,
            KeyContainer = KeyContainer,
            BaseDirectory = Path.GetDirectoryName(Path.GetFullPath(ProjectPath)),
        };

        var warningsAsErrors = WarningPromotion.GetPromotedCodes(
            TreatWarningsAsErrors, NoWarn, WarningsNotAsErrors, DiagnosticCodes.ChecksSkipped, DiagnosticCodes.DebugSymbols);

        var stopwatch = Stopwatch.StartNew();

        if (!AssemblyWeaver.Weave(options, new TaskWeaverLogger(Log, ProjectPath, warningsAsErrors)) || Log.HasLoggedErrors)
            return false;

        Log.LogMessage(MessageImportance.Normal, $"RuntimeNullables: Wove '{AssemblyPath}' in {stopwatch.ElapsedMilliseconds} ms.");
        return true;
    }

    /// <summary>
    /// Parses a setting the way MSBuild does (like the build targets, which check the settings before compiling), or empty for the default.
    /// </summary>
    private bool TryParseSetting(string? value, string propertyName, out bool? result)
    {
        result = null;

        if (string.IsNullOrEmpty(value))
            return true;

        if (MSBuildValues.TryParseBoolean(value, out bool parsed))
        {
            result = parsed;
            return true;
        }

        Log.LogError(
            null, DiagnosticCodes.InvalidSetting, null, ProjectPath, 0, 0, 0, 0,
            $"RuntimeNullables: Invalid value '{value}' for the {propertyName} property. Expected 'true' or 'false'.");

        return false;
    }
}
