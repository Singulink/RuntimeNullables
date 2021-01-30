using System;

namespace RuntimeNullables
{
    /// <summary>
    /// Indicates whether nullable reference type null checks should be injected.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.ReturnValue,
        Inherited = false)]
    public sealed class NullChecksAttribute : Attribute
    {
        /// <summary>
        /// Gets a value indicating whether nullable reference type null checks should be injected.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NullChecksAttribute"/> class.
        /// </summary>
        public NullChecksAttribute(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
