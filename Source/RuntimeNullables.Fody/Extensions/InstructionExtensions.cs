using Mono.Cecil.Cil;

namespace RuntimeNullables.Fody.Extensions;

public static class InstructionExtensions
{
    public static Instruction Clone(this Instruction instruction)
    {
        if (instruction.Operand == null)
            return Instruction.Create(instruction.OpCode);

        return Instruction.Create(instruction.OpCode, (dynamic)instruction.Operand);
    }
}