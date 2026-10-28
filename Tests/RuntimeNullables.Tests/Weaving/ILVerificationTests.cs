using System;
using System.Collections.Generic;
using System.Linq;
using ILVerify;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using RuntimeNullables.Weaver;

namespace RuntimeNullables.Tests;

[TestClass]
public class ILVerificationTests
{
    // The weaver adds locals without setting .locals init (so it doesn't override [SkipLocalsInit]) and always assigns them before use. ILVerify
    // reports that as unverifiable, which is expected.
    private static readonly VerifierError[] _ignoredErrors = [VerifierError.InitLocals];

    [TestMethod]
    [DataRow("TestAssembly", true)]
    [DataRow("TestAssemblyNonPublic", true)]
    [DataRow("TestAssemblyPointers", false)]
#if NET11_0_OR_GREATER
    [DataRow("TestAssemblyRuntimeAsync", true)]
#endif
    [DataRow("TestAssemblySigned", true)]
    [DataRow("TestAssemblyThrowHelpers", true)]
    public void WeavingAddsNoVerificationErrors(string assemblyName, bool hasChecks)
    {
        string unwovenPath = TestFiles.GetUnwovenAssemblyPath(assemblyName);
        string wovenPath = TestFiles.GetWovenAssemblyPath(assemblyName);

        // Make sure the comparison is between an unwoven and a woven assembly.

        Assert.AreEqual(0, TestFiles.CountThrowHelperCalls(unwovenPath), "The unwoven assembly contains null checks.");
        Assert.AreEqual(hasChecks, TestFiles.CountThrowHelperCalls(wovenPath) > 0, "Unexpected null checks in the woven assembly.");

        AssertSameErrors(ILVerification.Verify(unwovenPath, _ignoredErrors), ILVerification.Verify(wovenPath, _ignoredErrors));
    }

    // Weaving an assembly that is already woven doubles up the null checks, which helps ensure the injected IL handles the stack sensibly around
    // other injected checks.

    [TestMethod]
    [DynamicData(nameof(TestAssemblies.All), typeof(TestAssemblies))]
    public void WeavingTwiceAddsNoVerificationErrors(string assemblyName)
    {
        using var outputDirectory = new TempDirectory();
        string wovenPath = TestFiles.GetWovenAssemblyPath(assemblyName);
        string rewovenPath = outputDirectory.GetFilePath(assemblyName + ".dll");

        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(wovenPath, TestFiles.GetReferencePaths(assemblyName))
        {
            OutputPath = rewovenPath,
            DefineConstants = ["DEBUG"],
            KeyFile = TestFiles.KeyFilePath,
        };

        Assert.IsTrue(AssemblyWeaver.Weave(options, logger), logger.ToString());
        AssertSameErrors(ILVerification.Verify(wovenPath, _ignoredErrors), ILVerification.Verify(rewovenPath, _ignoredErrors));
    }

    private static void AssertSameErrors(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        if (expected.SequenceEqual(actual))
            return;

        var added = actual.Except(expected).ToList();
        var removed = expected.Except(actual).ToList();

        Assert.Fail(
            $"Verification errors differ.{Environment.NewLine}" +
            $"Added:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", added)}{Environment.NewLine}" +
            $"Removed:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", removed)}{Environment.NewLine}" +
            $"Expected count: {expected.Count}, actual count: {actual.Count}");
    }
}
