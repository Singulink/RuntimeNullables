using System;

namespace TestAssemblyNonPublic;

public interface IPublicMembersInterface
{
    event Action InterfaceEvent;

    string InterfaceProperty { set; }
}
