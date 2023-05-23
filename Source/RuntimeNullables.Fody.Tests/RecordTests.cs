using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class RecordTests
{
    [TestMethod]
    public void Good()
    {
        var record = new Record("test", 5);
        record = record with { Name = "test2" };
    }

    [TestMethod]
    public void Bad()
    {
        var record = new Record("test", 5);
        Assert.ThrowsException<ArgumentNullException>(() => record = record with { Name = null! });
    }
}