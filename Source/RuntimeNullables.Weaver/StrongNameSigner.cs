using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil;

namespace RuntimeNullables.Weaver;

/// <summary>
/// Signs woven assemblies again with the strong name key the compiler signed them with.
/// </summary>
internal static class StrongNameSigner
{
    /// <summary>
    /// Sets the signing key on the writer parameters if the compiler signed the assembly with a private key.
    /// </summary>
    /// <returns><see langword="true"/> if successful, otherwise <see langword="false"/> (an error is logged).</returns>
    public static bool TryConfigure(ModuleDefinition module, AssemblyWeaverOptions options, WriterParameters parameters, IWeaverLogger logger)
    {
        // Delay signed assemblies don't have the strong name signed flag set, and public signed assemblies have it set without a valid signature. Neither
        // is signed again, and both keep the public key the compiler wrote.

        if (module.Assembly == null || (module.Attributes & ModuleAttributes.StrongNameSigned) == 0 || options.PublicSign)
            return true;

        // Same key sources and precedence as the compiler: a key file before a key container, each from the options before the (obsolete) assembly
        // attribute.

        var assemblyAttributes = module.Assembly.CustomAttributes;
        string? keyFile = NullIfEmpty(options.KeyFile) ?? GetStringArgument(assemblyAttributes, "System.Reflection.AssemblyKeyFileAttribute");
        string? keyContainer = NullIfEmpty(options.KeyContainer) ?? GetStringArgument(assemblyAttributes, "System.Reflection.AssemblyKeyNameAttribute");

        if (keyFile != null)
            return TrySetKeyFile(ResolveKeyFile(keyFile, options), parameters, logger);

        if (keyContainer != null)
            return TrySetKeyContainer(keyContainer, parameters, logger);

        logger.LogError("The assembly is strong name signed, but no key was found to sign it again after weaving.");
        return false;
    }

    /// <summary>
    /// Checks that signing the woven assembly didn't change its public key, which happens when the key isn't the one the compiler used.
    /// </summary>
    /// <returns><see langword="true"/> if the public key is unchanged, otherwise <see langword="false"/> (an error is logged).</returns>
    public static bool CheckPublicKeyUnchanged(byte[] originalPublicKey, ModuleDefinition module, IWeaverLogger logger)
    {
        if ((module.Assembly?.Name.PublicKey ?? []).SequenceEqual(originalPublicKey))
            return true;

        logger.LogError(
            "The woven assembly was signed with a strong name key that doesn't match the public key it was compiled with, so its identity changed. " +
            "Make sure the key file or container is the one the assembly is compiled with.");

        return false;
    }

    /// <summary>
    /// Resolves a relative key file path like the compiler does: against the base directory first, then the assembly's directory.
    /// </summary>
    private static string ResolveKeyFile(string keyFile, AssemblyWeaverOptions options)
    {
        if (Path.IsPathRooted(keyFile))
            return keyFile;

        string assemblyDirectory = Path.GetDirectoryName(Path.GetFullPath(options.AssemblyPath))!;
        string? baseDirectory = NullIfEmpty(options.BaseDirectory);
        string[] searchDirectories = baseDirectory == null ? [assemblyDirectory] : [baseDirectory, assemblyDirectory];
        var candidates = searchDirectories.Select(d => Path.GetFullPath(Path.Combine(d, keyFile))).ToList();

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static bool TrySetKeyFile(string keyFile, WriterParameters parameters, IWeaverLogger logger)
    {
        byte[] key;

        try
        {
            key = File.ReadAllBytes(keyFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError($"Could not read the strong name key file '{keyFile}': {ex.Message}");
            return false;
        }

        if (!IsPrivateKeyBlob(key))
        {
            logger.LogError($"The strong name key file '{keyFile}' does not contain a private key, so the woven assembly cannot be signed.");
            return false;
        }

        parameters.StrongNameKeyBlob = key;
        return true;
    }

    private static bool TrySetKeyContainer(string keyContainer, WriterParameters parameters, IWeaverLogger logger)
    {
        // Cecil opens the container in the machine key store (which is also where MSBuild imports .pfx keys) and creates a new key if it doesn't exist,
        // so make sure it exists first.

        try
        {
            var cspParameters = new CspParameters
            {
                KeyContainerName = keyContainer,
                KeyNumber = (int)KeyNumber.Signature,
                Flags = CspProviderFlags.UseMachineKeyStore | CspProviderFlags.UseExistingKey,
            };

            using var key = new RSACryptoServiceProvider(cspParameters);
        }
        catch (Exception ex) when (ex is CryptographicException or PlatformNotSupportedException)
        {
            logger.LogError($"Could not open the strong name key container '{keyContainer}' in the machine key store: {ex.Message}");
            return false;
        }

        parameters.StrongNameKeyContainer = keyContainer;
        return true;
    }

    /// <summary>
    /// Determines whether a key blob is a CAPI RSA private key (a <c>PRIVATEKEYBLOB</c> with the <c>RSA2</c> magic number), as produced by <c>sn -k</c>.
    /// </summary>
    private static bool IsPrivateKeyBlob(byte[] blob)
    {
        const byte PrivateKeyBlobType = 0x07;
        const byte BlobVersion = 0x02;
        const uint Rsa2Magic = 0x32415352; // "RSA2"

        return blob.Length >= 12 && blob[0] == PrivateKeyBlobType && blob[1] == BlobVersion && blob[2] == 0 && blob[3] == 0 &&
            BitConverter.ToUInt32(blob, 8) == Rsa2Magic;
    }

    private static string? GetStringArgument(IEnumerable<CustomAttribute> attributes, string attributeFullName)
    {
        var attribute = attributes.FirstOrDefault(a => a.AttributeType.FullName == attributeFullName);
        return NullIfEmpty(attribute?.ConstructorArguments.FirstOrDefault().Value as string);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
