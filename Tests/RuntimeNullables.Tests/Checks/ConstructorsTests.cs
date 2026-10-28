using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class ConstructorsTests
{
    [TestMethod]
    public void Constructor()
    {
        _ = new Constructors(string.Empty);
        NullCheckAssert.ThrowsArgumentNull("value", () => new Constructors(null!));
    }

    [TestMethod]
    public void ConstructorWithNullableParameter()
    {
        _ = new Constructors(null, 0);
    }
}
