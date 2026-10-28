using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil.Cil;
using RuntimeNullables.Tests.Infrastructure;
using RuntimeNullables.Weaver;

namespace RuntimeNullables.Tests;

[TestClass]
public class ReferenceCleanupTests
{
    // Both assemblies use [NullChecks], including (in TestAssembly) on items the weaver skips and through a using alias.

    [TestMethod]
    [DataRow("TestAssembly")]
    [DataRow("TestAssemblyNonPublic")]
    public void UnwovenAssemblyReferencesRuntimeNullables(string assemblyName)
    {
        // Makes sure the test below is meaningful.

        var (assemblyReferences, typeReferences) = GetReferences(TestFiles.GetUnwovenAssemblyPath(assemblyName));

        CollectionAssert.Contains(assemblyReferences, "RuntimeNullables");
        CollectionAssert.Contains(typeReferences, "NullChecksAttribute");
    }

    [TestMethod]
    [DataRow("TestAssembly")]
    [DataRow("TestAssemblyNonPublic")]
    public void WovenAssemblyDoesNotReferenceRuntimeNullables(string assemblyName)
    {
        var (assemblyReferences, typeReferences) = GetReferences(TestFiles.GetWovenAssemblyPath(assemblyName));

        CollectionAssert.DoesNotContain(assemblyReferences, "RuntimeNullables");
        CollectionAssert.DoesNotContain(typeReferences, "NullChecksAttribute");
    }

    [TestMethod]
    [DataRow("TestAssembly")]
    [DataRow("TestAssemblyNonPublic")]
    [DataRow("TestAssemblyPointers")]
    [DataRow("TestAssemblySigned")]
    [DataRow("TestAssemblyThrowHelpers")]
    public void WeavingAddsNoAssemblyReferences(string assemblyName)
    {
        // BCL types the weaver uses must be imported from assemblies the compiler already references.

        var (unwovenReferences, _) = GetReferences(TestFiles.GetUnwovenAssemblyPath(assemblyName));
        var (wovenReferences, _) = GetReferences(TestFiles.GetWovenAssemblyPath(assemblyName));

        CollectionAssert.AreEquivalent(unwovenReferences.Where(r => r != "RuntimeNullables").ToList(), wovenReferences);
    }

    [TestMethod]
    public void UsingRuntimeNullablesTypesOutsideAttributesFails()
    {
        using var directory = new TempDirectory();
        string inputPath = directory.GetFilePath("TestAssembly.dll");

        // Add "ldtoken [RuntimeNullables]NullChecksAttribute; pop" (i.e. typeof(NullChecksAttribute)) to a method.

        TestFiles.WriteModifiedCopy("TestAssembly", inputPath, module => {
            var nullChecksAttributeType = module.GetTypeReferences().Single(t => t.FullName == "RuntimeNullables.NullChecksAttribute");
            var method = module.GetType("TestAssembly.InputParameters").Methods.First(m => m.HasBody);
            var il = method.Body.GetILProcessor();
            var firstInstruction = method.Body.Instructions[0];

            il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldtoken, nullChecksAttributeType));
            il.InsertBefore(firstInstruction, il.Create(OpCodes.Pop));
        });

        byte[] input = File.ReadAllBytes(inputPath);
        var logger = new TestWeaverLogger();

        bool result = AssemblyWeaver.Weave(new AssemblyWeaverOptions(inputPath, TestFiles.GetReferencePaths("TestAssembly")), logger);

        Assert.IsFalse(result);
        Assert.IsTrue(logger.HasErrorContaining("only available at compile time"), logger.ToString());
        Assert.IsTrue(logger.HasErrorContaining("'System.Void TestAssembly.InputParameters::"), logger.ToString());
        CollectionAssert.AreEqual(input, File.ReadAllBytes(inputPath), "The assembly should not be written when weaving fails.");
    }

    private static (List<string> AssemblyReferences, List<string> TypeReferences) GetReferences(string assemblyPath)
    {
        using var peReader = new PEReader(File.OpenRead(assemblyPath));
        var metadata = peReader.GetMetadataReader();

        var assemblyReferences = metadata.AssemblyReferences.Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name)).ToList();
        var typeReferences = metadata.TypeReferences.Select(h => metadata.GetString(metadata.GetTypeReference(h).Name)).ToList();

        return (assemblyReferences, typeReferences);
    }
}
