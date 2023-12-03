using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RuntimeNullables.Fody.Contexts;

namespace RuntimeNullables.Fody;

public partial class ModuleWeaver
{
    private static void InjectThrowHelpers(WeavingContext weavingContext)
    {
        var moduleContext = weavingContext.ModuleContext;
        var module = weavingContext.ModuleContext.Module;
        var bclReferences = weavingContext.BclReferences;

        var throwHelperType = moduleContext.Module.Types.FirstOrDefault(t => t.FullName == "RuntimeNullables.ThrowHelpers");
        bool wasTypeInjected = false;

        if (throwHelperType == null) {
            throwHelperType = InjectThrowHelperType();

            weavingContext.WriteInfo("Throw helper class not found - injecting standard throw helpers.");
            wasTypeInjected = true;
        }

        const string ThrowArgumentNullMethod = "ThrowArgumentNull";

        if (!(GetThrowHelperMethod(ThrowArgumentNullMethod) is { } throwArgumentNullMethod)) {
            throwArgumentNullMethod = InjectThrowHelperMethod(ThrowArgumentNullMethod, "paramName", bclReferences.ArgumentNullExceptionConstructor);

            if (!wasTypeInjected)
                weavingContext.WriteInfo("Argument null throw helper method not found on user class - injecting standard throw helper.");
        }

        MethodDefinition throwOutputNullMethod = null;
        MethodDefinition getAsyncResultNullExceptionMethod = null;

        if (weavingContext.CheckOutputs) {
            const string ThrowOutputNullMethod = "ThrowOutputNull";

            if ((throwOutputNullMethod = GetThrowHelperMethod(ThrowOutputNullMethod)) == null) {
                throwOutputNullMethod = InjectThrowHelperMethod(ThrowOutputNullMethod, "message", bclReferences.NullReferenceExceptionConstructor);

                if (!wasTypeInjected)
                    weavingContext.WriteInfo("Output null throw helper method not found on user class - injecting standard throw helper.");
            }

            const string GetAsyncResultNullExceptionMethod = "GetAsyncResultNullException";

            if ((getAsyncResultNullExceptionMethod = GetThrowHelperMethod(GetAsyncResultNullExceptionMethod)) == null) {
                getAsyncResultNullExceptionMethod = InjectThrowHelperMethod(
                    GetAsyncResultNullExceptionMethod, "message", bclReferences.NullReferenceExceptionConstructor);

                if (!wasTypeInjected)
                    weavingContext.WriteInfo("Async result null throw helper method not found on user class - injecting standard throw helper.");
            }
        }

        weavingContext.ModuleReferences = new ModuleReferences(throwArgumentNullMethod, throwOutputNullMethod, getAsyncResultNullExceptionMethod);

        MethodDefinition? GetThrowHelperMethod(string methodName)
        {
            var method = throwHelperType.Methods.FirstOrDefault(m =>
                !m.HasGenericParameters &&
                m.Name == methodName &&
                m.Parameters.Count == 1 &&
                m.Parameters[0].ParameterType.FullName == bclReferences.StringType.FullName);

            if (method != null) {
                if (!method.IsStatic)
                    throw new RuntimeNullablesException("Throw helper methods must be static.");

                var returnType = DoesThrowHelperThrow(methodName) ? bclReferences.VoidType : bclReferences.ExceptionType;

                if (method.ReturnType.FullName != returnType.FullName)
                    throw new RuntimeNullablesException($"Throw helper method '{methodName}' must return '{returnType}'.");
            }

            return method;
        }

        MethodDefinition InjectThrowHelperMethod(string methodName, string stringParamName, MethodReference exceptionConstructor)
        {
            const MethodAttributes methodAttributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Static;

            bool throws = DoesThrowHelperThrow(methodName);

            var method = new MethodDefinition(methodName, methodAttributes, throws ? bclReferences.VoidType : bclReferences.ExceptionType);
            method.Parameters.Add(new ParameterDefinition(stringParamName, ParameterAttributes.None, bclReferences.StringType));

            if (!wasTypeInjected)
                AddGeneratedCodeAttribute(method);

            var instructions = method.Body.Instructions;

            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(Instruction.Create(OpCodes.Newobj, exceptionConstructor));
            instructions.Add(throws ? Instruction.Create(OpCodes.Throw) : Instruction.Create(OpCodes.Ret));

            throwHelperType.Methods.Add(method);

            return method;
        }

        TypeDefinition InjectThrowHelperType()
        {
            const TypeAttributes typeAttributes =
                TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

            var type = new TypeDefinition("RuntimeNullables", "ThrowHelpers", typeAttributes, bclReferences.ObjectType);

            AddGeneratedCodeAttribute(type);
            module.Types.Add(type);

            return type;
        }

        void AddGeneratedCodeAttribute(ICustomAttributeProvider item)
        {
            // [System.CodeDom.Compiler.GeneratedCodeAttribute(string tool, string version)]

            var assemblyInfo = typeof(ModuleWeaver).Assembly.GetName();

            string name = assemblyInfo.Name ?? throw new RuntimeNullablesException("Could not get runtime nullables assembly name.");
            string version = assemblyInfo.Version?.ToString() ?? throw new RuntimeNullablesException("Could not get runtime nullables version.");

            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            byte[] versionBytes = Encoding.ASCII.GetBytes(version);

            // TODO: Remove suppression when StyleCop gets C# 12 update
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly

            byte[] attributeBlob = [1, 0, (byte)nameBytes.Length, .. nameBytes, (byte)versionBytes.Length, .. versionBytes, 0, 0];

#pragma warning restore SA1010

            var attribute = new CustomAttribute(bclReferences.GeneratedCodeAttributeConstructor, attributeBlob);
            item.CustomAttributes.Add(attribute);
        }

        static bool DoesThrowHelperThrow(string methodName)
        {
            if (methodName.StartsWith("Get", StringComparison.Ordinal))
                return false;
            else if (methodName.StartsWith("Throw", StringComparison.Ordinal))
                return true;
            else // Internal sanity check
                throw new RuntimeNullablesException("Internal error: Invalid throw helper name.");
        }
    }
}