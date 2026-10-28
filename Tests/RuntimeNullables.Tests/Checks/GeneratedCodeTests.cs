using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class GeneratedCodeTests
{
    [TestMethod]
    public void Method() => GeneratedCode.Method(null!);

    [TestMethod]
    public void NestedMethod() => GeneratedCode.Nested.Method(null!);
}
