using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace RuntimeNullables.Fody.Extensions
{
    internal static class InstructionCollectionExtensions
    {
        public static int LastIndexOf(this Collection<Instruction> collection, Instruction item)
        {
            for (int i = collection.Count - 1; i >= 0; i--) {
                if (collection[i] == item)
                    return i;
            }

            return -1;
        }
    }
}
