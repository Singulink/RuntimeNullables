using RuntimeNullables;

#pragma warning disable CA1801 // Review unused parameters

namespace TestAssembly;

/// <summary>
/// [NullChecks] on constructors, properties, accessors and a method, which also turns off its return value check (see <see cref="NestedContainer"/> and
/// <see cref="Returns"/> for types and return values).
/// </summary>
public class NullChecksAttributes
{
    private string _value = null!;

    public NullChecksAttributes() { }

    [NullChecks(false)]
    public NullChecksAttributes(string value) { }

    [NullChecks(false)]
    public string UncheckedProperty
    {
        get => _value;
        set => _value = value;
    }

    [NullChecks(false)]
    public string CheckedGetterProperty
    {
        [NullChecks(true)]
        get => _value;
        set => _value = value;
    }

    public string UncheckedSetterProperty
    {
        get => _value;

        [NullChecks(false)]
        set => _value = value;
    }

    [NullChecks(false)]
    public static string UncheckedMethod(string value) => null!;
}
