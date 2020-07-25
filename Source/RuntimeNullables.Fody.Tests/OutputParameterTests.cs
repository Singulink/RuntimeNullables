using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class OutputParameterTests
    {
        [TestMethod]
        public void GoodReference() => OutputParameters.GoodReference(out _);

        [TestMethod]
        public void BadReference() => Assert.ThrowsException<NullReferenceException>(() => OutputParameters.BadReference(out _));

        [TestMethod]
        public void NullableReference() => OutputParameters.NullableReference(out _);

        [TestMethod]
        public void GoodGenericNotNullConstraint() => OutputParameters.GoodGenericNotNullConstraint<object>(out _);

        [TestMethod]
        public void BadGenericNotNull() => Assert.ThrowsException<NullReferenceException>(() => OutputParameters.BadGenericNotNullConstraint<object>(out _));

        [TestMethod]
        public void GoodGenericNotNullAttribute() => OutputParameters.GoodGenericNotNullAttribute<object>(out _);

        [TestMethod]
        public void BadGenericNotNullAttribute() => Assert.ThrowsException<NullReferenceException>(() => OutputParameters.BadGenericNotNullAttribute<object>(out _));

        [TestMethod]
        public void GenericMaybeNullAttribute() => OutputParameters.GenericMaybeNullAttribute<object>(out _);

        [TestMethod]
        public void GenericUnconstrained() => OutputParameters.GenericUnconstrained<object>(out _);
    }
}
