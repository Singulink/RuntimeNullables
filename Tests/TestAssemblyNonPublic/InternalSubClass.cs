using RuntimeNullables;

namespace TestAssemblyNonPublic;

[NullChecks(true)]
internal class InternalSubClass : PublicBaseClass, IPublicInterface, IInternalInterface
{
    public InternalSubClass() { }

    public InternalSubClass(string value) : base(value) { }

    public override void AbstractPublicMethod(string value) { }

    public override void VirtualPublicMethod(string value) { }

    internal override void AbstractInternalMethod(string value) { }

    internal override void VirtualInternalMethod(string value) { }

    void IPublicInterface.InterfacePublicMethod(string value) { }

    void IInternalInterface.InterfaceInternalMethod(string value) { }

    // The tests call the protected members of the base class through these (they aren't checked themselves since the class is internal).

    public void CallProtectedMethod(string value) => ProtectedMethod(value);

    public void CallProtectedInternalMethod(string value) => ProtectedInternalMethod(value);

    public void CallPrivateProtectedMethod(string value) => PrivateProtectedMethod(value);

    public static void CallProtectedNestedMethod(string value) => ProtectedNested.Method(value);

    public static void CallPrivateProtectedNestedMethod(string value) => PrivateProtectedNested.Method(value);
}
