using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Removes the module's reference to the RuntimeNullables assembly, which is only needed at compile time for <c>[NullChecks]</c> attributes.
/// </summary>
/// <remarks>
/// <para>The weaver removes all <c>[NullChecks]</c> attributes. This class removes the debug information (such as <c>using</c> aliases and local
/// constants in the PDB) that refers to RuntimeNullables types. The assembly reference is only removed if nothing else in the module still refers to it, since
/// removing a reference that is still in use would corrupt the written metadata.</para>
/// </remarks>
internal sealed class ReferenceCleaner
{
    private const string RuntimeNullablesAssemblyName = "RuntimeNullables";
    private const int MaxReportedUsages = 5;

    private readonly HashSet<ImportDebugInformation> _visitedImports = [];
    private readonly List<string> _remainingUsages = [];

    private int _removedDebugInfoCount;
    private bool _hasUnreportedUsages;

    private ReferenceCleaner() { }

    /// <summary>
    /// Removes the RuntimeNullables assembly reference from the module if present.
    /// </summary>
    /// <returns><see langword="true"/> if the module no longer references RuntimeNullables, otherwise <see langword="false"/> (an error is logged).</returns>
    public static bool Clean(ModuleDefinition module, IWeaverLogger logger)
    {
        var references = module.AssemblyReferences.Where(r => r.Name == RuntimeNullablesAssemblyName).ToList();

        if (references.Count == 0)
            return true;

        var cleaner = new ReferenceCleaner();
        cleaner.VisitModule(module);

        if (cleaner._removedDebugInfoCount > 0)
            logger.LogInfo($"Removed {cleaner._removedDebugInfoCount} debug information item(s) that referred to RuntimeNullables.");

        if (cleaner._remainingUsages.Count > 0)
        {
            string usages = string.Join("; ", cleaner._remainingUsages) + (cleaner._hasUnreportedUsages ? "; ..." : string.Empty);

            logger.LogError(
                "The RuntimeNullables assembly is only available at compile time, so its types may only be used in [NullChecks] attributes. " +
                $"Remove these uses: {usages}");

            return false;
        }

        foreach (var reference in references)
            module.AssemblyReferences.Remove(reference);

        logger.LogInfo($"Removed reference to '{RuntimeNullablesAssemblyName}'.");
        return true;
    }

    private void VisitModule(ModuleDefinition module)
    {
        if (module.Assembly != null)
            VisitAttributes(module.Assembly, module.Assembly);

        VisitAttributes(module, module);

        foreach (var type in module.GetTypes())
            VisitType(type, module.HasSymbols);

        foreach (var exportedType in module.ExportedTypes)
        {
            if (IsRuntimeNullablesScope(exportedType.Scope))
                AddUsage($"type forwarder for '{exportedType.FullName}'");
        }

        foreach (var resource in module.Resources.OfType<AssemblyLinkedResource>())
        {
            if (IsRuntimeNullablesScope(resource.Assembly))
                AddUsage($"linked resource '{resource.Name}'");
        }
    }

    private void VisitType(TypeDefinition type, bool hasSymbols)
    {
        VisitAttributes(type, type);
        CheckType(type.BaseType, type);
        VisitGenericParameters(type, type);

        foreach (var interfaceImpl in type.Interfaces)
        {
            CheckType(interfaceImpl.InterfaceType, type);
            VisitAttributes(interfaceImpl, type);
        }

        foreach (var field in type.Fields)
        {
            CheckType(field.FieldType, field);
            VisitAttributes(field, field);
        }

        foreach (var property in type.Properties)
        {
            CheckType(property.PropertyType, property);
            VisitAttributes(property, property);
        }

        foreach (var @event in type.Events)
        {
            CheckType(@event.EventType, @event);
            VisitAttributes(@event, @event);
        }

        foreach (var method in type.Methods)
            VisitMethod(method, hasSymbols);
    }

    private void VisitMethod(MethodDefinition method, bool hasSymbols)
    {
        VisitAttributes(method, method);
        VisitAttributes(method.MethodReturnType, method);
        CheckType(method.ReturnType, method);
        VisitGenericParameters(method, method);

        foreach (var parameter in method.Parameters)
        {
            CheckType(parameter.ParameterType, method);
            VisitAttributes(parameter, method);
        }

        foreach (var overriddenMethod in method.Overrides)
            CheckMember(overriddenMethod, method);

        if (method.HasBody)
        {
            var body = method.Body;

            foreach (var variable in body.Variables)
                CheckType(variable.VariableType, method);

            foreach (var handler in body.ExceptionHandlers)
                CheckType(handler.CatchType, method);

            foreach (var instruction in body.Instructions)
                CheckOperand(instruction.Operand, method);
        }

        if (hasSymbols && method.DebugInformation.Scope is { } scope)
            VisitScope(scope);
    }

    private void VisitGenericParameters(IGenericParameterProvider provider, object owner)
    {
        foreach (var genericParameter in provider.GenericParameters)
        {
            VisitAttributes(genericParameter, owner);

            foreach (var constraint in genericParameter.Constraints)
            {
                CheckType(constraint.ConstraintType, owner);
                VisitAttributes(constraint, owner);
            }
        }
    }

    private void VisitAttributes(ICustomAttributeProvider provider, object owner)
    {
        if (!provider.HasCustomAttributes)
            return;

        // Only the constructor is a metadata reference. Types in attribute arguments (typeof and enum values) are stored as type names.

        foreach (var attribute in provider.CustomAttributes)
            CheckMember(attribute.Constructor, owner);
    }

    private void VisitScope(ScopeDebugInformation scope)
    {
        for (var import = scope.Import; import != null && _visitedImports.Add(import); import = import.Parent)
        {
            var targets = import.Targets;

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];

                if (IsRuntimeNullablesType(target.Type) || IsRuntimeNullablesScope(target.AssemblyReference))
                {
                    targets.RemoveAt(i);
                    _removedDebugInfoCount++;
                }
            }
        }

        if (scope.HasConstants)
        {
            var constants = scope.Constants;

            for (int i = constants.Count - 1; i >= 0; i--)
            {
                if (IsRuntimeNullablesType(constants[i].ConstantType))
                {
                    constants.RemoveAt(i);
                    _removedDebugInfoCount++;
                }
            }
        }

        if (scope.HasScopes)
        {
            foreach (var childScope in scope.Scopes)
                VisitScope(childScope);
        }
    }

    private void CheckOperand(object? operand, MethodDefinition owner)
    {
        switch (operand)
        {
            case TypeReference type:
                CheckType(type, owner);
                break;
            case MemberReference member:
                CheckMember(member, owner);
                break;
            case CallSite callSite:
                CheckType(callSite.ReturnType, owner);

                foreach (var parameter in callSite.Parameters)
                    CheckType(parameter.ParameterType, owner);

                break;
        }
    }

    private void CheckMember(MemberReference member, object owner)
    {
        CheckType(member.DeclaringType, owner);

        switch (member)
        {
            case FieldReference field:
                CheckType(field.FieldType, owner);
                break;
            case MethodReference method:
                CheckType(method.ReturnType, owner);

                foreach (var parameter in method.Parameters)
                    CheckType(parameter.ParameterType, owner);

                if (method is GenericInstanceMethod genericInstanceMethod)
                {
                    foreach (var genericArgument in genericInstanceMethod.GenericArguments)
                        CheckType(genericArgument, owner);
                }

                break;
        }
    }

    private void CheckType(TypeReference? type, object owner)
    {
        if (IsRuntimeNullablesType(type))
            AddUsage($"'{owner}' references '{type}'");
    }

    private void AddUsage(string usage)
    {
        if (_remainingUsages.Contains(usage))
            return;

        if (_remainingUsages.Count < MaxReportedUsages)
            _remainingUsages.Add(usage);
        else
            _hasUnreportedUsages = true;
    }

    private static bool IsRuntimeNullablesType(TypeReference? type)
    {
        return type switch
        {
            null or GenericParameter => false,
            GenericInstanceType genericInstance => IsRuntimeNullablesType(genericInstance.ElementType) ||
                genericInstance.GenericArguments.Any(IsRuntimeNullablesType),
            IModifierType modifierType => IsRuntimeNullablesType(modifierType.ModifierType) || IsRuntimeNullablesType(modifierType.ElementType),
            FunctionPointerType functionPointer => IsRuntimeNullablesType(functionPointer.ReturnType) ||
                functionPointer.Parameters.Any(p => IsRuntimeNullablesType(p.ParameterType)),
            TypeSpecification specification => IsRuntimeNullablesType(specification.ElementType),
            _ => IsRuntimeNullablesScope(type.Scope),
        };
    }

    private static bool IsRuntimeNullablesScope(IMetadataScope? scope) => scope is AssemblyNameReference { Name: RuntimeNullablesAssemblyName };
}
