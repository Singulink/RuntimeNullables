using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssemblyNonPublic;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class NonPublicTests
    {
        [TestMethod]
        public void NoChecksOnAbstractInternalOnly()
        {
            var instance = new InternalSubClass();
            instance.AbstractInternalMethod(null!);
        }

        [TestMethod]
        public void NoChecksOnVirtualInternalOnly()
        {
            var instance = new InternalSubClass();
            instance.VirtualInternalMethod(null!);
        }

        [TestMethod]
        public void NoChecksOnInterfaceInternalOnly()
        {
            var instance = (IInternalInterface)new InternalSubClass();
            instance.InterfaceInternalMethod(null!);
        }

        [TestMethod]
        public void ChecksOnOverrideOfAbstractPublicMethod()
        {
            var instance = new InternalSubClass();
            Assert.ThrowsException<ArgumentNullException>(() => instance.AbstractPublicMethod(null!));
        }

        [TestMethod]
        public void ChecksOnOverrideOfVirtualPublicMethod()
        {
            var instance = new InternalSubClass();
            Assert.ThrowsException<ArgumentNullException>(() => instance.VirtualPublicMethod(null!));
        }

        [TestMethod]
        public void ChecksOnOverrideOfInterfacePublicMethod()
        {
            var instance = (IPublicInterface)new InternalSubClass();
            Assert.ThrowsException<ArgumentNullException>(() => instance.InterfacePublicMethod(null!));
        }
    }
}