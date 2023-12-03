using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests;

[TestClass]
public class VersioningTests
{
    [TestMethod]
    public void GeneratedCodeToolVersion()
    {
        // Ensure that the GeneratedCodeAttribute version placed on the throw helper class is correct.

        var throwHelperType = typeof(InputParameters).Assembly.GetType("RuntimeNullables.ThrowHelpers");
        string generatedCodeVersion = throwHelperType!.GetCustomAttribute<GeneratedCodeAttribute>()!.Version;

        string weaverVersion = new AssemblyName(typeof(ModuleWeaver).Assembly.FullName!).Version!.ToString();
        string addinVersion = new AssemblyName(typeof(NullChecksAttribute).Assembly.FullName!).Version!.ToString();

        Assert.AreEqual(weaverVersion, generatedCodeVersion);
        Assert.AreEqual(addinVersion, generatedCodeVersion);
    }
}