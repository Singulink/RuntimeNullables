using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using RuntimeNullables.Weaver.Contexts;
using RuntimeNullables.Weaver.Extensions;

namespace RuntimeNullables.Weaver;

public partial class ModuleWeaver
{
    private const string AsyncTaskResultMessage = "Async task result nullability contract was broken.";

    private static bool InjectReturnChecks(MethodContext methodContext, ref ReturnBlockInfo? returnBlockInfo, ArgumentCopies argumentCopies)
    {
        var method = methodContext.Method;
        var returnType = method.ReturnType.WithoutModifiers();
        var weavingContext = methodContext.WeavingContext;

        // Runtime async (.NET 11) task result. The method's body returns the result itself:

        if (method.IsRuntimeAsync())
        {
            if (!returnType.IsTaskWithResultType())
            {
                if (!returnType.IsNonGenericTaskType())
                    weavingContext.WriteWarning($"Skipping runtime async result check on method '{method}': Unknown Task return type.");

                return false;
            }

            if (methodContext.IsReturnValueGenericArgumentNullableOrValueType())
                return false;

            var resultType = ((GenericInstanceType)returnType).GenericArguments[0];
            returnBlockInfo ??= new ReturnBlockInfo(method);
            InjectRuntimeAsyncResultCheck(method, resultType, returnBlockInfo, weavingContext.ModuleReferences);

            return true;
        }

        // Enumerable results:

        if (method.GetIteratorStateMachineType(weavingContext) is { } iteratorStateMachineType)
        {
            if (!returnType.IsNonGenericEnumeratorType())
            {
                if (!returnType.IsGenericEnumeratorType())
                    weavingContext.WriteWarning($"Skipping iterator state machine result checks on method '{method}': Unknown iterator return type.");
                else if (!methodContext.IsReturnValueGenericArgumentNullableOrValueType())
                    InjectIteratorStateMachineChecks(method, iteratorStateMachineType, weavingContext);
            }

            // Method not modified directly so always return false
            return false;
        }

        // Async task result:

        if (method.GetAsyncStateMachineType(weavingContext) is { } asyncStateMachineType)
        {
            if (!returnType.IsNonGenericTaskType())
            {
                if (!returnType.IsTaskWithResultType())
                    weavingContext.WriteWarning($"Skipping async state machine result check on method '{method}': Unknown Task return type.");
                else if (!methodContext.IsReturnValueGenericArgumentNullableOrValueType())
                    InjectAsyncStateMachineCheck(method, asyncStateMachineType, weavingContext);
            }

            // Method not modified directly so always return false
            return false;
        }

        // Async enumerable results:

        if (method.GetAsyncIteratorStateMachineType(weavingContext) is { } asyncIteratorStateMachineType)
        {
            if (!returnType.IsAsyncEnumeratorType())
                weavingContext.WriteWarning($"Skipping async iterator state machine result checks on method '{method}': Unknown async iterator return type.");
            else if (!methodContext.IsReturnValueGenericArgumentNullableOrValueType())
                InjectAsyncIteratorStateMachineChecks(method, asyncIteratorStateMachineType, weavingContext);

            // Method not modified directly so always return false
            return false;
        }

        bool modified = false;

        // Synchronous task result:

        if (returnType.IsTaskWithResultType() && !methodContext.IsReturnValueGenericArgumentNullableOrValueType())
        {
            returnBlockInfo ??= new ReturnBlockInfo(method);

            if (InjectSynchronousTaskResultCheck(method, (GenericInstanceType)returnType, returnBlockInfo, weavingContext))
                modified = true;
        }

        // Return reference:

        if (returnType.IsReferenceType())
        {
            bool? maybeNull = ReturnValueMaybeNull();
            var checks = new List<OutputCheck>();

            if (maybeNull == false || (maybeNull == null && !methodContext.IsReturnValueNullable()))
            {
                checks.Add(OutputCheck.ForReturnValue());
            }
            else
            {
                // [NotNullIfNotNull]: a return value that can be null can't be if one of the named arguments isn't.

                foreach (var source in GetNotNullIfNotNullSources(method, method.MethodReturnType))
                    checks.Add(OutputCheck.ForReturnValue(whenArgumentNotNull: argumentCopies.GetOrAdd(source)));
            }

            foreach (var check in checks)
            {
                InjectOutputCheck(method, returnBlockInfo ??= new ReturnBlockInfo(method), weavingContext.ModuleReferences, check);
                modified = true;
            }
        }

        return modified;

        bool? ReturnValueMaybeNull()
        {
            bool maybeNull = method.MethodReturnType.HasMaybeNullAttribute();
            bool notNull = method.MethodReturnType.HasNotNullAttribute();
            const string messageFormat = "Skipping return value check on method '{0}': Conflicting return value postconditions.";

            return weavingContext.CombineExclusiveAnnotations(maybeNull, notNull, messageFormat, method);
        }
    }

    private static void InjectIteratorStateMachineChecks(MethodDefinition method, TypeDefinition stateMachineType, WeavingContext weavingContext)
    {
        var moduleReferences = weavingContext.ModuleReferences;

        if (!(GetMoveNextMethod(stateMachineType) is { } moveNextMethod))
        {
            WriteSkipWarning("'MoveNext' method not found on state machine.");
            return;
        }

        if (!(GetFieldFromCurrentGetter(stateMachineType) is { } currentField))
        {
            WriteSkipWarning("'Current' getter with field reference not found.");
            return;
        }

        var moveNextMethodBody = moveNextMethod.Body;
        var instructions = moveNextMethodBody.Instructions;

        moveNextMethodBody.SimplifyMacros();

        for (int index = 1; index < instructions.Count; index++)
        {
            var instruction = instructions[index];

            // Inject checks before all successful MoveNext operations.

            if (instruction.OpCode == OpCodes.Ret && instruction.Previous.OpCode == OpCodes.Ldc_I4 && (int)instruction.Previous.Operand == 1)
            {
                index--;
                instructions.Insert(index++, Instruction.Create(OpCodes.Ldarg_0));
                instructions.Insert(index++, Instruction.Create(OpCodes.Ldfld, currentField));
                ILHelpers.InsertGetValueRef(ref index, instructions, currentField.FieldType);

                const string message = "Enumerator result nullability contract was broken.";
                ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
                    ref index, instructions, moduleReferences.ThrowOutputNullMethod, message, instructions[index]);
                index += 2;
            }
        }

        moveNextMethodBody.OptimizeMacros();

        void WriteSkipWarning(string message) => weavingContext.WriteWarning($"Skipping iterator state machine result checks on method '{method}': {message}");
    }

    private static void InjectAsyncStateMachineCheck(MethodDefinition method, TypeDefinition stateMachineType, WeavingContext weavingContext)
    {
        var bclReferences = weavingContext.BclReferences;
        var moduleReferences = weavingContext.ModuleReferences;

        if (!(GetMoveNextMethod(stateMachineType) is { } moveNextMethod))
        {
            WriteSkipWarning("'MoveNext' method not found.");
            return;
        }

        var moveNextMethodBody = moveNextMethod.Body;
        var instructions = moveNextMethodBody.Instructions;

        if (!(GetSetResultMethodRef(instructions, out int index) is { } setResultMethodRef))
        {
            WriteSkipWarning("'SetResult' method not found.");
            return;
        }

        if (!(GetSetExceptionMethodRef(setResultMethodRef.DeclaringType, bclReferences) is { } setExceptionMethodRef))
        {
            WriteSkipWarning("'SetException' method not found.");
            return;
        }

        var resultType = ((GenericInstanceType)setResultMethodRef.DeclaringType).GenericArguments[0];
        moveNextMethodBody.SimplifyMacros(); // For reliability just in case codegen is different and branches into this section of instructions exists

        // Keep instruction that loads result value where is it so that branches stay pointed to it but insert another one that stays with the SetResult call.

        var oldLoadResultInstruction = instructions[--index];
        var newLoadResultInstruction = oldLoadResultInstruction.Clone();
        instructions.Insert(++index, newLoadResultInstruction); // Don't increment index so this stays with the SetResult call

        // Value on stack is the result value

        ILHelpers.InsertGetValueRef(ref index, instructions, resultType);

        ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
            ref index, instructions, moduleReferences.GetAsyncResultNullExceptionMethod, AsyncTaskResultMessage, newLoadResultInstruction);

        instructions.Insert(index++, Instruction.Create(OpCodes.Call, setExceptionMethodRef));
        instructions.Insert(index++, Instruction.Create(OpCodes.Ret));

        moveNextMethodBody.OptimizeMacros();

        void WriteSkipWarning(string message) => weavingContext.WriteWarning($"Skipping async state machine result check on method '{method}': {message}");
    }

    private static void InjectAsyncIteratorStateMachineChecks(MethodDefinition method, TypeDefinition stateMachineType, WeavingContext weavingContext)
    {
        var bclReferences = weavingContext.BclReferences;
        var moduleReferences = weavingContext.ModuleReferences;

        if (!(GetMoveNextMethod(stateMachineType) is { } moveNextMethod))
        {
            WriteSkipWarning("'MoveNext' method not found on state machine.");
            return;
        }

        var moveNextMethodBody = moveNextMethod.Body;
        var instructions = moveNextMethodBody.Instructions;

        if (!(GetSetResultMethodRef(instructions, out int index) is { } setResultMethodRef))
        {
            WriteSkipWarning("'SetResult' method not found.");
            return;
        }

        if (instructions[index - 1].OpCode != OpCodes.Ldc_I4_1)
        {
            WriteSkipWarning("Unexpected state machine implementation - 'SetResult' method argument was not true.");
            return;
        }

        if (!(GetSetExceptionMethodRef(setResultMethodRef.DeclaringType, bclReferences) is { } setExceptionMethodRef))
        {
            WriteSkipWarning("'SetException' method not found.");
            return;
        }

        if (!(GetFieldFromCurrentGetter(stateMachineType) is { } currentField))
        {
            WriteSkipWarning("'Current' getter with field reference not found.");
            return;
        }

        moveNextMethodBody.SimplifyMacros(); // For reliability just in case codegen is different and branches into this section of instructions exists

        // Go back one instruction to keep the argument for SetResult together with the call

        var oldBlockStart = instructions[--index];

        // Stack currently has the SetException/SetResult object on it

        instructions.Insert(index++, Instruction.Create(OpCodes.Ldarg_0));
        instructions.Insert(index++, Instruction.Create(OpCodes.Ldfld, currentField));
        ILHelpers.InsertGetValueRef(ref index, instructions, currentField.FieldType);

        const string message = "Async enumerator result nullability contract was broken.";
        ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
            ref index, instructions, moduleReferences.GetAsyncResultNullExceptionMethod, message, oldBlockStart);

        instructions.Insert(index++, Instruction.Create(OpCodes.Call, setExceptionMethodRef));
        instructions.Insert(index++, Instruction.Create(OpCodes.Ret));

        moveNextMethodBody.OptimizeMacros();

        void WriteSkipWarning(string message) =>
            weavingContext.WriteWarning($"Skipping async iterator state machine result checks on method '{method}': {message}");
    }

    /// <summary>
    /// Checks the result that a runtime async method returns. The runtime stores the exception the method throws in its task, like the check for
    /// async state machines does with <c>SetException</c>.
    /// </summary>
    private static void InjectRuntimeAsyncResultCheck(
        MethodDefinition method, TypeReference resultType, ReturnBlockInfo returnBlockInfo, ModuleReferences moduleReferences)
    {
        var instructions = method.Body.Instructions;
        var returnBlockStartPoints = returnBlockInfo.NewStartPoints;

        for (int i = 0; i < returnBlockStartPoints.Length; i++)
        {
            var returnBlockStartPoint = returnBlockStartPoints[i];
            int index = instructions.LastIndexOf(returnBlockStartPoint);

            // Value on stack is the result

            var firstInstruction = Instruction.Create(OpCodes.Dup);
            instructions.Insert(index++, firstInstruction);
            ILHelpers.InsertGetValueRef(ref index, instructions, resultType);

            ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
                ref index, instructions, moduleReferences.GetAsyncResultNullExceptionMethod, AsyncTaskResultMessage, returnBlockStartPoint, returnBlockInfo);

            instructions.Insert(index++, Instruction.Create(OpCodes.Throw));

            returnBlockStartPoints[i] = firstInstruction;
        }
    }

    private static bool InjectSynchronousTaskResultCheck(
        MethodDefinition method, GenericInstanceType taskType, ReturnBlockInfo returnBlockInfo, WeavingContext weavingContext)
    {
        var bclReferences = weavingContext.BclReferences;
        var moduleReferences = weavingContext.ModuleReferences;

        var genericParameter = taskType.ResolveOrThrow().GenericParameters[0];

        var isCompletedGetter = new MethodReference("get_IsCompleted", bclReferences.BoolType, taskType) { HasThis = true };
        var isCanceledGetter = new MethodReference("get_IsCanceled", bclReferences.BoolType, taskType) { HasThis = true };
        var isFaultedGetter = new MethodReference("get_IsFaulted", bclReferences.BoolType, taskType) { HasThis = true };
        var resultGetter = new MethodReference("get_Result", genericParameter, taskType) { HasThis = true };

        if (isCompletedGetter.Resolve() == null)
        {
            WriteSkipWarning("'IsComplete' getter not found.");
            return false;
        }

        if (isCanceledGetter.Resolve() == null)
        {
            WriteSkipWarning("'IsCanceled' getter not found.");
            return false;
        }

        if (isFaultedGetter.Resolve() == null)
        {
            WriteSkipWarning("'IsFaulted' getter not found.");
            return false;
        }

        if (resultGetter.Resolve() == null)
        {
            WriteSkipWarning("'Result' getter not found.");
            return false;
        }

        var resultType = taskType.GenericArguments[0];
        var instructions = method.Body.Instructions;
        var returnBlockStartPoints = returnBlockInfo.NewStartPoints;

        if (taskType.IsValueType)
        {
            // Value type task object

            var returnTaskVar = new VariableDefinition(taskType);
            method.Body.Variables.Add(returnTaskVar);

            for (int i = 0; i < returnBlockStartPoints.Length; i++)
            {
                Instruction returnBlockStartPoint = returnBlockStartPoints[i];
                int index = instructions.LastIndexOf(returnBlockStartPoint);

                // Value on stack is the return task

                var firstInstruction = Instruction.Create(OpCodes.Dup);
                instructions.Insert(index++, firstInstruction);
                instructions.Insert(index++, Instruction.Create(OpCodes.Stloc, returnTaskVar));

                instructions.Insert(index++, Instruction.Create(OpCodes.Ldloca, returnTaskVar));
                instructions.Insert(index++, Instruction.Create(OpCodes.Call, isCompletedGetter));
                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(OpCodes.Brfalse_S, returnBlockStartPoint));

                instructions.Insert(index++, Instruction.Create(OpCodes.Ldloca, returnTaskVar));
                instructions.Insert(index++, Instruction.Create(OpCodes.Call, isCanceledGetter));
                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(OpCodes.Brtrue_S, returnBlockStartPoint));

                instructions.Insert(index++, Instruction.Create(OpCodes.Ldloca, returnTaskVar));
                instructions.Insert(index++, Instruction.Create(OpCodes.Call, isFaultedGetter));
                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(OpCodes.Brtrue_S, returnBlockStartPoint));

                instructions.Insert(index++, Instruction.Create(OpCodes.Ldloca, returnTaskVar));
                instructions.Insert(index++, Instruction.Create(OpCodes.Call, resultGetter));
                ILHelpers.InsertGetValueRef(ref index, instructions, resultType);

                const string message = "Task result nullability contract was broken.";
                ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
                    ref index, instructions, moduleReferences.ThrowOutputNullMethod, message, returnBlockStartPoint, returnBlockInfo);

                returnBlockStartPoints[i] = firstInstruction;
            }
        }
        else
        {
            // Reference type task object

            for (int i = 0; i < returnBlockStartPoints.Length; i++)
            {
                Instruction returnBlockStartPoint = returnBlockStartPoints[i];
                int index = instructions.LastIndexOf(returnBlockStartPoint);

                // Value on stack is the return task, which has no result to check if it's null (when its type allows that)

                var firstInstruction = Instruction.Create(OpCodes.Dup);
                instructions.Insert(index++, firstInstruction);
                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(OpCodes.Brfalse_S, returnBlockStartPoint));

                instructions.Insert(index++, Instruction.Create(OpCodes.Dup));
                instructions.Insert(index++, Instruction.Create(OpCodes.Call, isCompletedGetter));
                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(OpCodes.Brfalse_S, returnBlockStartPoint));

                instructions.Insert(index++, Instruction.Create(OpCodes.Dup));
                instructions.Insert(index++, Instruction.Create(OpCodes.Call, isCanceledGetter));
                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(OpCodes.Brtrue_S, returnBlockStartPoint));

                instructions.Insert(index++, Instruction.Create(OpCodes.Dup));
                instructions.Insert(index++, Instruction.Create(OpCodes.Call, isFaultedGetter));
                instructions.Insert(index++, returnBlockInfo.CreateBranchInstruction(OpCodes.Brtrue_S, returnBlockStartPoint));

                instructions.Insert(index++, Instruction.Create(OpCodes.Dup));
                instructions.Insert(index++, Instruction.Create(OpCodes.Call, resultGetter));
                ILHelpers.InsertGetValueRef(ref index, instructions, resultType);

                const string message = "Task result nullability contract was broken.";
                ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
                    ref index, instructions, moduleReferences.ThrowOutputNullMethod, message, returnBlockStartPoint, returnBlockInfo);

                returnBlockStartPoints[i] = firstInstruction;
            }
        }

        return true;

        void WriteSkipWarning(string message) => weavingContext.WriteWarning($"Skipping synchronous task result check on method '{method}': {message}");
    }

    private static MethodDefinition? GetMoveNextMethod(TypeDefinition type)
    {
        return type.Methods.FirstOrDefault(m => m.Name == "MoveNext" && m.Parameters.Count == 0 && !m.HasGenericParameters && m.HasBody);
    }

    private static MethodReference? GetSetResultMethodRef(Collection<Instruction> instructions, out int index)
    {
        for (int i = instructions.Count - 1; i >= 0; i--)
        {
            var instruction = instructions[i];

            if (instruction.OpCode == OpCodes.Call &&
                instruction.Operand is MethodReference methodRef &&
                methodRef.Name == "SetResult" &&
                methodRef.Parameters.Count == 1)
            {
                index = i;
                return methodRef;
            }
        }

        index = -1;
        return null;
    }

    private static MethodReference? GetSetExceptionMethodRef(TypeReference type, BclReferences bclReferences)
    {
        var methodRef = new MethodReference("SetException", bclReferences.VoidType, type) {
            HasThis = true,
            Parameters = { new ParameterDefinition(bclReferences.ExceptionType) },
        };

        if (methodRef.Resolve() == null)
            return null;

        return methodRef;
    }

    private static FieldReference? GetFieldFromCurrentGetter(TypeDefinition type)
    {
        var currentGetter = type.Methods.FirstOrDefault(m => m.Name.EndsWith("get_Current", StringComparison.Ordinal));

        if (currentGetter == null)
            return null;

        var instructions = currentGetter.Body.Instructions;

        return instructions.FirstOrDefault(i => i.OpCode == OpCodes.Ldfld)?.Operand as FieldReference;
    }
}
