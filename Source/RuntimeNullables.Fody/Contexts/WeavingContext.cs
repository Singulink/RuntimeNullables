using System;

namespace RuntimeNullables.Fody.Contexts
{
    internal class WeavingContext
    {
        private readonly ModuleWeaver _weaver;

        private BclReferences? _bclReferences;
        private ModuleReferences? _moduleReferences;

        public WeavingContext(ModuleWeaver weaver)
        {
            _weaver = weaver;
            ModuleContext = new ModuleContext(weaver.ModuleDefinition, this);

            bool isDebug = weaver.DefineConstants.Contains("DEBUG");

            CheckOutputs = weaver.GetConfigBooleanValue("CheckOutputs") ?? isDebug;
            CheckNonPublic = weaver.GetConfigBooleanValue("CheckNonPublic") ?? isDebug;
        }

        public ModuleContext ModuleContext { get; }

        public BclReferences BclReferences => _bclReferences ?? throw new InvalidOperationException("Weaving context not built.");

        public ModuleReferences ModuleReferences {
            get => _moduleReferences ?? throw new InvalidOperationException("Module references not set.");
            set => _moduleReferences = value;
        }

        public bool CheckOutputs { get; }

        public bool CheckNonPublic { get; }

        public void WriteInfo(string message) => _weaver.WriteInfo(message);

        public void WriteWarning(string message) => _weaver.WriteWarning(message);

        public void WriteError(string message) => _weaver.WriteError(message);

        public void Build()
        {
            _bclReferences = new BclReferences(_weaver);
            ModuleContext.Build();
        }

        /// <summary>
        /// Combines the result of two exclusive annotation presence checks and writes a warning if they are both present.
        /// </summary>
        /// <returns>True if trueResult == true or both result are true, false if falseResult == true, otherwise null.</returns>
        public bool? CombineExclusiveAnnotations(bool trueResult, bool falseResult, string warningFormat, object warningArg0, object? warningArg1 = null)
        {
            if (trueResult && falseResult) {
                string message = string.Format(null, warningFormat, warningArg0, warningArg1);
                WriteWarning(message);
                return true;
            }

            if (trueResult)
                return true;

            if (falseResult)
                return false;

            return null;
        }
    }
}
