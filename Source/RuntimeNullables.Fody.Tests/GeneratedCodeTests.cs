using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class GeneratedCodeTests
    {
        [TestMethod]
        public void NoChecksOnGeneratedCode()
        {
            GeneratedCode.Method(null!);
        }
    }
}