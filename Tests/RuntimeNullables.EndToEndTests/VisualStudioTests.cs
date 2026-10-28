using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.EndToEndTests.Infrastructure;

namespace RuntimeNullables.EndToEndTests;

[TestClass]
public class VisualStudioTests
{
    /// <summary>
    /// Builds with the MSBuild of each Visual Studio installation, which loads the weaving task on .NET Framework. Each build uses the newest .NET
    /// SDK that the installation supports, and the app targets .NET 8, which all of them can target, and runs on the newest .NET runtime.
    /// </summary>
    [TestMethod]
    public void BuildWithVisualStudioWeaves()
    {
        int buildCount = 0;

        foreach (var msbuild in VisualStudioMSBuild.FindAll())
        {
            if (msbuild.SdkVersion == null)
                continue;

            using var project = new TestProject($"{nameof(BuildWithVisualStudioWeaves)}-{msbuild.Version.Major}");
            project.WriteProject(targetFrameworks: "net8.0", otherProperties: "<RollForward>Major</RollForward>");
            project.WriteGlobalJson(msbuild.SdkVersion);

            var result = project.BuildWith(msbuild.Path);
            Assert.IsTrue(result.Succeeded, $"{msbuild}:\n{result.Output}");
            Assert.AreEqual(0, result.Diagnostics.Count, $"{msbuild}:\n{result.Output}");
            Assert.AreEqual(AppResult.Default, project.Run(), msbuild.ToString());

            var writeTime = project.IntermediateAssemblyWriteTime;
            result = project.BuildWith(msbuild.Path);
            Assert.IsTrue(result.Succeeded, $"{msbuild}:\n{result.Output}");
            Assert.AreEqual(writeTime, project.IntermediateAssemblyWriteTime, $"{msbuild}: A build without changes doesn't compile again.");

            Console.WriteLine($"Built with {msbuild}.");
            buildCount++;
        }

        if (buildCount == 0)
            Assert.Inconclusive("No Visual Studio installation with a .NET SDK that can target .NET 8 was found.");
    }
}
