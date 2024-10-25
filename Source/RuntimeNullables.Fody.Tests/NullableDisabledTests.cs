using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class NullableDisabledTests
{
    [TestMethod]
    public void NonNull() => NullableDisabled.NonNull(null);
}