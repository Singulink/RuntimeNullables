using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class NestedContainerTests
{
    [TestMethod]
    public void Unchecked()
    {
        NestedContainer.Nested.Unchecked(null!);
        NestedContainer.Nested.NestedDeeper.Unchecked(null!);
    }

    [TestMethod]
    public void Checked()
    {
        NullCheckAssert.ThrowsArgumentNull("value", () => NestedContainer.Nested.Checked(null!));
        NullCheckAssert.ThrowsArgumentNull("value", () => NestedContainer.Nested.NestedDeeper.Checked(null!));
    }
}
