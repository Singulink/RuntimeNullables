using Mono.Cecil.Cil;

namespace RuntimeNullables.Weaver.Extensions;

internal static class InstructionExtensions
{
    /// <summary>
    /// Creates a new instruction with the same opcode and operand.
    /// </summary>
    public static Instruction Clone(this Instruction instruction)
    {
        // Instruction.Create has an overload for each operand type, so set the opcode and operand directly instead of picking one.

        var clone = Instruction.Create(OpCodes.Nop);
        clone.OpCode = instruction.OpCode;
        clone.Operand = instruction.Operand;

        return clone;
    }
}
