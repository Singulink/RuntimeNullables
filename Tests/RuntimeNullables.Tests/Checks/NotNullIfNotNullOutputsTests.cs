using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

[TestClass]
public class NotNullIfNotNullOutputsTests
{
    [TestMethod]
    public void Echo()
    {
        Assert.IsNull(NotNullIfNotNullOutputs.Echo(null));
        Assert.AreEqual("value", NotNullIfNotNullOutputs.Echo("value"));
    }

    [TestMethod]
    public void BadEcho()
    {
        Assert.IsNull(NotNullIfNotNullOutputs.BadEcho(null));
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => NotNullIfNotNullOutputs.BadEcho("value"));
    }

    [TestMethod]
    public void BadEchoEither()
    {
        Assert.IsNull(NotNullIfNotNullOutputs.BadEchoEither(null, null));
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => NotNullIfNotNullOutputs.BadEchoEither("value", null));
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => NotNullIfNotNullOutputs.BadEchoEither(null, "value"));
    }

    [TestMethod]
    public void BadEchoThatAssigns()
    {
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => NotNullIfNotNullOutputs.BadEchoThatAssigns("value"));
    }

    [TestMethod]
    public void BadEchoMaybeNull()
    {
        Assert.IsNull(NotNullIfNotNullOutputs.BadEchoMaybeNull(null));
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => NotNullIfNotNullOutputs.BadEchoMaybeNull("value"));
    }

    [TestMethod]
    public void BadEchoGeneric()
    {
        Assert.IsNull(NotNullIfNotNullOutputs.BadEchoGeneric<string?>(null));
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => NotNullIfNotNullOutputs.BadEchoGeneric("value"));
    }

    [TestMethod]
    public void BadNumberToString()
    {
        Assert.IsNull(NotNullIfNotNullOutputs.BadNumberToString(null));
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => NotNullIfNotNullOutputs.BadNumberToString(1));
    }

    [TestMethod]
    public void BadEchoUnknownName() => Assert.IsNull(NotNullIfNotNullOutputs.BadEchoUnknownName("value"));

    [TestMethod]
    public void BadEchoRefArgument()
    {
        string? value = "value";
        Assert.IsNull(NotNullIfNotNullOutputs.BadEchoRefArgument(ref value));
    }

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
    [TestMethod]
    public async Task BadEchoAsync() => Assert.IsNull(await NotNullIfNotNullOutputs.BadEchoAsync("value"));
#pragma warning restore CA2007

    [TestMethod]
    public void BadEchoIterator() => Assert.IsNull(NotNullIfNotNullOutputs.BadEchoIterator("value").Single());

    [TestMethod]
    public void OutEcho()
    {
        NotNullIfNotNullOutputs.OutEcho(null, out string? output);
        Assert.IsNull(output);

        NotNullIfNotNullOutputs.OutEcho("value", out output);
        Assert.AreEqual("value", output);
    }

    [TestMethod]
    public void BadOutEcho()
    {
        NotNullIfNotNullOutputs.BadOutEcho(null, out _);
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.OutputParameter("output"), () => NotNullIfNotNullOutputs.BadOutEcho("value", out _));
    }

    [TestMethod]
    public void BadRefEcho()
    {
        string? output = "output";
        NotNullIfNotNullOutputs.BadRefEcho(null, ref output);

        output = "output";
        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.OutputParameter("output"), () => NotNullIfNotNullOutputs.BadRefEcho("value", ref output));
    }

    [TestMethod]
    public void ThrowingEcho() => Assert.ThrowsException<InvalidOperationException>(() => NotNullIfNotNullOutputs.ThrowingEcho("value"));

    [TestMethod]
    public void ThrowingOutEcho()
    {
        Assert.ThrowsException<InvalidOperationException>(() => NotNullIfNotNullOutputs.ThrowingOutEcho("value", out _));
    }
}
