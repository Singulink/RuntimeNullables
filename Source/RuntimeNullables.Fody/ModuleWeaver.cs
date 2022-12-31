using System;
using System.Collections.Generic;
using System.Xml;
using Fody;
using RuntimeNullables.Fody.Contexts;

namespace RuntimeNullables.Fody
{
    /// <summary>
    /// Provides the implementation of the weaving functionality.
    /// </summary>
    /// <remarks>
    /// <para>See https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md for complete details on nullable metadata.</para>
    /// </remarks>
    public partial class ModuleWeaver : BaseModuleWeaver
    {
        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "mscorlib";
            yield return "System.Runtime";
            yield return "System";
            yield return "netstandard";
            yield return "System.Diagnostics.Tools";
        }

        public override bool ShouldCleanReference => true;

        public override void Execute()
        {
            // Uncomment to attach debugger when msbuild task runs:
            // if (ModuleDefinition.Assembly.Name.Name == "TestAssembly")
            //     Debugger.Launch();

            var weavingContext = new WeavingContext(this);
            weavingContext.WriteInfo("Starting weaving context build.");
            weavingContext.Build();

            weavingContext.WriteInfo($"Starting injection - CheckOutputs: {weavingContext.CheckOutputs}, CheckNonPublic: {weavingContext.CheckNonPublic}.");

            try {
                InjectThrowHelpers(weavingContext);
            }
            catch (Exception ex) {
                throw new RuntimeNullablesException("Error injecting throw helpers.", ex);
            }

            var moduleContext = weavingContext.ModuleContext;

            foreach (var typeContext in moduleContext.TypeContexts.Values) {
                foreach (var methodContext in typeContext.MethodContexts) {
                    try {
                        InjectMethodChecks(methodContext);
                    }
                    catch (Exception ex) {
                        throw new RuntimeNullablesException($"Error injecting method '{methodContext.Method}' with null checks.", ex);
                    }
                }
            }

            weavingContext.WriteInfo("Injection complete.");
        }

        public bool? GetConfigBooleanValue(string attributeName)
        {
            if (Config?.Attribute(attributeName)?.Value is string value) {
                try {
                    return XmlConvert.ToBoolean(value);
                }
                catch {
                    throw new RuntimeNullablesException($"Invalid value '{value}' for '{attributeName}' attribute.");
                }
            }

            return null;
        }
    }
}