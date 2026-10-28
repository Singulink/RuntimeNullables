using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

/// <summary>
/// Input and output checks on one method, with type and method generic parameters.
/// </summary>
[TestClass]
public class ComboTests
{
    private readonly Combo<string> _instance = new();

    [TestMethod]
    public void GoodInputAndOutput()
    {
        object refValue = string.Empty;
        _instance.GetValue(false, string.Empty, ref refValue, false);
    }

    [TestMethod]
    public void BadInput()
    {
        object refValue = string.Empty;
        NullCheckAssert.ThrowsArgumentNull("inputValue", () => _instance.GetValue(false, null!, ref refValue, false));

        refValue = null!;
        NullCheckAssert.ThrowsArgumentNull("refValue", () => _instance.GetValue(false, string.Empty, ref refValue, false));
    }

    [TestMethod]
    public void BadOutput()
    {
        object refValue = string.Empty;
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => _instance.GetValue(true, string.Empty, ref refValue, false));
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.OutputParameter("refValue"), () => _instance.GetValue(false, string.Empty, ref refValue, true));
    }
}
