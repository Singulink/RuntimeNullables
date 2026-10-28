using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class NullChecksAttributesTests
{
    [TestMethod]
    public void UncheckedConstructor()
    {
        _ = new NullChecksAttributes(null!);
    }

    [TestMethod]
    public void UncheckedProperty()
    {
        var instance = new NullChecksAttributes();

        _ = instance.UncheckedProperty;
        instance.UncheckedProperty = null!;
    }

    [TestMethod]
    public void CheckedGetterProperty()
    {
        var instance = new NullChecksAttributes();

        instance.CheckedGetterProperty = null!;
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = instance.CheckedGetterProperty);
    }

    [TestMethod]
    public void UncheckedSetterProperty()
    {
        var instance = new NullChecksAttributes();

        instance.UncheckedSetterProperty = null!;
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = instance.UncheckedSetterProperty);
    }

    [TestMethod]
    public void UncheckedMethod()
    {
        // Turning checks off for a method turns them off for its return value too.
        NullChecksAttributes.UncheckedMethod(null!);
    }
}
