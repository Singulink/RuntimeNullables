using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using RuntimeNullables.Weaver.Extensions;

namespace RuntimeNullables.Weaver.Contexts;

internal sealed class TypeContext : NullableContext
{
    private readonly List<MethodContext> _methodContexts = new();
    private List<GenericParameterInfo>? _genericParameters;

    public TypeDefinition Type { get; }

    public ModuleContext ModuleContext { get; }

    public TypeContext? DeclaringTypeContext { get; }

    public ICollection<MethodContext> MethodContexts => _methodContexts;

    public ICollection<GenericParameterInfo> GenericParameters => _genericParameters ??
        throw new InvalidOperationException("Type does not have generic parameters.");

    public TypeContext(TypeDefinition type, ModuleContext moduleContext) : base(type, moduleContext)
    {
        Type = type;
        ModuleContext = moduleContext;
    }

    public TypeContext(TypeDefinition type, TypeContext declaringTypeContext) : base(type, declaringTypeContext)
    {
        Type = type;
        DeclaringTypeContext = declaringTypeContext;
        ModuleContext = declaringTypeContext.ModuleContext;
    }

    public override void Build()
    {
        base.Build();

        if (Type.HasGenericParameters)
            _genericParameters = Type.GenericParameters.Select(p => new GenericParameterInfo(p, IsGenericParameterNullable(p))).ToList();

        var propertyContexts = Type.Properties
            .Where(p => (p.GetMethod ?? p.SetMethod).HasBody && !p.HasAnyGeneratedAttribute())
            .Select(p => new PropertyContext(p, this))
            .ToList();

        // C# 14 extension properties are declared in grouping types, which aren't checked. The code of their accessors is in implementation methods in
        // this type, which are checked as the accessors rather than as methods, so the properties' attributes apply to them.

        var extensionProperties = Type.NestedTypes.Where(t => t.IsExtensionGroupingType()).SelectMany(t => t.Properties).ToList();
        var extensionAccessorImplementations = new HashSet<MethodDefinition>();

        foreach (var property in extensionProperties)
        {
            var getMethod = property.GetMethod?.FindExtensionAccessorImplementation(WeavingContext);
            var setMethod = property.SetMethod?.FindExtensionAccessorImplementation(WeavingContext);

            extensionAccessorImplementations.UnionWith(new[] { getMethod, setMethod }.OfType<MethodDefinition>());

            if (!property.HasAnyGeneratedAttribute())
                propertyContexts.Add(new PropertyContext(property, this, getMethod, setMethod));
        }

        var methods = Type.Methods.Where(m =>
            m.HasBody && !m.IsGetter && !m.IsSetter && !m.HasAnyGeneratedAttribute() && !extensionAccessorImplementations.Contains(m));

        foreach (var method in methods)
        {
            var methodContext = new MethodContext(method, this);
            methodContext.Build();
            _methodContexts.Add(methodContext);
        }

        // Generated members aren't checked, so [NullChecks(true)] can't turn checks on for them.

        foreach (var method in Type.Methods.Where(m => !m.IsGetter && !m.IsSetter && m.HasAnyGeneratedAttribute()))
            WeavingContext.WarnAboutNullChecksAttributes(method, GetGeneratedCodeReason(method.Name));

        foreach (var property in Type.Properties.Concat(extensionProperties).Where(p => p.HasAnyGeneratedAttribute()))
            WeavingContext.WarnAboutNullChecksAttributes(property, GetGeneratedCodeReason(property.Name));

        foreach (var propertyContext in propertyContexts)
        {
            propertyContext.Build();

            if (propertyContext.GetMethodContext != null)
                _methodContexts.Add(propertyContext.GetMethodContext);

            if (propertyContext.SetMethodContext != null)
                _methodContexts.Add(propertyContext.SetMethodContext);
        }
    }

    /// <summary>
    /// Gets the reason why a generated item isn't checked, for warnings. Compiler-generated names start with '&lt;' (lambdas, local functions and the
    /// types the compiler makes for them).
    /// </summary>
    internal static string GetGeneratedCodeReason(string name)
    {
        return name.StartsWith("<", System.StringComparison.Ordinal) ?
            "lambdas and local functions are generated code, which isn't checked." :
            "it's generated code, which isn't checked.";
    }

    /// <summary>
    /// Determines nullability of a generic parameter, considering constraints.
    /// A generic parameter with a class constraint (HasReferenceTypeConstraint) is only nullable
    /// if explicitly annotated with NullableAttribute(2), i.e. "where T : class?".
    /// </summary>
    private bool IsGenericParameterNullable(GenericParameter parameter)
    {
        // If parameter has a class constraint, it's only nullable if explicitly marked as such
        if (parameter.HasReferenceTypeConstraint)
        {
            var nullableValue = parameter.GetNullableAttributeValue(0, WeavingContext);
            return nullableValue == NullableValue.Annotated;
        }

        // For other generic parameters, use the standard context-based nullability
        return IsContextItemNullable(parameter);
    }

    /// <summary>
    /// Walks the parent hierarchy of contexts to find the generic parameter and get its nullability.
    /// </summary>
    public bool GetGenericParameterIsNullable(GenericParameter parameter)
    {
        if (parameter.Owner != Type)
        {
            if (DeclaringTypeContext == null)
            {
                WeavingContext.WriteError($"Could not resolve generic parameter '{parameter}': owner '{parameter.Owner}' not found.");
                return true;
            }

            return DeclaringTypeContext.GetGenericParameterIsNullable(parameter);
        }

        var paramInfo = GenericParameters.FirstOrDefault(p => p.Parameter == parameter);

        if (paramInfo.Parameter == null)
        {
            WeavingContext.WriteError($"Could not resolve generic parameter '{parameter}': parameter not found.");
            return true;
        }

        return paramInfo.Nullable;
    }
}
