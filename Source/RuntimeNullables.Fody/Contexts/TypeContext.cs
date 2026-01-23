using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace RuntimeNullables.Fody.Contexts;

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

        foreach (var method in Type.Methods.Where(m => m.HasBody && !m.IsGetter && !m.IsSetter && !m.HasAnyGeneratedAttribute())) {
            var methodContext = new MethodContext(method, this);
            methodContext.Build();
            _methodContexts.Add(methodContext);
        }

        foreach (var property in Type.Properties.Where(p => (p.GetMethod ?? p.SetMethod).HasBody && !p.HasAnyGeneratedAttribute())) {
            var propertyContext = new PropertyContext(property, this);
            propertyContext.Build();

            if (propertyContext.GetMethodContext != null)
                _methodContexts.Add(propertyContext.GetMethodContext);

            if (propertyContext.SetMethodContext != null)
                _methodContexts.Add(propertyContext.SetMethodContext);
        }
    }

    /// <summary>
    /// Determines nullability of a generic parameter, considering constraints.
    /// A generic parameter with a class constraint (HasReferenceTypeConstraint) is only nullable
    /// if explicitly annotated with NullableAttribute(2), i.e. "where T : class?".
    /// </summary>
    private bool IsGenericParameterNullable(GenericParameter parameter)
    {
        // If parameter has a class constraint, it's only nullable if explicitly marked as such
        if (parameter.HasReferenceTypeConstraint) {
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
        if (parameter.Owner != Type) {
            if (DeclaringTypeContext == null) {
                WeavingContext.WriteError($"Could not resolve generic parameter '{parameter}': owner '{parameter.Owner}' not found.");
                return true;
            }

            return DeclaringTypeContext.GetGenericParameterIsNullable(parameter);
        }

        var paramInfo = GenericParameters.FirstOrDefault(p => p.Parameter == parameter);

        if (paramInfo.Parameter == null) {
            WeavingContext.WriteError($"Could not resolve generic parameter '{parameter}': parameter not found.");
            return true;
        }

        return paramInfo.Nullable;
    }
}