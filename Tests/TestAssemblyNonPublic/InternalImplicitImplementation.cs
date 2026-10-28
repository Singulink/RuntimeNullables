using System;
using RuntimeNullables;

namespace TestAssemblyNonPublic;

/// <summary>
/// Implements the interfaces with public members instead of explicit implementations. Other assemblies can call the public interfaces' members.
/// </summary>
[NullChecks(true)]
internal class InternalImplicitImplementation : IPublicInterface, IInternalInterface, IPublicGenericInterface<int>, IPublicMembersInterface
{
    public event Action InterfaceEvent
    {
        add { }
        remove { }
    }

    public string InterfaceProperty
    {
        set { }
    }

    public void InterfacePublicMethod(string value) { }

    public void InterfaceInternalMethod(string value) { }

    public void InterfaceGenericMethod(int value, string text) { }
}

/// <summary>
/// The base class's method implements the interface for the derived class.
/// </summary>
[NullChecks(true)]
internal class InternalBaseImplementation
{
    public void InterfacePublicMethod(string value) { }
}

[NullChecks(true)]
internal class InternalInheritedImplementation : InternalBaseImplementation, IPublicInterface
{
}

#if NET7_0_OR_GREATER

[NullChecks(true)]
internal class InternalStaticImplementation : IPublicStaticInterface
{
    public static void InterfaceStaticMethod(string value) { }
}

#endif
