using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssemblyPointers;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class PointerTests
    {
        private readonly int _value = 5;

        [TestMethod]
        public unsafe void ReturnSameValueNullableParameter()
        {
            fixed (int* value = &_value) {
                Pointers.ReturnSameValueNullableParameter(value);
                var ex = Assert.ThrowsException<NullReferenceException>(() => Pointers.ReturnSameValueNullableParameter(null));
                Assert.IsTrue(ex.Message.Contains("return", StringComparison.OrdinalIgnoreCase), "Exception message did not apply to return value.");
            }
        }

        [TestMethod]
        public unsafe void AllowNullOnlyNoOp()
        {
            fixed (int* value = &_value) {
                Pointers.AllowNullOnlyNoOp(value);
                var ex = Assert.ThrowsException<NullReferenceException>(() => Pointers.AllowNullOnlyNoOp(null));
                Assert.IsTrue(ex.Message.Contains("output", StringComparison.OrdinalIgnoreCase), "Exception message did not apply to output value.");
            }
        }

        [TestMethod]
        public unsafe void NonNullParameter()
        {
            fixed (int* value = &_value) {
                Pointers.NonNullParameter(value);
                Assert.ThrowsException<ArgumentNullException>(() => Pointers.NonNullParameter(null));
            }
        }
    }
}
