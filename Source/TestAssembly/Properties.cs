namespace TestAssembly;

/// <summary>
/// Properties are all initially null, even for non-nullable properties.
/// </summary>
public class Properties
{
    private string _reference = null!;
    private string? _nullableReference;

    public string ReferenceAuto { get; set; } = null!;

    public string Reference {
        get => _reference;
        set => _reference = value;
    }

    public string? NullableReferenceAuto { get; set; }

    public string? NullableReference {
        get => _nullableReference;
        set => _nullableReference = value;
    }
}