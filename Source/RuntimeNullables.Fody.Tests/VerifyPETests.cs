using System.Collections.Generic;
using Fody;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.Fody.Tests
{
    // TODO: Figure out why this fails in CI, ignore for now.
#if !CI
    [TestClass]
#endif
    public class VerifyPETests
    {
        // This will effectively "double up" the null checks since the assembly has already been processed.
        // IL should still verify afterwards - helps ensure we are doing sensible stack management in our injected IL.

        [TestMethod]
        public void VerifyTestAssembly()
        {
            var weaver = new ModuleWeaver() { DefineConstants = new List<string> { "DEBUG" } };
            weaver.ExecuteTestRun("TestAssembly.dll", ignoreCodes: new[] { "0x80131205", "0x80131884" });
        }

        [TestMethod]
        public void VerifyTestAssemblyPointers()
        {
            var weaver = new ModuleWeaver() { DefineConstants = new List<string> { "DEBUG" } };
            weaver.ExecuteTestRun("TestAssemblyNonPublic.dll", ignoreCodes: new[] { "0x80131205", "0x801318DE" });
        }

        [TestMethod]
        public void VerifyTestAssemblyNonPublic()
        {
            var weaver = new ModuleWeaver() { DefineConstants = new List<string> { "DEBUG" } };
            weaver.ExecuteTestRun("TestAssemblyNonPublic.dll", ignoreCodes: new[] { "0x80131205" });
        }

        [TestMethod]
        public void VerifyTestAssemblyWithThrowHelpers()
        {
            var weaver = new ModuleWeaver() { DefineConstants = new List<string> { "DEBUG" } };
            weaver.ExecuteTestRun("TestAssemblyThrowHelpers.dll", ignoreCodes: new[] { "0x80131205" });
        }
    }
}