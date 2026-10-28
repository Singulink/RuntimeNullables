using System.CodeDom.Compiler;
using RuntimeNullables;
using NullChecksAlias = RuntimeNullables.NullChecksAttribute;

namespace TestAssembly;

/// <summary>
/// The weaver doesn't add checks to these items but still has to remove their [NullChecks] attributes. The alias also leaves a reference to the
/// attribute type in the PDB's import scopes.
/// </summary>
public static class NullChecksOnSkippedItems
{
    public interface IInterface
    {
        [NullChecks(false)]
        [return: NullChecks(false)]
        string Method(string value);
    }

    public abstract class AbstractMembers
    {
        [NullChecksAlias(false)]
        public abstract void Method(string value);

        [NullChecks(false)]
        public abstract string Property { get; }
    }

    [GeneratedCode("RuntimeNullables.Tests", "1.0")]
    [NullChecks(false)]
    public class GeneratedCode
    {
        [NullChecks(false)]
        public static void Method(string value) { }
    }
}
