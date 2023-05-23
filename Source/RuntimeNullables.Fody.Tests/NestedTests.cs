using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;
using TestAssemblyNonPublic;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class NestedTests
{
    [TestMethod]
    public void NoNullChecksNested()
    {
        NestedContainer.Nested.Unchecked(null!);
        NestedContainer.Nested.NestedDeeper.Unchecked(null!);
    }

    [TestMethod]
    public void NullChecksNested()
    {
        Assert.ThrowsException<ArgumentNullException>(() => NestedContainer.Nested.Checked(null!));
        Assert.ThrowsException<ArgumentNullException>(() => NestedContainer.Nested.NestedDeeper.Checked(null!));
    }

    [TestMethod]
    public void PublicContainingNestedNonPublicClass()
    {
        PublicContainingNestedNonPublic.Nested.Method(null!);
    }
}