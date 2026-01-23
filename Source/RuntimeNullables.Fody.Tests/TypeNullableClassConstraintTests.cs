using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class TypeNullableClassConstraintTests
{
    [TestMethod]
    public void Direct_NullParameter_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>();
        instance.Direct(null);
    }

    [TestMethod]
    public void Direct_NonNullParameter_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>();
        instance.Direct(string.Empty);
    }

    [TestMethod]
    public void AllowNull_NullParameter_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>();
        instance.AllowNull(null);
    }

    [TestMethod]
    public void MaybeNull_RefParameter_SetToNull_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>();
        string param = string.Empty;
        instance.MaybeNull(ref param);
        Assert.IsNull(param);
    }

    [TestMethod]
    public void Nullable_NullParameter_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>();
        string result = instance.Nullable(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Nullable_NonNullParameter_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>();
        string result = instance.Nullable(string.Empty);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void NullableRef_RefParameter_NonNull_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>();
        string param = string.Empty;
        instance.NullableRef(ref param);
        Assert.IsNull(param);
    }

    [TestMethod]
    public void NullableRef_RefParameter_Null_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>();
        string param = null;
        instance.NullableRef(ref param);
        Assert.IsNull(param);
    }

    [TestMethod]
    public void Nested_Direct_NullParameter_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>.Nested();
        instance.Direct(null);
    }

    [TestMethod]
    public void Nested_Direct_NonNullParameter_DoesNotThrow()
    {
        var instance = new TestAssembly.TypeNullableClassConstraint<string?>.Nested();
        instance.Direct(string.Empty);
    }
}