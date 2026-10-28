using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

/// <summary>
/// With the <c>class</c> constraint, <c>T</c> can't be null.
/// </summary>
[TestClass]
public class TypeClassConstraintTests
{
    private readonly TypeClassConstraint<string> _instance = new();

    [TestMethod]
    public void Direct()
    {
        _instance.Direct(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("param", () => _instance.Direct(null!));
    }

    [TestMethod]
    public void AllowNull() => Assert.IsNull(_instance.AllowNull(null));

    [TestMethod]
    public void MaybeNull()
    {
        // The ref parameter can't be null going in, but can be null coming out.

        string param = string.Empty;
        _instance.MaybeNull(ref param);
        Assert.IsNull(param);

        string nullParam = null!;
        NullCheckAssert.ThrowsArgumentNull("param", () => _instance.MaybeNull(ref nullParam));
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
        var instance = new TypeClassConstraint<string>.Nested();

        instance.Direct(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("param", () => instance.Direct(null!));
    }
}
