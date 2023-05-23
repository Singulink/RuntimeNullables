using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using BclCollections = System.Collections.Generic;

namespace RuntimeNullables.Fody;

internal class ReturnBlockInfo
{
    public Instruction[] NewStartPoints { get; }

    public BclCollections.IReadOnlyList<Instruction> OldStartPoints { get; }

    /// <summary>
    /// Gets a list of branch instructions within the return block that should not be updated.
    /// </summary>
    public Collection<Instruction> Branches { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReturnBlockInfo"/> class for the given method, optionally setting the start points to the instruction
    /// previous to the return instruction.
    /// Creates.
    /// </summary>
    public ReturnBlockInfo(MethodDefinition method)
    {
        NewStartPoints = method.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray();
        OldStartPoints = (Instruction[])NewStartPoints.Clone();

        Branches = new Collection<Instruction>();
    }

    /// <summary>
    /// Creates a branch instruction and adds it to the list of branch instructions. All branching instructions injected into a return block by the weaver
    /// should be created with this method.
    /// </summary>
    public Instruction CreateBranchInstruction(OpCode opcode, Instruction operand)
    {
        var instruction = Instruction.Create(opcode, operand);
        Branches.Add(instruction);
        return instruction;
    }
}