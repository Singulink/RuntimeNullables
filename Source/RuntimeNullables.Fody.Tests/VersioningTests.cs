using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAssembly;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class VersioningTests
    {
        [TestMethod]
        public void GeneratedCodeToolVersion()
        {
            // Ensure that the GeneratedCodeAttribute version placed on the throw helper class is correct.

            var throwHelperType = typeof(InputParameters).Assembly.GetType("RuntimeNullables.ThrowHelpers");
            string generatedCodeVersionString = throwHelperType!.GetCustomAttribute<GeneratedCodeAttribute>()!.Version;

            var weaverVersion = new AssemblyName(typeof(ModuleWeaver).Assembly.FullName!).Version!;
            string weaverVersionString = $"{weaverVersion.Major}.{weaverVersion.Minor}.{weaverVersion.Build}";

            var addinVersion = new AssemblyName(typeof(NullChecksAttribute).Assembly.FullName!).Version!;
            string addinVersionString = $"{addinVersion.Major}.{addinVersion.Minor}.{addinVersion.Build}";

            Assert.AreEqual(weaverVersionString, generatedCodeVersionString);
            Assert.AreEqual(addinVersionString, generatedCodeVersionString);
        }
    }
}
