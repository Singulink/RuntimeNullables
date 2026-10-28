using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using RuntimeNullables.Weaver.Extensions;

namespace RuntimeNullables.Weaver.Contexts;

internal sealed class ModuleContext : NullableContext
{
    private readonly Dictionary<TypeDefinition, TypeContext> _typeContexts = new();

    public ModuleDefinition Module { get; }

    public IReadOnlyDictionary<TypeDefinition, TypeContext> TypeContexts => _typeContexts;

    public ModuleContext(ModuleDefinition module, WeavingContext weavingContext) : base(module, new AssemblyContext(module.Assembly, weavingContext))
    {
        Module = module;
    }

    public override void Build()
    {
        base.Build();

        foreach (var type in Module.GetTypes())
            Build(type);

        // [NullChecks(true)] can't turn checks on for a skipped type, so say so. Extension grouping types are left out, since the attributes of the
        // members declared in them apply to the implementation methods (see TypeContext).

        foreach (var type in Module.GetTypes().Where(t => !_typeContexts.ContainsKey(t) && !IsInExtensionGroupingType(t)))
            WeavingContext.WarnAboutNullChecksAttributes(type, GetSkipReason(type));

        TypeContext? Build(TypeDefinition type)
        {
            if (_typeContexts.TryGetValue(type, out var typeContext))
                return typeContext;

            if (type.HasAnyGeneratedAttribute() || IsExcludedNamespaceType(type) || type.IsExtensionGroupingType())
                return null;

            if (type.DeclaringType == null)
            {
                typeContext = new TypeContext(type, this);
            }
            else
            {
                var declaringTypeContext = Build(type.DeclaringType);

                // Types nested in a skipped type are skipped too. Like Roslyn, everything inside a generated type counts as generated.

                if (declaringTypeContext == null)
                    return null;

                typeContext = new TypeContext(type, declaringTypeContext);
            }

            typeContext.Build();
            _typeContexts.Add(type, typeContext);

            return typeContext;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the type is in a namespace where projects put copies of the attributes the weaver reads and their own throw
    /// helpers, which aren't checked.
    /// </summary>
    private static bool IsExcludedNamespaceType(TypeDefinition type)
    {
        return type.Namespace is "RuntimeNullables" or "System.Runtime.CompilerServices" or "System.Diagnostics.CodeAnalysis";
    }

    /// <summary>
    /// Gets a value indicating whether the type is a C# 14 extension grouping type or one of its nested marker types.
    /// </summary>
    private static bool IsInExtensionGroupingType(TypeDefinition type)
    {
        return type.IsExtensionGroupingType() || type.DeclaringType?.IsExtensionGroupingType() == true;
    }

    /// <summary>
    /// Gets the reason why a skipped type isn't checked, for warnings: the type itself, or a type it's nested in, is generated code or in an excluded
    /// namespace.
    /// </summary>
    private static string GetSkipReason(TypeDefinition type)
    {
        for (var skippedType = type; skippedType != null; skippedType = skippedType.DeclaringType)
        {
            if (skippedType.HasAnyGeneratedAttribute())
            {
                return skippedType.Name.StartsWith("<", System.StringComparison.Ordinal) ?
                    TypeContext.GetGeneratedCodeReason(skippedType.Name) :
                    $"'{skippedType}' is generated code, which isn't checked.";
            }

            if (IsExcludedNamespaceType(skippedType))
                return $"'{skippedType}' is in the {skippedType.Namespace} namespace, which isn't checked.";
        }

        throw new RuntimeNullablesException($"Internal error: '{type}' isn't a skipped type.");
    }
}
