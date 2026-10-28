using System;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Options for weaving an assembly file with <see cref="AssemblyWeaver"/>.
/// </summary>
public sealed class AssemblyWeaverOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyWeaverOptions"/> class.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly to weave.</param>
    /// <param name="referencePaths">The paths of the assemblies the assembly was compiled against.</param>
    public AssemblyWeaverOptions(string assemblyPath, IEnumerable<string> referencePaths)
    {
        AssemblyPath = assemblyPath;
        ReferencePaths = referencePaths.ToList();
    }

    /// <summary>
    /// Gets the path of the assembly to weave. Debug symbols are read from the same location.
    /// </summary>
    public string AssemblyPath { get; }

    /// <summary>
    /// Gets or sets the path the woven assembly is written to. Debug symbols are written next to it. Defaults to <see cref="AssemblyPath"/>.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets the paths of the assemblies the assembly was compiled against.
    /// </summary>
    public IReadOnlyList<string> ReferencePaths { get; }

    /// <inheritdoc cref="ModuleWeaver.CheckOutputs"/>
    public bool? CheckOutputs { get; set; }

    /// <inheritdoc cref="ModuleWeaver.CheckNonPublic"/>
    public bool? CheckNonPublic { get; set; }

    /// <inheritdoc cref="ModuleWeaver.DefineConstants"/>
    public ICollection<string> DefineConstants { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the assembly was public signed (the <c>PublicSign</c> MSBuild property). Public signed assemblies are
    /// not re-signed.
    /// </summary>
    public bool PublicSign { get; set; }

    /// <summary>
    /// Gets or sets the strong name key file used to sign the woven assembly again if the compiler signed it. Like the compiler, a key file takes
    /// precedence over <see cref="KeyContainer"/>, and a relative path is looked up in <see cref="BaseDirectory"/> and then in the assembly's directory.
    /// </summary>
    public string? KeyFile { get; set; }

    /// <summary>
    /// Gets or sets the strong name key container (in the machine key store) used to sign the woven assembly again if the compiler signed it and no key
    /// file is set.
    /// </summary>
    public string? KeyContainer { get; set; }

    /// <summary>
    /// Gets or sets the directory a relative key file path is looked up in first, like the compiler's base directory (the project directory in MSBuild
    /// builds).
    /// </summary>
    public string? BaseDirectory { get; set; }
}
