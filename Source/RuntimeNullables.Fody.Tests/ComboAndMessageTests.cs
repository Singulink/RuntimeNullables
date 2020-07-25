using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class ComboAndMessageTests
    {
        [TestMethod]
        public void GoodInputAndOutput()
        {
            var instance = new Combo<string>();
            object refValue = string.Empty;

            instance.GetValue<object>(false, string.Empty, ref refValue, false);
        }

        [TestMethod]
        public void BadInput()
        {
            var instance = new Combo<string>();

            object refValue = string.Empty;
            var ex = Assert.ThrowsException<ArgumentNullException>(() => instance.GetValue<object>(false, null!, ref refValue, false));
            Assert.AreEqual("inputValue", ex.ParamName);

            refValue = null;
            ex = Assert.ThrowsException<ArgumentNullException>(() => instance.GetValue<object>(false, string.Empty, ref refValue!, false));
            Assert.AreEqual("refValue", ex.ParamName);
        }

        [TestMethod]
        public void BadOutput()
        {
            var instance = new Combo<string>();
            object refValue = string.Empty;

            var ex = Assert.ThrowsException<NullReferenceException>(() => instance.GetValue<object>(true, string.Empty, ref refValue, false));
            Assert.AreEqual("Return value nullability contract was broken.", ex.Message);

            ex = Assert.ThrowsException<NullReferenceException>(() => instance.GetValue<object>(false, string.Empty, ref refValue, true));
            Assert.AreEqual("Output parameter 'refValue' nullability contract was broken.", ex.Message);
        }
    }
}
