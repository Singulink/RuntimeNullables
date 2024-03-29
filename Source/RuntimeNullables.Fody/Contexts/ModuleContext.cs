﻿using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace RuntimeNullables.Fody.Contexts;

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

        TypeContext? Build(TypeDefinition type)
        {
            if (_typeContexts.TryGetValue(type, out var typeContext))
                return typeContext;

            if (type.HasAnyGeneratedAttribute() || IsInjectedType(type))
                return null;

            if (type.DeclaringType == null) {
                typeContext = new TypeContext(type, this);
            }
            else {
                var declaringTypeContext = Build(type.DeclaringType);

                if (declaringTypeContext == null)
                    return null;

                typeContext = new TypeContext(type, declaringTypeContext);
            }

            typeContext.Build();
            _typeContexts.Add(type, typeContext);

            return typeContext;
        }

        static bool IsInjectedType(TypeDefinition type)
        {
            return type.Namespace is "RuntimeNullables" or "System.Runtime.CompilerServices" or "System.Diagnostics.CodeAnalysis";
        }
    }
}