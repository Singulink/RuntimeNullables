using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RuntimeNullables.Weaver.Extensions;

namespace RuntimeNullables.Weaver;

public partial class ModuleWeaver
{
    /// <summary>
    /// Injects a null check of an output before each of the method's returns, optionally only when a condition holds.
    /// </summary>
    private static void InjectOutputCheck(MethodDefinition method, ReturnBlockInfo returnBlockInfo, ModuleReferences moduleReferences, OutputCheck check)
    {
        var instructions = method.Body.Instructions;
        var returnBlockStartPoints = returnBlockInfo.NewStartPoints;

        for (int i = 0; i < returnBlockStartPoints.Length; i++)
        {
            // The return value (if any) is on the stack. Every branch skips to the start point, which is the next check or the return.

            var startPoint = returnBlockStartPoints[i];
            int index = instructions.LastIndexOf(startPoint);
            int firstIndex = index;

            if (check.WhenResult is bool result)
            {
                instructions.Insert(index++, Instruction.Create(OpCodes.Dup));
                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(result ? OpCodes.Brfalse : OpCodes.Brtrue, startPoint));
            }

            if (check.WhenArgumentNotNull is { } argument)
            {
                instructions.Insert(index++, Instruction.Create(OpCodes.Ldloc, argument));

                // A Nullable<T> without a value boxes to null.

                if (argument.VariableType.IsNullableValueType())
                    instructions.Insert(index++, Instruction.Create(OpCodes.Box, argument.VariableType));
                else
                    ILHelpers.InsertGetValueRef(ref index, instructions, argument.VariableType);

                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(OpCodes.Brfalse, startPoint));
            }

            switch (check.Output)
            {
                case ParameterDefinition parameter:
                    instructions.Insert(index++, Instruction.Create(OpCodes.Ldarg, parameter));
                    ILHelpers.InsertGetValueRef(ref index, instructions, parameter.ParameterType);
                    break;
                case VariableDefinition argumentCopy:
                    instructions.Insert(index++, Instruction.Create(OpCodes.Ldloc, argumentCopy));
                    ILHelpers.InsertGetValueRef(ref index, instructions, argumentCopy.VariableType);
                    break;
                default:
                    instructions.Insert(index++, Instruction.Create(OpCodes.Dup));
                    ILHelpers.InsertGetValueRef(ref index, instructions, method.ReturnType);
                    break;
            }

            ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
                ref index, instructions, moduleReferences.ThrowOutputNullMethod, check.Message, startPoint, returnBlockInfo);

            returnBlockStartPoints[i] = instructions[firstIndex];
        }
    }

    /// <summary>
    /// Gets the by-value parameters named by the item's <c>[NotNullIfNotNull]</c> attributes: the item isn't null if one of their arguments isn't.
    /// Other parameters (out/ref/in, value types other than <see cref="System.Nullable{T}"/>, names that don't match) are ignored.
    /// </summary>
    private static IEnumerable<ParameterDefinition> GetNotNullIfNotNullSources(MethodDefinition method, ICustomAttributeProvider item)
    {
        return item.GetNotNullIfNotNullParameterNames()
            .Select(name => method.Parameters.FirstOrDefault(p => p.Name == name))
            .OfType<ParameterDefinition>()
            .Where(p => p.ParameterType.WithoutModifiers() is var type && !type.IsByReference && (type.IsReferenceType() || type.IsNullableValueType()));
    }

    /// <summary>
    /// An output null check: the value that has to be non-null and the conditions under which it's checked.
    /// </summary>
    private sealed class OutputCheck
    {
        private OutputCheck(object? output, string message, bool? whenResult, VariableDefinition? whenArgumentNotNull)
        {
            Output = output;
            Message = message;
            WhenResult = whenResult;
            WhenArgumentNotNull = whenArgumentNotNull;
        }

        /// <summary>
        /// Gets the checked output: an <c>out</c>/<c>ref</c> parameter, the copy of a by-value argument, or <see langword="null"/> for the return
        /// value.
        /// </summary>
        public object? Output { get; }

        public string Message { get; }

        /// <summary>
        /// Gets the <see langword="bool"/> return value the check applies to, or <see langword="null"/> if it applies to any return value.
        /// </summary>
        public bool? WhenResult { get; }

        /// <summary>
        /// Gets the copy of the argument that has to be non-null for the check to apply, or <see langword="null"/> if it always applies.
        /// </summary>
        public VariableDefinition? WhenArgumentNotNull { get; }

        public static OutputCheck ForReturnValue(VariableDefinition? whenArgumentNotNull = null)
        {
            return new(null, "Return value nullability contract was broken.", null, whenArgumentNotNull);
        }

        public static OutputCheck ForParameter(ParameterDefinition parameter, bool? whenResult = null, VariableDefinition? whenArgumentNotNull = null)
        {
            return new(parameter, $"Output parameter '{parameter.Name}' nullability contract was broken.", whenResult, whenArgumentNotNull);
        }

        public static OutputCheck ForArgument(ParameterDefinition parameter, VariableDefinition argumentCopy, bool? whenResult = null)
        {
            return new(argumentCopy, $"Parameter '{parameter.Name}' nullability contract was broken.", whenResult, null);
        }
    }
}
