using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class ReturnTests
    {
        [TestMethod]
        public void GoodReference() => Returns.GoodReference();

        [TestMethod]
        public void BadReference() => Assert.ThrowsException<NullReferenceException>(Returns.BadReference);

        [TestMethod]
        public void NullableReference() => Returns.NullableReference();

        [TestMethod]
        public void NoNullChecksBadReference() => Returns.NoNullChecksBadReference();

        [TestMethod]
        public void NullChecksOnNoNullChecksMethodBadReference() => Assert.ThrowsException<NullReferenceException>(Returns.NullChecksOnNoNullChecksMethodBadReference);

        [TestMethod]
        public void GoodGenericNotNullConstraint() => Returns.GoodGenericNotNullConstraint<object>();

        [TestMethod]
        public void BadGenericNotNull() => Assert.ThrowsException<NullReferenceException>(Returns.BadGenericNotNullConstraint<object>);

        [TestMethod]
        public void GoodGenericNotNullAttribute() => Returns.GoodGenericNotNullAttribute<object>();

        [TestMethod]
        public void BadGenericNotNullAttribute() => Assert.ThrowsException<NullReferenceException>(Returns.BadGenericNotNullAttribute<object>);

        [TestMethod]
        public void GenericMaybeNullAttribute() => Returns.GenericMaybeNullAttribute<object>();

        [TestMethod]
        public void GenericUnconstrained() => Returns.GenericUnconstrained<object>();
    }
}
