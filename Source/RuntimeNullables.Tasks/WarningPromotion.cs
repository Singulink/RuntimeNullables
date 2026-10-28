using System;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeNullables.Tasks;

/// <summary>
/// Decides which task warnings are reported as errors.
/// </summary>
/// <remarks>
/// <para>MSBuild applies <c>WarningsAsErrors</c> and <c>NoWarn</c> to task warnings itself, but not <c>TreatWarningsAsErrors</c>, so the tasks apply it
/// to their warnings that <c>NoWarn</c> and <c>WarningsNotAsErrors</c> don't list.</para>
/// </remarks>
internal static class WarningPromotion
{
    /// <summary>
    /// Gets the codes to report as errors.
    /// </summary>
    /// <param name="treatWarningsAsErrors">Whether warnings are treated as errors.</param>
    /// <param name="noWarn">The codes of suppressed warnings (<c>$(NoWarn)</c> and <c>$(MSBuildWarningsAsMessages)</c>).</param>
    /// <param name="warningsNotAsErrors">The codes of warnings that are not reported as errors (<c>$(WarningsNotAsErrors)</c> and
    /// <c>$(MSBuildWarningsNotAsErrors)</c>).</param>
    /// <param name="codes">The codes that <paramref name="treatWarningsAsErrors"/> applies to.</param>
    public static ISet<string> GetPromotedCodes(bool treatWarningsAsErrors, string? noWarn, string? warningsNotAsErrors, params string[] codes)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (treatWarningsAsErrors)
        {
            result.UnionWith(codes);
            result.ExceptWith(MSBuildValues.SplitList(noWarn).Concat(MSBuildValues.SplitList(warningsNotAsErrors)));
        }

        return result;
    }
}
