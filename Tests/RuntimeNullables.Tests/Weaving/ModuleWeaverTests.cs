using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using RuntimeNullables.Tests.Infrastructure;
using RuntimeNullables.Weaver;

namespace RuntimeNullables.Tests;

/// <summary>
/// Weaves test assemblies in memory to test what the weaver does to a module before it's written.
/// </summary>
[TestClass]
public class ModuleWeaverTests
{
    private const string AssemblyName = "TestAssembly";
    private const string NullChecksAttributeFullName = "RuntimeNullables.NullChecksAttribute";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void DefaultSettingsDependOnDebugConstant(bool isDebug)
    {
        using var module = TestFiles.ReadUnwovenModule("TestAssemblySigned");
        var type = module.GetType("TestAssemblySigned.Signed");

        var logger = new TestWeaverLogger();
        new ModuleWeaver(module, logger) { DefineConstants = isDebug ? ["DEBUG", "TRACE"] : ["TRACE"] }.Execute();

        Assert.AreEqual(0, logger.Errors.Count, logger.ToString());
        Assert.AreEqual(isDebug ? 2 : 1, CountThrowHelperCalls(type, "Echo"), "The public API's inputs, and outputs.");
        Assert.AreEqual(isDebug ? 1 : 0, CountThrowHelperCalls(type, "InternalMethod"), "Members outside the public API.");
        Assert.AreEqual(isDebug ? 1 : 0, CountThrowHelperCalls(type, "BadReturn"), "Outputs.");
    }

    [TestMethod]
    public void TestAssemblyIsWovenWithoutWarnings()
    {
        // This includes async methods that never return a result and async iterators that never yield a value (TaskResults.ThrowingAsync,
        // AsyncEnumerables.GetNoReferencesAsync etc.), for which the compiler leaves out the code that sets a result.

        using var module = TestFiles.ReadUnwovenModule(AssemblyName);
        var logger = Weave(module);

        Assert.AreEqual(0, logger.Warnings.Count, logger.ToString());
    }

    [TestMethod]
    [DataRow("TestAssembly.InputParameters", "Reference", false, "AllowNull", "DisallowNull", "conflicting preconditions on parameter 'value'")]
    [DataRow("TestAssembly.OutputParameters", "BadReference", false, "MaybeNull", "NotNull", "conflicting postconditions on parameter 'value'")]
    [DataRow("TestAssembly.Returns", "BadReference", true, "MaybeNull", "NotNull", "Conflicting return value postconditions")]
    public void ConflictingAnnotationsSkipTheCheckWithWarning(
        string typeName, string methodName, bool onReturnValue, string attribute1, string attribute2, string warning)
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);
        var method = module.GetType(typeName).Methods.Single(m => m.Name == methodName);
        ICustomAttributeProvider annotatedItem = onReturnValue ? method.MethodReturnType : method.Parameters[0];

        foreach (string attributeName in new[] { attribute1, attribute2 })
        {
            var attributeType = Type.GetType($"System.Diagnostics.CodeAnalysis.{attributeName}Attribute", throwOnError: true)!;
            annotatedItem.CustomAttributes.Add(new CustomAttribute(module.ImportReference(attributeType.GetConstructor(Type.EmptyTypes))));
        }

        var logger = Weave(module);

        Assert.AreEqual(0, TestFiles.CountThrowHelperCalls(method));
        Assert.IsTrue(logger.Warnings.Any(w => w.Message.Contains(warning, StringComparison.Ordinal)), logger.ToString());
    }

    [TestMethod]
    public void ConflictingConditionalAnnotationsSkipTheCheckWithWarning()
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);
        var method = module.GetType("TestAssembly.ConditionalOutputParameters").Methods.Single(m => m.Name == "NotNullWhenTrue");

        // The output already has [NotNullWhen(true)]. With [MaybeNullWhen(true)] too, it may be null for both results.

        var attribute = new CustomAttribute(module.ImportReference(typeof(MaybeNullWhenAttribute).GetConstructor([typeof(bool)])));
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.Boolean, true));
        method.Parameters.Single(p => p.Name == "output").CustomAttributes.Add(attribute);

        var logger = Weave(module);

        Assert.AreEqual(0, TestFiles.CountThrowHelperCalls(method));
        string warning = "conflicting postconditions on parameter 'output'";
        Assert.IsTrue(logger.Warnings.Any(w => w.Message.Contains(warning, StringComparison.Ordinal)), logger.ToString());
    }

    [TestMethod]
    public void NullChecksOnGeneratedCodeIsReported()
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);

        var generatedType = module.GetType("TestAssembly.GeneratedCode");
        var nestedMethod = generatedType.NestedTypes.Single(t => t.Name == "Nested").Methods.Single(m => m.Name == "Method");
        var partlyGeneratedType = module.GetType("TestAssembly.PartlyGeneratedCode");
        var generatedMethod = partlyGeneratedType.Methods.Single(m => m.Name == "GeneratedMethod");
        var generatedProperty = partlyGeneratedType.Properties.Single(p => p.Name == "GeneratedPropertyWithAttribute");
        var excludedType = module.GetType("System.Diagnostics.CodeAnalysis.CodeAnalysisNamespaceType");

        AddNullChecks(module, generatedType, true);
        AddNullChecks(module, nestedMethod, true);
        AddNullChecks(module, generatedMethod.MethodReturnType, true);
        AddNullChecks(module, generatedProperty, true);
        AddNullChecks(module, generatedProperty.GetMethod, true);
        AddNullChecks(module, excludedType, true);

        // No warnings: [NullChecks(false)] agrees with generated code not being checked, the other method is checked, and the accessors of a property
        // that isn't marked as generated are checked even if they are ([CompilerGenerated] like auto-property accessors). The test assembly's own
        // [assembly: NullChecks(true)] doesn't warn either, since it covers everything rather than generated code.

        AddNullChecks(module, generatedMethod, false);
        AddNullChecks(module, partlyGeneratedType.Methods.Single(m => m.Name == "Method"), true);
        AddNullChecks(module, partlyGeneratedType.Properties.Single(p => p.Name == "GeneratedAccessorProperty").GetMethod, true);

        var logger = Weave(module);
        string generatedTypeReason = "'TestAssembly.GeneratedCode' is generated code, which isn't checked.";

        string[] expectedWarnings =
        [
            $"[NullChecks(true)] on 'TestAssembly.GeneratedCode' has no effect: {generatedTypeReason}",
            $"[NullChecks(true)] on '{nestedMethod}' has no effect: {generatedTypeReason}",
            $"[NullChecks(true)] on the return value of '{generatedMethod}' has no effect: it's generated code, which isn't checked.",
            $"[NullChecks(true)] on '{generatedProperty}' has no effect: it's generated code, which isn't checked.",
            $"[NullChecks(true)] on '{generatedProperty.GetMethod}' has no effect: it's generated code, which isn't checked.",
            $"[NullChecks(true)] on '{excludedType}' has no effect: '{excludedType}' is in the System.Diagnostics.CodeAnalysis namespace, which isn't checked.",
        ];

        CollectionAssert.AreEquivalent(expectedWarnings, logger.Warnings.Select(w => w.Message).ToList(), logger.ToString());
        Assert.IsTrue(logger.Warnings.All(w => w.Kind == WeaverWarningKind.ChecksSkipped));
    }

    [TestMethod]
    public void ExtensionMembersAreCheckedThroughTheirImplementationMethods()
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);

        var type = module.GetType("TestAssembly.ExtensionMembers");
        var groupingType = type.NestedTypes.Single(t => t.Properties.Any(p => p.Name == "Property"));
        var property = groupingType.Properties.Single(p => p.Name == "Property");
        var generatedProperty = groupingType.Properties.Single(p => p.Name == "StaticProperty");

        // [NullChecks(true)] on the declarations of checked members doesn't warn, since it applies to their implementation methods.

        AddNullChecks(module, property, true);
        AddNullChecks(module, groupingType.Methods.Single(m => m.Name == "Echo"), true);

        // A property's attributes apply to the implementation methods of its accessors, including the attributes that mark it as generated.

        generatedProperty.CustomAttributes.Add(new CustomAttribute(module.ImportReference(typeof(CompilerGeneratedAttribute).GetConstructor([]))));
        AddNullChecks(module, generatedProperty, true);

        var logger = Weave(module);

        string[] expectedWarnings = [$"[NullChecks(true)] on '{generatedProperty}' has no effect: it's generated code, which isn't checked."];
        CollectionAssert.AreEqual(expectedWarnings, logger.Warnings.Select(w => w.Message).ToList(), logger.ToString());

        Assert.IsTrue(type.NestedTypes.SelectMany(t => t.Methods).All(m => TestFiles.CountThrowHelperCalls(m) == 0), "Declarations aren't checked.");
        Assert.AreEqual(2, CountThrowHelperCalls(type, "set_Property"));
        Assert.AreEqual(0, CountThrowHelperCalls(type, "get_StaticProperty"));
        Assert.AreEqual(0, CountThrowHelperCalls(type, "set_StaticProperty"));
    }

    [TestMethod]
    public void RuntimeAsyncMethodWithUnknownReturnTypeIsReported()
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);

        // Mark a method that returns a string as a runtime async method (MethodImplAttributes.Async), which only Task and ValueTask can be.

        var method = module.GetType("TestAssembly.Returns").Methods.Single(m => m.Name == "BadReference");
        method.ImplAttributes |= (MethodImplAttributes)0x2000;

        var logger = Weave(module);

        Assert.AreEqual(0, TestFiles.CountThrowHelperCalls(method));
        Assert.IsTrue(logger.Warnings.Any(w => w.Message.Contains("Unknown Task return type", StringComparison.Ordinal)), logger.ToString());
    }

    [TestMethod]
    public void OnlyArgumentNullHelperIsInjectedWithoutOutputChecks()
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);
        Weave(module, checkOutputs: false);

        var methodNames = module.GetType("RuntimeNullables.ThrowHelpers").Methods.Select(m => m.Name).ToList();
        CollectionAssert.AreEqual(new[] { "ThrowArgumentNull" }, methodNames);

        // No output checks, and no copies of arguments for them:

        var method = module.GetType("TestAssembly.ArgumentPostconditions").Methods.Single(m => m.Name == "BadThrowIfNull");
        Assert.AreEqual(0, TestFiles.CountThrowHelperCalls(method));
        Assert.AreEqual(0, method.Body.Variables.Count);
    }

    [TestMethod]
    public void MissingCustomThrowHelperIsInjected()
    {
        using var module = TestFiles.ReadUnwovenModule("TestAssemblyThrowHelpers");
        var throwHelpers = module.GetType("RuntimeNullables.ThrowHelpers");
        throwHelpers.Methods.Remove(throwHelpers.Methods.Single(m => m.Name == "GetAsyncResultNullException"));

        Weave(module);

        // Only the injected method is marked as generated, since the class is the project's own.

        var injectedMethod = throwHelpers.Methods.Single(m => m.Name == "GetAsyncResultNullException");
        Assert.IsTrue(injectedMethod.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(GeneratedCodeAttribute).FullName));
        Assert.IsFalse(throwHelpers.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(GeneratedCodeAttribute).FullName));
        Assert.IsFalse(throwHelpers.Methods.Single(m => m.Name == "ThrowOutputNull").CustomAttributes.Any());
    }

    [TestMethod]
    public void AllNullChecksAttributesAreRemoved()
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);

        // The weaver doesn't add checks to the members of NullChecksOnSkippedItems, but their attributes have to go too.

        var owners = GetNullChecksAttributeOwners(module).ToList();
        string ownerList = string.Join("\n", owners);

        CollectionAssert.Contains(owners, module.Assembly.FullName, ownerList);
        CollectionAssert.Contains(owners, module.Name, ownerList);
        Assert.IsTrue(owners.Any(o => o.Contains("NullChecksOnSkippedItems/IInterface", StringComparison.Ordinal)), ownerList);
        Assert.IsTrue(owners.Any(o => o.Contains("NullChecksOnSkippedItems/GeneratedCode", StringComparison.Ordinal)), ownerList);

        Weave(module);

        CollectionAssert.AreEqual(Array.Empty<string>(), GetNullChecksAttributeOwners(module).ToList());
    }

    [TestMethod]
    public void InjectedGeneratedCodeAttributeKeepsItsArgumentsWhenReadBeforeWriting()
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);
        Weave(module);

        var weaverName = typeof(ModuleWeaver).Assembly.GetName();
        object[] expectedArguments = [weaverName.Name!, weaverName.Version!.ToString()];

        CollectionAssert.AreEqual(expectedArguments, GetGeneratedCodeArguments(module));

        using var stream = new MemoryStream();
        module.Write(stream);
        stream.Position = 0;
        using var writtenModule = ModuleDefinition.ReadModule(stream);

        CollectionAssert.AreEqual(expectedArguments, GetGeneratedCodeArguments(writtenModule));

        static object[] GetGeneratedCodeArguments(ModuleDefinition module)
        {
            var attribute = module.GetType("RuntimeNullables.ThrowHelpers").CustomAttributes
                .Single(a => a.AttributeType.FullName == typeof(GeneratedCodeAttribute).FullName);

            return attribute.ConstructorArguments.Select(a => a.Value).ToArray();
        }
    }

    [TestMethod]
    public void UnresolvableReferenceWithThrowingResolverIsReported()
    {
        // The module uses Cecil's default resolver, which throws for assemblies it can't find instead of returning null like the weaver's resolver.

        using var module = TestFiles.ReadUnwovenModule(AssemblyName);

        var taskType = MissingAssembly.GetType(module, "System.Threading.Tasks", "Task`1", genericParameterCount: 1);
        var method = module.GetType("TestAssembly.TaskResults").Methods.Single(m => m.Name == "GoodReference");
        method.ReturnType = taskType.MakeGenericInstanceType(module.TypeSystem.String);

        var weaver = new ModuleWeaver(module, new TestWeaverLogger()) { CheckOutputs = true, CheckNonPublic = true };
        var exception = Assert.ThrowsException<RuntimeNullablesException>(weaver.Execute);

        var innerException = exception.InnerException as RuntimeNullablesException;

        Assert.IsNotNull(innerException, exception.ToString());
        Assert.AreEqual(
            "Could not resolve 'System.Threading.Tasks.Task`1' from 'MissingAssembly'. Make sure all of the project's compile references are available to " +
            "the weaver.",
            innerException.Message);
    }

    [TestMethod]
    public void OutputChecksKeepShortBranchesInRange()
    {
        using var module = TestFiles.ReadUnwovenModule(AssemblyName);

        // A short branch that jumps over a return, with 118 bytes in between. The output check injected before the return adds 14 more, so the branch
        // has to become a long branch.

        var method = module.GetType("TestAssembly.OutputParameters").Methods.Single(m => m.Name == "GoodReference");
        var parameter = method.Parameters[0];
        var instructions = method.Body.Instructions;
        var branchTarget = Instruction.Create(OpCodes.Ldarg, parameter);

        instructions.Clear();
        instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        instructions.Add(Instruction.Create(OpCodes.Brtrue_S, branchTarget));
        instructions.Add(Instruction.Create(OpCodes.Ldarg, parameter));
        instructions.Add(Instruction.Create(OpCodes.Ldstr, string.Empty));
        instructions.Add(Instruction.Create(OpCodes.Stind_Ref));

        for (int i = 0; i < 110; i++)
            instructions.Add(Instruction.Create(OpCodes.Nop));

        instructions.Add(Instruction.Create(OpCodes.Ret));
        instructions.Add(branchTarget);
        instructions.Add(Instruction.Create(OpCodes.Ldstr, string.Empty));
        instructions.Add(Instruction.Create(OpCodes.Stind_Ref));
        instructions.Add(Instruction.Create(OpCodes.Ret));

        Weave(module);

        using var stream = new MemoryStream();
        module.Write(stream);
        stream.Position = 0;
        using var writtenModule = ModuleDefinition.ReadModule(stream);

        var writtenInstructions = writtenModule.GetType("TestAssembly.OutputParameters").Methods.Single(m => m.Name == "GoodReference").Body.Instructions;
        var firstReturn = writtenInstructions.First(i => i.OpCode == OpCodes.Ret);

        Assert.AreEqual(OpCodes.Brtrue, writtenInstructions[1].OpCode);
        Assert.AreSame(firstReturn.Next, writtenInstructions[1].Operand);
    }

    /// <summary>
    /// Weaves the module with everything checked (or everything but outputs) and asserts that there were no errors.
    /// </summary>
    private static TestWeaverLogger Weave(ModuleDefinition module, bool checkOutputs = true)
    {
        var logger = new TestWeaverLogger();
        new ModuleWeaver(module, logger) { CheckOutputs = checkOutputs, CheckNonPublic = true }.Execute();

        Assert.AreEqual(0, logger.Errors.Count, logger.ToString());
        return logger;
    }

    private static void AddNullChecks(ModuleDefinition module, ICustomAttributeProvider item, bool enabled)
    {
        var attribute = new CustomAttribute(module.ImportReference(typeof(NullChecksAttribute).GetConstructor([typeof(bool)])));
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.Boolean, enabled));
        item.CustomAttributes.Add(attribute);
    }

    private static int CountThrowHelperCalls(TypeDefinition type, string methodName)
    {
        return TestFiles.CountThrowHelperCalls(type.Methods.Single(m => m.Name == methodName));
    }

    private static IEnumerable<string> GetNullChecksAttributeOwners(ModuleDefinition module)
    {
        return GetAttributeProviders(module)
            .Where(p => p.Provider.HasCustomAttributes && p.Provider.CustomAttributes.Any(a => a.AttributeType.FullName == NullChecksAttributeFullName))
            .Select(p => p.Owner);
    }

    private static IEnumerable<(string Owner, ICustomAttributeProvider Provider)> GetAttributeProviders(ModuleDefinition module)
    {
        yield return (module.Assembly.FullName, module.Assembly);
        yield return (module.Name, module);

        foreach (var type in module.GetTypes())
        {
            yield return (type.FullName, type);

            foreach (var genericParameter in type.GenericParameters)
                yield return ($"{type.FullName} <{genericParameter.Name}>", genericParameter);

            foreach (var field in type.Fields)
                yield return (field.FullName, field);

            foreach (var property in type.Properties)
                yield return (property.FullName, property);

            foreach (var @event in type.Events)
                yield return (@event.FullName, @event);

            foreach (var method in type.Methods)
            {
                yield return (method.FullName, method);
                yield return ($"{method.FullName} (return value)", method.MethodReturnType);

                foreach (var parameter in method.Parameters)
                    yield return ($"{method.FullName} ({parameter.Name})", parameter);

                foreach (var genericParameter in method.GenericParameters)
                    yield return ($"{method.FullName} <{genericParameter.Name}>", genericParameter);
            }
        }
    }
}
