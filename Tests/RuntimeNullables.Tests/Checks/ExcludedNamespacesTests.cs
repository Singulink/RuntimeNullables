using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.Tests;

/// <summary>
/// The weaver skips types in the namespaces where projects put copies of the attributes it reads (and in <c>RuntimeNullables</c>, where their own
/// throw helpers go, see <see cref="UsesThrowHelpersTests"/>).
/// </summary>
[TestClass]
public class ExcludedNamespacesTests
{
    [TestMethod]
    public void CodeAnalysisNamespaceTypeMethod() => CodeAnalysisNamespaceType.Method(null!);

    [TestMethod]
    public void CompilerServicesNamespaceTypeMethod() => CompilerServicesNamespaceType.Method(null!);
}
