using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class EnumeratorTests
    {
        [TestMethod]
        public void GoodGetReferences()
        {
            var enumerator = Enumerators.GoodGetReferences();
            while (enumerator.MoveNext()) { }
        }

        [TestMethod]
        public void BadGetReferences()
        {
            Assert.ThrowsException<NullReferenceException>(() => {
                var enumerator = Enumerators.BadGetReferences();
                while (enumerator.MoveNext()) { }
            });
        }

        [TestMethod]
        public void GoodGetGenerics()
        {
            var enumerator = Enumerators.GoodGetGenerics<object>();
            while (enumerator.MoveNext()) { }
        }

        [TestMethod]
        public void BadGetGenerics()
        {
            Assert.ThrowsException<NullReferenceException>(() => {
                var enumerator = Enumerators.BadGetGenerics<object>();
                while (enumerator.MoveNext()) { }
            });
        }

        [TestMethod]
        public void GetNullNonGenerics()
        {
            var enumerator = Enumerators.GetNullNonGenerics();
            while (enumerator.MoveNext()) { }
        }
    }
}