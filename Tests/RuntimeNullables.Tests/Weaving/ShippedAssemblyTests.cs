using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Weaver;
using TestAssembly;

namespace RuntimeNullables.Tests;

/// <summary>
/// Tests the assemblies in the package.
/// </summary>
[TestClass]
public class ShippedAssemblyTests
{
    [TestMethod]
    public void VersionsMatch()
    {
        // The [GeneratedCode] attribute on the injected throw helpers has the weaver's version, which is the package's version.

        var throwHelperType = typeof(InputParameters).Assembly.GetType("RuntimeNullables.ThrowHelpers")!;
        string generatedCodeVersion = throwHelperType.GetCustomAttribute<GeneratedCodeAttribute>()!.Version!;

        Assert.AreEqual(typeof(ModuleWeaver).Assembly.GetName().Version!.ToString(), generatedCodeVersion);
        Assert.AreEqual(typeof(NullChecksAttribute).Assembly.GetName().Version!.ToString(), generatedCodeVersion);
    }

    [TestMethod]
    public void NoValueTupleReferences()
    {
        // System.ValueTuple may cause issues in some configurations, avoid using it.

        using var fileStream = File.OpenRead(typeof(ModuleWeaver).Assembly.Location);
        using var peReader = new PEReader(fileStream);
        var metadataReader = peReader.GetMetadataReader();

        foreach (var typeRefHandle in metadataReader.TypeReferences)
        {
            var typeRef = metadataReader.GetTypeReference(typeRefHandle);

            string typeNamespace = metadataReader.GetString(typeRef.Namespace);
            if (typeNamespace != typeof(ValueTuple).Namespace)
                continue;

            string typeName = metadataReader.GetString(typeRef.Name);
            Assert.IsFalse(typeName.Contains(nameof(ValueTuple), StringComparison.Ordinal));
        }
    }
}
