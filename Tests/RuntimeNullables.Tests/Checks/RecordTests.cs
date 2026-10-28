using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class RecordTests
{
    [TestMethod]
    public void Constructor()
    {
        _ = new Record("test", 5);
        NullCheckAssert.ThrowsArgumentNull("Name", () => new Record(null!, 5));
    }

    [TestMethod]
    public void InitSetter()
    {
        var record = new Record("test", 5);

        _ = record with { Name = "test2" };
        NullCheckAssert.ThrowsArgumentNull("value", () => record = record with { Name = null! });
    }
}
