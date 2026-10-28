using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class PartlyNullableDisabledTests
{
    [TestMethod]
    public void Method() => NullCheckAssert.ThrowsArgumentNull("value", () => PartlyNullableDisabled.Method(null!));

    [TestMethod]
    public void NullableDisabledMethod() => PartlyNullableDisabled.NullableDisabledMethod(null);
}
