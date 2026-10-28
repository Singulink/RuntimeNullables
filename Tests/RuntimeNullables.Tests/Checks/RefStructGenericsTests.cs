#if NET9_0_OR_GREATER

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssembly;

namespace RuntimeNullables.Tests;

/// <summary>
/// The ref struct cases would throw <see cref="InvalidProgramException"/> if the runtime didn't accept how the checks box the values.
/// </summary>
[TestClass]
public class RefStructGenericsTests
{
    [TestMethod]
    public void Echo()
    {
        Span<int> span = stackalloc int[1];
        _ = RefStructGenerics.Echo(span);

        NullCheckAssert.ThrowsArgumentNull("value", () => RefStructGenerics.Echo<string>(null!));
    }

    [TestMethod]
    public void BadReturn()
    {
        _ = RefStructGenerics.BadReturn<Span<int>>();

        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.ReturnValue, () => RefStructGenerics.BadReturn<string>());
    }

    [TestMethod]
    public void BadOut()
    {
        RefStructGenerics.BadOut(out Span<int> _);

        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.OutputParameter("value"), () => RefStructGenerics.BadOut(out string _));
    }

    [TestMethod]
    public void BadNotNullArgument()
    {
        RefStructGenerics.BadNotNullArgument(default(Span<int>));

        NullCheckAssert.ThrowsOutputNull(NullCheckMessages.Parameter("value"), () => RefStructGenerics.BadNotNullArgument<string>(null));
    }
}

#endif
