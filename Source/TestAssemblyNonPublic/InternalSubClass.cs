using RuntimeNullables;

namespace TestAssemblyNonPublic
{
    [NullChecks(true)]
    internal class InternalSubClass : PublicBaseClass, IPublicInterface, IInternalInterface
    {
        public override void AbstractPublicMethod(string value) { }

        public override void VirtualPublicMethod(string value) { }

        internal override void AbstractInternalMethod(string value) { }

        internal override void VirtualInternalMethod(string value) { }

        void IPublicInterface.InterfacePublicMethod(string value) { }

        void IInternalInterface.InterfaceInternalMethod(string value) { }
    }
}
