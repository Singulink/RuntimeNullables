using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssemblySigned;

namespace RuntimeNullables.Tests;

/// <summary>
/// TestAssemblySigned uses the default settings: members outside the public API and outputs are only checked in Debug builds.
/// </summary>
[TestClass]
public class DefaultSettingsTests
{
    [TestMethod]
    public void PublicApiIsChecked() => NullCheckAssert.ThrowsArgumentNull("value", () => Signed.Echo(null!));

#if DEBUG
    [TestMethod]
    public void InternalMethodIsChecked() => NullCheckAssert.ThrowsArgumentNull("value", () => Signed.CallInternalMethod(null));

    [TestMethod]
    public void OutputIsChecked() => NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => Signed.BadReturn());
#else
    [TestMethod]
    public void InternalMethodIsNotChecked() => Signed.CallInternalMethod(null);

    [TestMethod]
    public void OutputIsNotChecked() => Assert.IsNull(Signed.BadReturn());
#endif
}
