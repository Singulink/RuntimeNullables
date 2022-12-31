using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssemblyNonPublic;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class CheckOutputsFalseTests
    {
        [TestMethod]
        public void CheckOutputsFalse()
        {
            PublicBaseClass.UncheckedBadReferenceReturn();
        }
    }
}