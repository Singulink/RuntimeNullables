using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Tests;

/// <summary>
/// With the <c>class?</c> constraint, <c>T</c> can be null, so nothing is checked.
/// </summary>
[TestClass]
public class TypeNullableClassConstraintTests
{
    private readonly TypeNullableClassConstraint<string?> _instance = new();

    [TestMethod]
    public void Direct()
    {
        Assert.IsNull(_instance.Direct(null));
        Assert.IsNotNull(_instance.Direct(string.Empty));
    }

    [TestMethod]
    public void AllowNull() => Assert.IsNull(_instance.AllowNull(null));

    [TestMethod]
    public void MaybeNull()
    {
        string? param = string.Empty;
        _instance.MaybeNull(ref param);
        Assert.IsNull(param);
    }

    [TestMethod]
    public void Nullable()
    {
        Assert.IsNull(_instance.Nullable(null));
        Assert.IsNotNull(_instance.Nullable(string.Empty));
    }

    [TestMethod]
    public void NullableRef()
    {
        string? param = string.Empty;
        _instance.NullableRef(ref param);
        Assert.IsNull(param);

        param = null;
        _instance.NullableRef(ref param);
        Assert.IsNull(param);
    }

    [TestMethod]
    public void NestedDirect()
    {
        var instance = new TypeNullableClassConstraint<string?>.Nested();

        Assert.IsNull(instance.Direct(null));
        Assert.IsNotNull(instance.Direct(string.Empty));
    }
}
