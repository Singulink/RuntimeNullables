using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class StructMembersTests
{
    [TestMethod]
    public void Constructor()
    {
        _ = new StructMembers(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => new StructMembers(null!));
    }

    [TestMethod]
    public void Method()
    {
        var instance = default(StructMembers);

        instance.Method(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => instance.Method(null!));
    }

    [TestMethod]
    public void BadReturn()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => default(StructMembers).BadReturn());
    }
}
