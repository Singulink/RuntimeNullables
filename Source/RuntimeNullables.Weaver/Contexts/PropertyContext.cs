using Mono.Cecil;

namespace RuntimeNullables.Weaver.Contexts;

internal sealed class PropertyContext : NullableContext
{
    private readonly MethodDefinition? _getMethod;
    private readonly MethodDefinition? _setMethod;

    public PropertyDefinition Property { get; }

    public TypeContext TypeContext { get; }

    public MethodContext? GetMethodContext { get; private set; }

    public MethodContext? SetMethodContext { get; private set; }

    public PropertyContext(PropertyDefinition property, TypeContext typeContext) : this(property, typeContext, property.GetMethod, property.SetMethod)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyContext"/> class with the methods that have the code of the property's accessors, which
    /// are the implementation methods in the type for C# 14 extension properties.
    /// </summary>
    public PropertyContext(PropertyDefinition property, TypeContext typeContext, MethodDefinition? getMethod, MethodDefinition? setMethod)
        : base(property, typeContext)
    {
        Property = property;
        TypeContext = typeContext;

        _getMethod = getMethod;
        _setMethod = setMethod;
    }

    public override void Build()
    {
        base.Build();

        if (_getMethod != null)
        {
            GetMethodContext = new MethodContext(_getMethod, this);
            GetMethodContext.Build();
        }

        if (_setMethod != null)
        {
            SetMethodContext = new MethodContext(_setMethod, this);
            SetMethodContext.Build();
        }
    }
}
