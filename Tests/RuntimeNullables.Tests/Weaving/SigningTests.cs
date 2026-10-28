using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using RuntimeNullables.Tests.Infrastructure;
using RuntimeNullables.Weaver;
using TestAssemblySigned;

namespace RuntimeNullables.Tests;

[TestClass]
public class SigningTests
{
    private const string AssemblyName = "TestAssemblySigned";

    [TestMethod]
    public void SignedAssemblyIsWoven()
    {
        NullCheckAssert.ThrowsArgumentNull("value", () => Signed.Echo(null!));
        CollectionAssert.AreEqual(typeof(NullChecksAttribute).Assembly.GetName().GetPublicKey(), typeof(Signed).Assembly.GetName().GetPublicKey());
    }

    [TestMethod]
    public void SignedAssemblyHasValidSignatureAfterBuild()
    {
        AssertValidSignature(TestFiles.GetWovenAssemblyPath(AssemblyName));
    }

    [TestMethod]
    public void SignedAssemblyIsSignedAgainWithKeyFile()
    {
        using var directory = new TempDirectory();
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        AssertWeaves(TestFiles.GetUnwovenAssemblyPath(AssemblyName), outputPath, keyFile: TestFiles.KeyFilePath);
        AssertValidSignature(outputPath);
    }

    [TestMethod]
    public void SignedAssemblyIsSignedAgainWithAssemblyKeyFileAttribute()
    {
        using var directory = new TempDirectory();
        string inputPath = directory.GetFilePath("Input.dll");
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        // A relative [AssemblyKeyFile] path is resolved against the assembly's directory (no base directory is set). The key name doesn't exist
        // anywhere else, e.g. the current directory.

        Directory.CreateDirectory(directory.GetFilePath("Keys"));
        File.Copy(TestFiles.KeyFilePath, directory.GetFilePath(@"Keys\AttributeKey.snk"));

        TestFiles.WriteModifiedCopy(AssemblyName, inputPath, module => {
            var constructor = module.ImportReference(typeof(AssemblyKeyFileAttribute).GetConstructor([typeof(string)]));
            var attribute = new CustomAttribute(constructor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, @"Keys\AttributeKey.snk"));
            module.Assembly.CustomAttributes.Add(attribute);
        });

        AssertWeaves(inputPath, outputPath, keyFile: null);
        AssertValidSignature(outputPath);
    }

    [TestMethod]
    public void SignedAssemblyWithoutKeyFails()
    {
        using var directory = new TempDirectory();
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        AssertFails(TestFiles.GetUnwovenAssemblyPath(AssemblyName), outputPath, keyFile: null, "no key was found");
        Assert.IsFalse(File.Exists(outputPath));
    }

    [TestMethod]
    public void MissingKeyFileFails()
    {
        using var directory = new TempDirectory();
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        string keyFile = directory.GetFilePath("missing.snk");

        AssertFails(TestFiles.GetUnwovenAssemblyPath(AssemblyName), outputPath, keyFile, "Could not read the strong name key file");
        Assert.IsFalse(File.Exists(outputPath));
    }

    [TestMethod]
    public void MissingKeyContainerFails()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Key containers are only supported on Windows.");

        using var directory = new TempDirectory();
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(TestFiles.GetUnwovenAssemblyPath(AssemblyName), TestFiles.GetReferencePaths(AssemblyName))
        {
            OutputPath = outputPath,
            KeyContainer = "RuntimeNullables.Tests.MissingContainer",
        };

        Assert.IsFalse(AssemblyWeaver.Weave(options, logger));
        Assert.AreEqual(1, logger.Errors.Count, logger.ToString());
        Assert.IsTrue(logger.HasErrorContaining("Could not open the strong name key container"), logger.ToString());
        Assert.IsFalse(File.Exists(outputPath));
    }

    [TestMethod]
    public void KeyFileWithoutPrivateKeyFails()
    {
        using var directory = new TempDirectory();
        string keyFile = directory.GetFilePath("public.snk");
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        using (var key = new RSACryptoServiceProvider(1024))
            File.WriteAllBytes(keyFile, key.ExportCspBlob(includePrivateParameters: false));

        AssertFails(TestFiles.GetUnwovenAssemblyPath(AssemblyName), outputPath, keyFile, "does not contain a private key");
        Assert.IsFalse(File.Exists(outputPath));
    }

    [TestMethod]
    public void DifferentKeyFails()
    {
        using var directory = new TempDirectory();
        string keyFile = directory.GetFilePath("other.snk");
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        using (var key = new RSACryptoServiceProvider(1024))
            File.WriteAllBytes(keyFile, key.ExportCspBlob(includePrivateParameters: true));

        AssertFails(TestFiles.GetUnwovenAssemblyPath(AssemblyName), outputPath, keyFile, "doesn't match the public key");

        // The mismatch is only found while writing, so the output exists (with the wrong identity).
        Assert.IsTrue(File.Exists(outputPath));
    }

    [TestMethod]
    public void PublicSignedAssemblyIsNotSignedAgain()
    {
        using var directory = new TempDirectory();
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(TestFiles.GetUnwovenAssemblyPath(AssemblyName), TestFiles.GetReferencePaths(AssemblyName))
        {
            OutputPath = outputPath,
            PublicSign = true,
        };

        Assert.IsTrue(AssemblyWeaver.Weave(options, logger), logger.ToString());

        using var module = ModuleDefinition.ReadModule(outputPath);
        Assert.IsTrue(module.Attributes.HasFlag(ModuleAttributes.StrongNameSigned));
        CollectionAssert.AreEqual(typeof(Signed).Assembly.GetName().GetPublicKey(), module.Assembly.Name.PublicKey);
    }

    [TestMethod]
    public void DelaySignedAssemblyIsNotSigned()
    {
        using var directory = new TempDirectory();
        string inputPath = directory.GetFilePath("Input.dll");
        string outputPath = directory.GetFilePath(AssemblyName + ".dll");

        // A delay signed assembly has a public key but not the strong name signed flag.

        TestFiles.WriteModifiedCopy(AssemblyName, inputPath, module => module.Attributes &= ~ModuleAttributes.StrongNameSigned);

        AssertWeaves(inputPath, outputPath, keyFile: null);

        using var wovenModule = ModuleDefinition.ReadModule(outputPath);
        Assert.IsFalse(wovenModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned));
        CollectionAssert.AreEqual(typeof(Signed).Assembly.GetName().GetPublicKey(), wovenModule.Assembly.Name.PublicKey);
    }

    private static void AssertWeaves(string inputPath, string outputPath, string? keyFile)
    {
        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(inputPath, TestFiles.GetReferencePaths(AssemblyName)) { OutputPath = outputPath, KeyFile = keyFile };

        Assert.IsTrue(AssemblyWeaver.Weave(options, logger), logger.ToString());
    }

    private static void AssertFails(string inputPath, string outputPath, string? keyFile, string expectedError)
    {
        var logger = new TestWeaverLogger();
        var options = new AssemblyWeaverOptions(inputPath, TestFiles.GetReferencePaths(AssemblyName)) { OutputPath = outputPath, KeyFile = keyFile };

        Assert.IsFalse(AssemblyWeaver.Weave(options, logger));
        Assert.AreEqual(1, logger.Errors.Count, logger.ToString());
        Assert.IsTrue(logger.HasErrorContaining(expectedError), logger.ToString());
    }

    private static void AssertValidSignature(string assemblyPath)
    {
        if (!WindowsSdkTools.TryFind("sn", out string? snPath))
            Assert.Inconclusive("sn.exe was not found in the Windows SDK.");

        var startInfo = new ProcessStartInfo(snPath, $"-vf \"{assemblyPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)!;
        var errorOutput = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        Assert.AreEqual(0, process.ExitCode, $"Strong name signature verification failed:{Environment.NewLine}{output}{errorOutput.Result}");
    }
}
