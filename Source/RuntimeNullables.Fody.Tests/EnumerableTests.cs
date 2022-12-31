using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class EnumerableTests
    {
        [TestMethod]
        public void GoodGetReferences()
        {
            foreach (var value in Enumerables.GoodGetReferences()) { }
        }

        [TestMethod]
        public void BadGetReferences()
        {
            Assert.ThrowsException<NullReferenceException>(() => {
                foreach (var value in Enumerables.BadGetReferences()) { }
            });
        }

        [TestMethod]
        public void GoodGetGenerics()
        {
            foreach (var value in Enumerables.GoodGetGenerics<object>()) { }
        }

        [TestMethod]
        public void BadGetGenerics()
        {
            Assert.ThrowsException<NullReferenceException>(() => {
                foreach (var value in Enumerables.BadGetGenerics<object>()) { }
            });
        }

        [TestMethod]
        public void GetNullNonGenerics()
        {
            foreach (var value in Enumerables.GetNullNonGenerics()) { }
        }
    }
}