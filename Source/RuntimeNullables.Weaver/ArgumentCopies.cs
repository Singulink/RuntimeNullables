using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Copies of a method's by-value arguments, saved when the method starts. Checks that run when the method returns use them, so they see the caller's
/// arguments even if the method assigns new values to its parameters.
/// </summary>
internal sealed class ArgumentCopies
{
    private readonly MethodDefinition _method;
    private readonly Dictionary<ParameterDefinition, VariableDefinition> _copies = [];

    public ArgumentCopies(MethodDefinition method)
    {
        _method = method;
    }

    /// <summary>
    /// Gets a value indicating whether any copies were added (at the start of the method).
    /// </summary>
    public bool HasCopies => _copies.Count > 0;

    /// <summary>
    /// Gets the local that holds the parameter's argument, adding the code that copies it at the start of the method if it isn't there yet.
    /// </summary>
    public VariableDefinition GetOrAdd(ParameterDefinition parameter)
    {
        if (!_copies.TryGetValue(parameter, out var copy))
        {
            copy = new VariableDefinition(parameter.ParameterType);
            _method.Body.Variables.Add(copy);

            var instructions = _method.Body.Instructions;
            instructions.Insert(0, Instruction.Create(OpCodes.Stloc, copy));
            instructions.Insert(0, Instruction.Create(OpCodes.Ldarg, parameter));

            _copies.Add(parameter, copy);
        }

        return copy;
    }
}
