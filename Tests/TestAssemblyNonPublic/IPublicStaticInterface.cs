#if NET7_0_OR_GREATER

namespace TestAssemblyNonPublic;

public interface IPublicStaticInterface
{
    static abstract void InterfaceStaticMethod(string value);
}

#endif
