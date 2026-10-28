using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuntimeNullables.Tests.Infrastructure;
using TestAssemblyNonPublic;

namespace RuntimeNullables.Tests;

/// <summary>
/// TestAssemblyNonPublic only checks the public API's inputs, like a Release build with the default settings.
/// </summary>
[TestClass]
public class PublicApiTests
{
    [TestMethod]
    public void NoChecksOnAbstractInternalOnly()
    {
        new InternalSubClass().AbstractInternalMethod(null!);
    }

    [TestMethod]
    public void NoChecksOnVirtualInternalOnly()
    {
        new InternalSubClass().VirtualInternalMethod(null!);
    }

    [TestMethod]
    public void NoChecksOnInterfaceInternalOnly()
    {
        var instance = (IInternalInterface)new InternalSubClass();
        instance.InterfaceInternalMethod(null!);
    }

    [TestMethod]
    public void ChecksOnOverrideOfAbstractPublicMethod()
    {
        var instance = new InternalSubClass();
        NullCheckAssert.ThrowsArgumentNull("value", () => instance.AbstractPublicMethod(null!));
    }

    [TestMethod]
    public void ChecksOnOverrideOfVirtualPublicMethod()
    {
        var instance = new InternalSubClass();
        NullCheckAssert.ThrowsArgumentNull("value", () => instance.VirtualPublicMethod(null!));
    }

    [TestMethod]
    public void ChecksOnOverrideOfInterfacePublicMethod()
    {
        var instance = (IPublicInterface)new InternalSubClass();
        NullCheckAssert.ThrowsArgumentNull("value", () => instance.InterfacePublicMethod(null!));
    }

    [TestMethod]
    public void ChecksOnOverrideOfGenericBaseMethod()
    {
        var instance = new InternalGenericSubClass();
        NullCheckAssert.ThrowsArgumentNull("text", () => instance.AbstractGenericMethod(0, null!));
    }

    [TestMethod]
    public void ChecksOnImplicitImplementationOfPublicInterface()
    {
        var instance = (IPublicInterface)new InternalImplicitImplementation();
        NullCheckAssert.ThrowsArgumentNull("value", () => instance.InterfacePublicMethod(null!));
    }

    [TestMethod]
    public void ChecksOnImplicitImplementationOfPublicGenericInterface()
    {
        var instance = (IPublicGenericInterface<int>)new InternalImplicitImplementation();
        NullCheckAssert.ThrowsArgumentNull("text", () => instance.InterfaceGenericMethod(0, null!));
    }

    [TestMethod]
    public void NoChecksOnImplicitImplementationOfInternalInterface()
    {
        var instance = (IInternalInterface)new InternalImplicitImplementation();
        instance.InterfaceInternalMethod(null!);
    }

    [TestMethod]
    public void ChecksOnInheritedImplementationOfPublicInterface()
    {
        var instance = (IPublicInterface)new InternalInheritedImplementation();
        NullCheckAssert.ThrowsArgumentNull("value", () => instance.InterfacePublicMethod(null!));
    }

    [TestMethod]
    public void ChecksOnImplicitImplementationOfPublicInterfaceProperty()
    {
        var instance = (IPublicMembersInterface)new InternalImplicitImplementation();
        NullCheckAssert.ThrowsArgumentNull("value", () => instance.InterfaceProperty = null!);
    }

    [TestMethod]
    public void ChecksOnImplicitImplementationOfPublicInterfaceEvent()
    {
        var instance = (IPublicMembersInterface)new InternalImplicitImplementation();
        NullCheckAssert.ThrowsArgumentNull("value", () => instance.InterfaceEvent += null!);
    }

#if NET7_0_OR_GREATER
    [TestMethod]
    public void ChecksOnImplementationOfPublicStaticAbstractMethod()
    {
        NullCheckAssert.ThrowsArgumentNull("value", () => CallStaticMethod<InternalStaticImplementation>(null!));

        static void CallStaticMethod<T>(string value) where T : IPublicStaticInterface => T.InterfaceStaticMethod(value);
    }
#endif

    [TestMethod]
    public void ChecksOnProtectedConstructor()
    {
        NullCheckAssert.ThrowsArgumentNull("value", () => new InternalSubClass(null!));
    }

    [TestMethod]
    public void ChecksOnProtectedMethod()
    {
        NullCheckAssert.ThrowsArgumentNull("value", () => new InternalSubClass().CallProtectedMethod(null!));
    }

    [TestMethod]
    public void ChecksOnProtectedInternalMethod()
    {
        NullCheckAssert.ThrowsArgumentNull("value", () => new InternalSubClass().CallProtectedInternalMethod(null!));
    }

    [TestMethod]
    public void NoChecksOnPrivateProtectedMethod()
    {
        new InternalSubClass().CallPrivateProtectedMethod(null!);
    }

    [TestMethod]
    public void ChecksOnProtectedNestedTypeMethod()
    {
        NullCheckAssert.ThrowsArgumentNull("value", () => InternalSubClass.CallProtectedNestedMethod(null!));
    }

    [TestMethod]
    public void NoChecksOnPrivateProtectedNestedTypeMethod()
    {
        InternalSubClass.CallPrivateProtectedNestedMethod(null!);
    }

    [TestMethod]
    public void NoChecksOnProtectedMethodOfSealedType()
    {
        PublicSealedClass.CallProtectedMethod(null);
    }

    [TestMethod]
    public void NoChecksOnInternalTypeNestedInPublicType()
    {
        PublicContainingNestedNonPublic.Nested.Method(null!);
    }

    [TestMethod]
    public void NoChecksOnPublicTypeNestedInInternalType()
    {
        InternalContainingNestedPublic.Nested.Method(null!);
    }

    // The settings take precedence over [NullChecks(true)]:

    [TestMethod]
    public void NoChecksOnInternalMethodWithNullChecks()
    {
        PublicBaseClass.CallInternalMethodWithNullChecks(null);
    }

    [TestMethod]
    public void NoChecksOnOutputs()
    {
        Assert.IsNull(PublicBaseClass.UncheckedBadReferenceReturn());
        Assert.IsNull(PublicBaseClass.UncheckedBadReferenceReturnWithNullChecks());
    }
}
