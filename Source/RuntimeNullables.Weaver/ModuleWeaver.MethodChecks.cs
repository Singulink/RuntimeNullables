using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using RuntimeNullables.Weaver.Contexts;
using RuntimeNullables.Weaver.Extensions;

namespace RuntimeNullables.Weaver;

public partial class ModuleWeaver
{
    private static void InjectMethodChecks(MethodContext methodContext)
    {
        var method = methodContext.Method;
        var weavingContext = methodContext.WeavingContext;
        var bclReferences = weavingContext.BclReferences;

        bool methodNullChecksEnabled = methodContext.NullChecksEnabled;
        bool? returnNullChecksValue = method.MethodReturnType.GetNullChecksAttributeValue(weavingContext);

        bool injectParamChecks = methodNullChecksEnabled;
        bool injectReturnChecks = weavingContext.CheckOutputs &&
            (returnNullChecksValue == true || (methodNullChecksEnabled && returnNullChecksValue != false)) &&
            method.ReturnType.WithoutModifiers().FullName != bclReferences.VoidType.FullName;

        if (!(injectParamChecks || injectReturnChecks) || !(weavingContext.CheckNonPublic || weavingContext.PublicApi.Contains(method)))
            return;

        // Creating the return block info expands the method's short branches (checks injected before returns can push them out of range). Input checks
        // are injected at the start of the method, which doesn't change the distance of any existing branch.

        ReturnBlockInfo? returnBlockInfo = null;
        var argumentCopies = new ArgumentCopies(method);

        bool returnChecksInjected = injectReturnChecks && InjectReturnChecks(methodContext, ref returnBlockInfo, argumentCopies);
        bool paramChecksInjected = injectParamChecks && InjectParameterChecks(methodContext, ref returnBlockInfo, argumentCopies);

        if (returnBlockInfo != null)
            UpdateReturnBlockInstructionReferences(method, returnBlockInfo);

        // Input checks and argument copies are inserted at the start of the method.

        if (paramChecksInjected || argumentCopies.HasCopies)
            AddParamChecksToDebugInfo(method);

        // Use the short forms of the injected instructions, and of the method's own instructions if they were expanded.

        if (paramChecksInjected || returnChecksInjected || returnBlockInfo != null)
            method.Body.OptimizeMacros();
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

        foreach (var instruction in instructions)
        {
            if (instruction.Operand is Instruction branchTarget && !branches.Contains(instruction))
            {
                int startPointIndex = oldStartPoints.IndexOf(branchTarget);

                if (startPointIndex >= 0)
                    instruction.Operand = newStartPoints[startPointIndex];
            }
            else if (instruction.Operand is Instruction[] branchTargets)
            {
                for (int i = 0; i < branchTargets.Length; i++)
                {
                    int startPointIndex = oldStartPoints.IndexOf(branchTargets[i]);

                    if (startPointIndex >= 0)
                        branchTargets[i] = newStartPoints[startPointIndex];
                }
            }
        }

        foreach (var handler in method.Body.ExceptionHandlers)
        {
            int handlerEndIndex = oldStartPoints.IndexOf(handler.HandlerEnd);

            if (handlerEndIndex >= 0)
                handler.HandlerEnd = newStartPoints[handlerEndIndex];
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

        if (debugInfo.HasSequencePoints && debugInfo.SequencePoints[0] is var firstSequencePoint && firstSequencePoint.Offset == 0)
        {
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
