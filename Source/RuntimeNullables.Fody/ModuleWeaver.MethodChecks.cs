using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using RuntimeNullables.Fody.Contexts;
using RuntimeNullables.Fody.Extensions;

namespace RuntimeNullables.Fody
{
    public partial class ModuleWeaver
    {
        private static void InjectMethodChecks(MethodContext methodContext)
        {
            var method = methodContext.Method;
            var weavingContext = methodContext.WeavingContext;
            var bclReferences = weavingContext.BclReferences;

            bool methodNullChecksEnabled = methodContext.NullChecksEnabled;
            bool? returnNullChecksValue = method.MethodReturnType.GetNullChecksAttributeValue(weavingContext, true);

            bool injectParamChecks = methodNullChecksEnabled;
            bool injectReturnChecks = weavingContext.CheckOutputs &&
                (returnNullChecksValue == true || (methodNullChecksEnabled && returnNullChecksValue != false)) &&
                method.ReturnType.FullName != bclReferences.VoidType.FullName;

            if (!(injectParamChecks || injectReturnChecks) || !(methodContext.WeavingContext.CheckNonPublic || IsMethodPossiblyExposed(method)))
                return;

            ReturnBlockInfo returnBlockInfo = null;

            // InjectReturnChecks will simplify macros if it modifies the method. Parameter checks don't simplify because they don't change branch offsets.

            bool returnChecksInjected = injectReturnChecks && InjectReturnChecks(methodContext, ref returnBlockInfo);
            bool paramChecksInjected = injectParamChecks && InjectParameterChecks(methodContext, ref returnBlockInfo);

            if (returnBlockInfo != null)
                UpdateReturnBlockInstructionReferences(method, returnBlockInfo);

            // Simplify injected macros and/or simplified macros if return checks simplified them.

            if (paramChecksInjected || returnChecksInjected)
                method.Body.OptimizeMacros();

            if (paramChecksInjected)
                AddParamChecksToDebugInfo(method);

            static bool IsMethodPossiblyExposed(MethodDefinition method)
            {
                if (method.IsPublic && IsTypeExposed(method.DeclaringType))
                    return true;

                // Check if method implements any public interface methods

                if (method.HasOverrides && method.Overrides.Any(m => IsMethodPossiblyExposed(m.Resolve())))
                    return true;

                // Check if method implements any public methods

                if (method.IsVirtual && !method.IsNewSlot) {
                    var baseMethod = method.GetBaseMethod();

                    if (baseMethod != method && IsMethodPossiblyExposed(baseMethod))
                        return true;
                }

                return false;
            }

            static bool IsTypeExposed(TypeDefinition type)
            {
                return type.IsPublic && (type.DeclaringType == null || IsTypeExposed(type.DeclaringType));
            }
        }

        /// <summary>
        /// Points all instructions that previously jumped to returns to point at the new return block start points.
        /// </summary>
        private static void UpdateReturnBlockInstructionReferences(MethodDefinition method, ReturnBlockInfo returnBlockInfo)
        {
            var instructions = method.Body.Instructions;

            var oldStartPoints = returnBlockInfo.OldStartPoints;
            var newStartPoints = returnBlockInfo.NewStartPoints;
            var branches = returnBlockInfo.Branches;

            foreach (var instruction in instructions) {
                if (instruction.Operand is Instruction branchTarget && !branches.Contains(instruction)) {
                    int startPointIndex = oldStartPoints.IndexOf(branchTarget);

                    if (startPointIndex >= 0)
                        instruction.Operand = newStartPoints[startPointIndex];
                }
                else if (instruction.Operand is Instruction[] branchTargets) {
                    for (int i = 0; i < branchTargets.Length; i++) {
                        int startPointIndex = oldStartPoints.IndexOf(branchTargets[i]);

                        if (startPointIndex >= 0)
                            branchTargets[i] = newStartPoints[startPointIndex];
                    }
                }
            }
        }

        /// <summary>
        /// Updates the method's debug information to include injected IL and variables.
        /// </summary>
        private static void AddParamChecksToDebugInfo(MethodDefinition method)
        {
            if (!(method.DebugInformation is { } debugInfo && debugInfo.Scope is { } debugScope))
                return;

            var instructions = method.Body.Instructions;

            if (debugInfo.HasSequencePoints && debugInfo.SequencePoints[0] is var firstSequencePoint && firstSequencePoint.Offset == 0) {
                var firstInstruction = instructions[0];

                debugInfo.SequencePoints[0] = new SequencePoint(firstInstruction, firstSequencePoint.Document) {
                    StartColumn = firstSequencePoint.StartColumn,
                    StartLine = firstSequencePoint.StartLine,
                    EndColumn = firstSequencePoint.EndColumn,
                    EndLine = firstSequencePoint.EndLine,
                };

                debugScope.Start = new InstructionOffset(firstInstruction);
            }
        }
    }
}
