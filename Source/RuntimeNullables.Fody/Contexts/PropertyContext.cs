using Mono.Cecil;

namespace RuntimeNullables.Fody.Contexts;

internal sealed class PropertyContext : NullableContext
{
    public PropertyDefinition Property { get; }

    public TypeContext TypeContext { get; }

    public MethodContext? GetMethodContext { get; private set; }

    public MethodContext? SetMethodContext { get; private set; }

    public PropertyContext(PropertyDefinition property, TypeContext typeContext) : base(property, typeContext)
    {
        Property = property;
        TypeContext = typeContext;
    }

    public override void Build()
    {
        base.Build();

        if (Property.GetMethod != null) {
            GetMethodContext = new MethodContext(Property.GetMethod, this);
            GetMethodContext.Build();
        }

        if (Property.SetMethod != null) {
            SetMethodContext = new MethodContext(Property.SetMethod, this);
            SetMethodContext.Build();
        }
    }
}