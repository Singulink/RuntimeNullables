using System;
using System.Collections.Generic;
using Mono.Cecil;
using RuntimeNullables.Weaver.Contexts;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Provides the implementation of the weaving functionality.
/// </summary>
/// <remarks>
/// <para>See https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md for complete details on nullable metadata.</para>
/// <para>This class only modifies the in-memory module and leaves its reference to the RuntimeNullables assembly in place. Use <see cref="AssemblyWeaver"/>
/// to weave an assembly file.</para>
/// </remarks>
public partial class ModuleWeaver
{
    private readonly IWeaverLogger _logger;
    private TypeFinder? _typeFinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleWeaver"/> class.
    /// </summary>
    /// <param name="moduleDefinition">The module to weave. Its <see cref="ModuleDefinition.AssemblyResolver"/> is used to locate BCL types.</param>
    /// <param name="logger">The logger that receives weaving messages.</param>
    public ModuleWeaver(ModuleDefinition moduleDefinition, IWeaverLogger logger)
    {
        ModuleDefinition = moduleDefinition;
        _logger = logger;
    }

    /// <summary>
    /// Gets the module being woven.
    /// </summary>
    public ModuleDefinition ModuleDefinition { get; }

    /// <summary>
    /// Gets or sets a value indicating whether outputs (return values and <see langword="out"/>/<see langword="ref"/> parameters) are checked. If
    /// <see langword="null"/> then outputs are checked when <see cref="DefineConstants"/> contains <c>DEBUG</c>.
    /// </summary>
    public bool? CheckOutputs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether members outside the public API are checked. If <see langword="null"/> then they are checked when
    /// <see cref="DefineConstants"/> contains <c>DEBUG</c>.
    /// </summary>
    public bool? CheckNonPublic { get; set; }

    /// <summary>
    /// Gets or sets the preprocessor symbols the module was compiled with.
    /// </summary>
    public ICollection<string> DefineConstants { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Injects null checks into the module and removes all <c>[NullChecks]</c> attributes, including those on items that don't get checks.
    /// </summary>
    public void Execute()
    {
        // Uncomment to attach debugger when msbuild task runs:
        // if (ModuleDefinition.Assembly.Name.Name == "TestAssembly")
        //     Debugger.Launch();

        var weavingContext = new WeavingContext(this);
        weavingContext.WriteInfo("Starting weaving context build.");
        weavingContext.Build();

        weavingContext.WriteInfo($"Starting injection - CheckOutputs: {weavingContext.CheckOutputs}, CheckNonPublic: {weavingContext.CheckNonPublic}.");

        try
        {
            InjectThrowHelpers(weavingContext);
        }
        catch (Exception ex)
        {
            throw new RuntimeNullablesException("Error injecting throw helpers.", ex);
        }

        var moduleContext = weavingContext.ModuleContext;

        foreach (var typeContext in moduleContext.TypeContexts.Values)
        {
            foreach (var methodContext in typeContext.MethodContexts)
            {
                try
                {
                    InjectMethodChecks(methodContext);
                }
                catch (Exception ex)
                {
                    throw new RuntimeNullablesException($"Error injecting method '{methodContext.Method}' with null checks.", ex);
                }
            }
        }

        RemoveAllNullChecksAttributes(ModuleDefinition);
        weavingContext.WriteInfo("Injection complete.");
    }

    /// <summary>
    /// Removes the <c>[NullChecks]</c> attributes from everything the attribute can be applied to. They are only read during weaving.
    /// </summary>
    private static void RemoveAllNullChecksAttributes(ModuleDefinition module)
    {
        module.Assembly?.RemoveNullChecksAttributes();
        module.RemoveNullChecksAttributes();

        foreach (var type in module.GetTypes())
        {
            type.RemoveNullChecksAttributes();

            foreach (var method in type.Methods)
            {
                method.RemoveNullChecksAttributes();
                method.MethodReturnType.RemoveNullChecksAttributes();
            }

            foreach (var property in type.Properties)
                property.RemoveNullChecksAttributes();
        }
    }

    /// <summary>
    /// Finds a public BCL type by its full name, or returns <see langword="null"/> if it could not be found.
    /// </summary>
    internal TypeDefinition? FindTypeDefinition(string fullName)
    {
        _typeFinder ??= new TypeFinder(ModuleDefinition.AssemblyResolver);
        return _typeFinder.Find(fullName);
    }

    internal void WriteInfo(string message) => _logger.LogInfo(message);

    internal void WriteWarning(string message) => _logger.LogWarning(WeaverWarningKind.ChecksSkipped, message);

    internal void WriteError(string message) => _logger.LogError(message);
}
