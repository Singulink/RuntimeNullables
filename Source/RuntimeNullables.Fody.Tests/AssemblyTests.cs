using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RuntimeNullables.Fody.Tests
{
    [TestClass]
    public class AssemblyTests
    {
        [TestMethod]
        public void NoValueTupleReferences()
        {
            // System.ValueTuple may cause issues in some configurations, avoid using it.

            using var fileStream = File.OpenRead(typeof(ModuleWeaver).Assembly.Location);
            using var peReader = new PEReader(fileStream);
            var metadataReader = peReader.GetMetadataReader();

            foreach (var typeRefHandle in metadataReader.TypeReferences) {
                var typeRef = metadataReader.GetTypeReference(typeRefHandle);

                string typeNamespace = metadataReader.GetString(typeRef.Namespace);
                if (typeNamespace != typeof(ValueTuple).Namespace)
                    continue;

                string typeName = metadataReader.GetString(typeRef.Name);
                Assert.IsFalse(typeName.Contains(nameof(ValueTuple), StringComparison.Ordinal));
            }
        }
    }
}