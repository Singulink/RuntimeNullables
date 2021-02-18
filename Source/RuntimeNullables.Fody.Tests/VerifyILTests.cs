using System;
using System.Threading.Tasks;
using Fody;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;
using TestAssemblyNonPublic;
using TestAssemblyPointers;
using TestAssemblyThrowHelpers;
using VerifyMSTest;
using VerifyTests;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class VerifyILTests : VerifyBase
    {
        private readonly VerifySettings _verifySettings;

        public VerifyILTests()
        {
            _verifySettings = new VerifySettings();
            _verifySettings.UniqueForRuntimeAndVersion();
            _verifySettings.UniqueForAssemblyConfiguration();

            // _verifySettings.AutoVerify();
        }

        // TestAssembly:

        [TestMethod]
        public Task InjectedThrowHelpers()
        {
            return Verify(Decompile(typeof(Combo<>).Assembly.GetType("RuntimeNullables.ThrowHelpers")!), _verifySettings);
        }

        [TestMethod]
        public Task AsyncEnumerables()
        {
            return Verify(Decompile(typeof(AsyncEnumerables)), _verifySettings);
        }

        [TestMethod]
        public Task AsyncEnumerators()
        {
            return Verify(Decompile(typeof(AsyncEnumerators)), _verifySettings);
        }

        [TestMethod]
        public Task Combo()
        {
            return Verify(Decompile(typeof(Combo<>)), _verifySettings);
        }

        [TestMethod]
        public Task InputParameters()
        {
            return Verify(Decompile(typeof(InputParameters)), _verifySettings);
        }

        [TestMethod]
        public Task NestedContainer()
        {
            return Verify(Decompile(typeof(NestedContainer)), _verifySettings);
        }

        [TestMethod]
        public Task OutputParameters()
        {
            return Verify(Decompile(typeof(OutputParameters)), _verifySettings);
        }

        [TestMethod]
        public Task Properties()
        {
            return Verify(Decompile(typeof(Properties)), _verifySettings);
        }

        [TestMethod]
        public Task Returns()
        {
            return Verify(Decompile(typeof(Returns)), _verifySettings);
        }

        [TestMethod]
        public Task TaskResults()
        {
            return Verify(Decompile(typeof(TaskResults)), _verifySettings);
        }

        // TestAssemblyNonPublic:

        [TestMethod]
        public Task InternalSubClass()
        {
            return Verify(Decompile(typeof(InternalSubClass)), _verifySettings);
        }

        [TestMethod]
        public Task PublicBaseClass()
        {
            return Verify(Decompile(typeof(PublicBaseClass)), _verifySettings);
        }

        [TestMethod]
        public Task PublicContainingNestedNonPublic()
        {
            return Verify(Decompile(typeof(PublicContainingNestedNonPublic)), _verifySettings);
        }

        // TestPointers:

        [TestMethod]
        public Task Pointers()
        {
            return Verify(Decompile(typeof(Pointers)), _verifySettings);
        }

        // TestAssemblyThrowHelpers:

        [TestMethod]
        public Task UsesThrowHelpers()
        {
            return Verify(Decompile(typeof(UsesThrowHelpers)), _verifySettings);
        }

        private static string Decompile(Type type) => Ildasm.Decompile(type.Assembly.Location, type.FullName);
    }
}
