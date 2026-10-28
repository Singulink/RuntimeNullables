using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using RuntimeNullables.Weaver.Extensions;

namespace RuntimeNullables.Weaver;

internal static class ILHelpers
{
    /// <summary>
    /// Converts the value on the stack to a value ref which can be used to test for null.
    /// </summary>
    public static void InsertGetValueRef(ref int index, Collection<Instruction> instructions, TypeReference valueType)
    {
        // Load the value a ref points to, then box generic values (a no-op for references) so they can be compared to null.

        valueType = valueType.WithoutModifiers();

        if (valueType.IsByReference)
        {
            valueType = valueType.WithoutRefAndModifiers();
            instructions.Insert(index++, valueType.IsGenericParameter ? Instruction.Create(OpCodes.Ldobj, valueType) : Instruction.Create(OpCodes.Ldind_Ref));
        }

        if (valueType.IsGenericParameter)
            instructions.Insert(index++, Instruction.Create(OpCodes.Box, valueType));
    }

    public static void InsertThrowHelperCallIfValueRefIsNull(
        ref int index,
        Collection<Instruction> instructions,
        MethodReference throwHelperMethod,
        string paramValue,
        Instruction continuationPoint,
        ReturnBlockInfo? returnBlockInfo = null)
    {
        var branchIfNotNull = Instruction.Create(OpCodes.Brtrue_S, continuationPoint);
        returnBlockInfo?.Branches.Add(branchIfNotNull);
        instructions.Insert(index++, branchIfNotNull);

        instructions.Insert(index++, Instruction.Create(OpCodes.Ldstr, paramValue));
        instructions.Insert(index++, Instruction.Create(OpCodes.Call, throwHelperMethod));
    }
}
