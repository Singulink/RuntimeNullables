using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssemblyThrowHelpers;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class ThrowHelpersTests
    {
        [TestMethod]
        public void ArgumentNull()
        {
            Assert.ThrowsException<InvalidOperationException>(() => UsesThrowHelpers.ReturnParameter(null!));
        }

        [TestMethod]
        public void OutputNull()
        {
            Assert.ThrowsException<InvalidDataException>(() => UsesThrowHelpers.ReturnNullableParameterNonNullReturn(null));
        }

        [TestMethod]
        public Task OutputNullAsync()
        {
            return Assert.ThrowsExceptionAsync<InvalidConstraintException>(() => UsesThrowHelpers.ReturnAllowNullParameterNonNullReturnAsync(null));
        }
    }
}
