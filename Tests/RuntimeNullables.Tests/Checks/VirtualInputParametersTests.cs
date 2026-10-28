using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class VirtualInputParametersTests
{
    private readonly VirtualInputParameters _instance = new();

    [TestMethod]
    public void InReference()
    {
        string value = null!;
        NullCheckAssert.ThrowsArgumentNull("value", () => _instance.InReference(in value));
    }

    [TestMethod]
    public void GenericInReference()
    {
        string value = null!;
        NullCheckAssert.ThrowsArgumentNull("value", () => _instance.GenericInReference(in value));
    }
}
