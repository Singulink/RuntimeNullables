using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RuntimeNullables.Fody.Contexts;
using RuntimeNullables.Fody.Extensions;

namespace RuntimeNullables.Fody;

public partial class ModuleWeaver
{
    private static bool InjectParameterChecks(MethodContext methodContext, ref ReturnBlockInfo? returnBlockInfo)
    {
        var method = methodContext.Method;
        var weavingContext = methodContext.WeavingContext;
        var moduleReferences = weavingContext.ModuleReferences;
        bool modified = false;

        foreach (var parameter in method.Parameters.Reverse()) {
            var parameterType = parameter.ParameterType;

            if (!parameterType.IsReferenceType())
                continue;

            // Preconditions:

            bool? allowsNull = false;
            bool isNullable = false;

            if (!parameter.IsOut) {
                allowsNull = AllowsNull(parameter);

                // Avoid calling MethodContext.IsParameterNullable if we won't need the value
                // We only use isNullable if allowsNull == null

                if (allowsNull == null)
                    isNullable = methodContext.IsParameterNullable(parameter);

                if (allowsNull == false || (allowsNull == null && !isNullable)) {
                    InjectParameterInputCheck(parameter, method, moduleReferences);
                    modified = true;
                }
            }

            // Postconditions:

            if (weavingContext.CheckOutputs && (parameterType.IsPointer || (parameterType.IsByReference && !parameter.IsIn))) {
                bool? maybeNull = MaybeNull(parameter);

                // Avoid calling MethodContext.IsParameterNullable unless we did not already do so for the getter and we are going to need the value
                // If allowsNull == null then isNullable was already determined above and we only use isNullable if maybeNull == null

                if (allowsNull != null && maybeNull == null)
                    isNullable = methodContext.IsParameterNullable(parameter);

                if (maybeNull == false || (maybeNull == null && !isNullable)) {
                    returnBlockInfo ??= new ReturnBlockInfo(method);

                    InjectParameterOutputCheck(parameter, method, returnBlockInfo, moduleReferences);
                    modified = true;
                }
            }
        }

        return modified;

        bool? AllowsNull(ParameterDefinition parameter)
        {
            const string messageFormat = "Skipping null checks due to conflicting preconditions on parameter '{0}' on method '{1}'.";

            bool allowsNull = parameter.HasAllowNullAttribute();
            bool disallowsNull = parameter.HasDisallowNullAttribute();

            return weavingContext.CombineExclusiveAnnotations(allowsNull, disallowsNull, messageFormat, parameter, method);
        }

        bool? MaybeNull(ParameterDefinition parameter)
        {
            const string messageFormat = "Skipping null checks due to conflicting postconditions on parameter '{0}' on method '{1}'.";

            bool maybeNull = parameter.HasAnyMaybeNullAttribute();
            bool notNull = parameter.HasNotNullAttribute();

            return weavingContext.CombineExclusiveAnnotations(maybeNull, notNull, messageFormat, parameter, method);
        }
    }

    private static void InjectParameterInputCheck(ParameterDefinition parameter, MethodDefinition method, ModuleReferences moduleReferences)
    {
        var instructions = method.Body.Instructions;
        var instruction0 = instructions[0];
        int index = 0;

        instructions.Insert(index++, Instruction.Create(OpCodes.Ldarg, parameter));
        ILHelpers.InsertGetValueRef(ref index, instructions, parameter.ParameterType);
        ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
            ref index, instructions, moduleReferences.ThrowArgumentNullMethod, parameter.Name, instruction0);
    }

    private static void InjectParameterOutputCheck(ParameterDefinition parameter, MethodDefinition method, ReturnBlockInfo returnBlockInfo, ModuleReferences moduleReferences)
    {
        var instructions = method.Body.Instructions;
        var returnBlockStartPoints = returnBlockInfo.NewStartPoints;

        for (int i = 0; i < returnBlockStartPoints.Length; i++) {
            Instruction injectionPoint = returnBlockStartPoints[i];
            int index = instructions.LastIndexOf(injectionPoint);

            var firstInstruction = Instruction.Create(OpCodes.Ldarg, parameter);
            instructions.Insert(index++, firstInstruction);
            ILHelpers.InsertGetValueRef(ref index, instructions, parameter.ParameterType);

            string message = $"Output parameter '{parameter.Name}' nullability contract was broken.";
            ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
                ref index, instructions, moduleReferences.ThrowOutputNullMethod, message, injectionPoint, returnBlockInfo);

            returnBlockStartPoints[i] = firstInstruction;
        }
    }
}