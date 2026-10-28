using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class NullableDisabledTests
{
    [TestMethod]
    public void NonNull() => NullableDisabled.NonNull(null);

    [TestMethod]
    public void DisallowNullInput() => NullCheckAssert.ThrowsArgumentNull("value", () => NullableDisabled.DisallowNullInput(null!));

    [TestMethod]
    public void BadNotNullOutput()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.OutputParameter("value"), () => NullableDisabled.BadNotNullOutput(out _));
    }
}
