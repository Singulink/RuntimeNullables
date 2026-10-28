using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class PartlyGeneratedCodeTests
{
    [TestMethod]
    public void GeneratedAccessorProperty()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _ = PartlyGeneratedCode.GeneratedAccessorProperty);
    }

    [TestMethod]
    public void GeneratedPropertyWithAttribute() => Assert.IsNull(PartlyGeneratedCode.GeneratedPropertyWithAttribute);

    [TestMethod]
    public void Method() => NullCheckAssert.ThrowsArgumentNull("value", () => PartlyGeneratedCode.Method(null!));

    [TestMethod]
    public void GeneratedMethod() => PartlyGeneratedCode.GeneratedMethod(null!);

    [TestMethod]
    public void CompilerGeneratedMethod() => PartlyGeneratedCode.CompilerGeneratedMethod(null!);
}
