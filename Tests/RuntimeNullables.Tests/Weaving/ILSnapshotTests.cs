using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using VerifyMSTest;
using VerifyTests;

namespace RuntimeNullables.Tests;

/// <summary>
/// Compares the IL of every type in the woven test assemblies with its snapshot in the Snapshots folder. Debug and Release builds have separate
/// snapshots since the compiler's code differs; the target frameworks share them. To accept a change, replace the <c>.verified.txt</c> file with the
/// <c>.received.txt</c> file that the failing test writes next to it (one per target framework).
/// </summary>
[TestClass]
public class ILSnapshotTests : VerifyBase
{
    private const string SnapshotsDirectoryName = "Snapshots";

    [TestMethod]
    [DynamicData(nameof(TestAssemblies.AllTypes), typeof(TestAssemblies))]
    public Task TypeMatchesSnapshot(string assemblyName, string typeName)
    {
        var settings = new VerifySettings();
        settings.UseDirectory(Path.Combine(SnapshotsDirectoryName, assemblyName));
        settings.UseFileName(GetSnapshotName(assemblyName, typeName));
        settings.UniqueForAssemblyConfiguration();

        return Verify(Ildasm.Decompile(TestFiles.GetWovenAssemblyPath(assemblyName), typeName), settings);
    }

#if NET11_0_OR_GREATER
    /// <summary>
    /// Makes sure every snapshot belongs to a type, so snapshots of renamed or removed types don't stay behind, and that no two types share one. Only
    /// net11.0 has every test assembly and type (some types need .NET 7 or later, and TestAssemblyRuntimeAsync needs .NET 11).
    /// </summary>
    [TestMethod]
    public void EverySnapshotHasOneType()
    {
        string snapshotsDirectory = Path.Combine(GetSourceDirectory(), SnapshotsDirectoryName);

        var expectedFiles = TestAssemblies.AllTypes
            .Select(data => (AssemblyName: (string)data[0], TypeName: (string)data[1]))
            .SelectMany(t => new[] { "Debug", "Release" }.Select(c =>
                Path.Combine(t.AssemblyName, $"{GetSnapshotName(t.AssemblyName, t.TypeName)}.{c}.verified.txt")))
            .ToList();

        var actualFiles = Directory.GetFiles(snapshotsDirectory, "*.verified.txt", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(snapshotsDirectory, f))
            .ToList();

        CollectionAssert.AreEquivalent(expectedFiles, actualFiles);
    }

    private static string GetSourceDirectory([CallerFilePath] string sourceFilePath = "") => Path.GetDirectoryName(sourceFilePath)!;
#endif

    /// <summary>
    /// Gets the type's name without the namespace if it's the assembly's root namespace, and without the generic parameter count.
    /// </summary>
    private static string GetSnapshotName(string assemblyName, string typeName)
    {
        if (typeName.StartsWith(assemblyName + ".", StringComparison.Ordinal))
            typeName = typeName[(assemblyName.Length + 1)..];

        int genericMarkerIndex = typeName.IndexOf('`', StringComparison.Ordinal);
        return genericMarkerIndex < 0 ? typeName : typeName[..genericMarkerIndex];
    }
}
