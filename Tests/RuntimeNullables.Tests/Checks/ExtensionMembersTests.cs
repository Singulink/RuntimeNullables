using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class ExtensionMembersTests
{
    [TestMethod]
    public void Echo()
    {
        NullCheckAssert.ThrowsArgumentNull("text", () => ((string)null!).Echo(string.Empty));
        NullCheckAssert.ThrowsArgumentNull("value", () => string.Empty.Echo(null!));

        Assert.AreEqual("value", string.Empty.Echo("value"));
    }

    [TestMethod]
    public void BadReturn()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => string.Empty.BadReturn());
    }

    [TestMethod]
    public void Property()
    {
        NullCheckAssert.ThrowsArgumentNull("text", () => ((string)null!).Property = string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => string.Empty.Property = null!);
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = string.Empty.Property);
    }

    [TestMethod]
    public void UncheckedProperty()
    {
        ((string)null!).UncheckedProperty = null!;
        Assert.IsNull(((string)null!).UncheckedProperty);
    }

    [TestMethod]
    public void StaticProperty()
    {
        NullCheckAssert.ThrowsArgumentNull("value", () => string.StaticProperty = null!);
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = string.StaticProperty);
    }

    [TestMethod]
    public void NullableReceiver()
    {
        Assert.IsTrue(((string?)null).IsMissing);
    }

    [TestMethod]
    public void GenericProperty()
    {
        NullCheckAssert.ThrowsArgumentNull("list", () => _ = ((List<string>)null!).FirstItem);
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = new List<string> { null! }.FirstItem);

        Assert.AreEqual("item", new List<string> { "item" }.FirstItem);
    }

    [TestMethod]
    public void GenericMethod()
    {
        NullCheckAssert.ThrowsArgumentNull("list", () => ((List<string>)null!).FirstItemOrNull());

        Assert.IsNull(new List<string>().FirstItemOrNull());
    }

    [TestMethod]
    public void UncheckedGenericProperty()
    {
        Assert.IsNull(new List<string> { null! }.UncheckedFirstItem);
    }
}
