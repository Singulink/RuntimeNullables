using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class PropertyTests
{
    [TestMethod]
    public void NullableReference()
    {
        var instance = new Properties();

        instance.NullableReference = null;
        _ = instance.NullableReference;
    }

    [TestMethod]
    public void NullableReferenceAuto()
    {
        var instance = new Properties();

        instance.NullableReferenceAuto = null;
        _ = instance.NullableReferenceAuto;
    }

    [TestMethod]
    public void Reference()
    {
        var instance = new Properties();

        Assert.ThrowsException<ArgumentNullException>(() => instance.Reference = null!);
        Assert.ThrowsException<NullReferenceException>(() => instance.Reference);

        instance.Reference = string.Empty;
        _ = instance.Reference;
    }

    [TestMethod]
    public void ReferenceAuto()
    {
        var instance = new Properties();

        Assert.ThrowsException<ArgumentNullException>(() => instance.ReferenceAuto = null!);
        Assert.ThrowsException<NullReferenceException>(() => instance.ReferenceAuto);

        instance.ReferenceAuto = string.Empty;
        _ = instance.ReferenceAuto;
    }
}