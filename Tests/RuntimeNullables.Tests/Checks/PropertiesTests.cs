using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class PropertiesTests
{
    [TestMethod]
    public void NullableReference()
    {
        var instance = new Properties();

        instance.NullableReference = null;
        _ = instance.NullableReference;
    }

    [TestMethod]
    public void NullableReferenceAuto()
    {
        var instance = new Properties();

        instance.NullableReferenceAuto = null;
        _ = instance.NullableReferenceAuto;
    }

    [TestMethod]
    public void Reference()
    {
        var instance = new Properties();

        NullCheckAssert.ThrowsArgumentNull("value", () => instance.Reference = null!);
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = instance.Reference);

        instance.Reference = string.Empty;
        _ = instance.Reference;
    }

    [TestMethod]
    public void ReferenceAuto()
    {
        var instance = new Properties();

        NullCheckAssert.ThrowsArgumentNull("value", () => instance.ReferenceAuto = null!);
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = instance.ReferenceAuto);

        instance.ReferenceAuto = string.Empty;
        _ = instance.ReferenceAuto;
    }

    [TestMethod]
    public void StaticReference()
    {
        // Other tests mustn't set the property, since the getter is only expected to throw while it's null.

        NullCheckAssert.ThrowsArgumentNull("value", () => Properties.StaticReference = null!);
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = Properties.StaticReference);
    }

    [TestMethod]
    public void Indexer()
    {
        var instance = new Properties();

        instance[string.Empty] = string.Empty;
        NullCheckAssert.ThrowsArgumentNull("key", () => instance[null!] = string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => instance[string.Empty] = null!);

        NullCheckAssert.ThrowsArgumentNull("key", () => _ = instance[null!]);
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = instance[string.Empty]);
    }
}
