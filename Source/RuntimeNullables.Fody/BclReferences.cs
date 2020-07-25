using System;
using System.CodeDom.Compiler;
using System.Linq;
using Mono.Cecil;

namespace RuntimeNullables.Fody
{
    internal class BclReferences
    {
        /// <summary>
        /// Gets a reference to <see cref="ArgumentNullException(string?)"/>.
        /// </summary>
        public MethodReference ArgumentNullExceptionConstructor { get; }

        /// <summary>
        /// Gets a reference to <see cref="NullReferenceException(string?)"/>.
        /// </summary>
        public MethodReference NullReferenceExceptionConstructor { get; }

        /// <summary>
        /// Gets a reference to <see cref="GeneratedCodeAttribute(string?, string?)"/>.
        /// </summary>
        public MethodReference GeneratedCodeAttributeConstructor { get; }

        public TypeReference VoidType { get; }

        public TypeReference ObjectType { get; }

        public TypeReference BoolType { get; }

        public TypeReference StringType { get; }

        public TypeReference ExceptionType { get; }

        public BclReferences(ModuleWeaver weaver)
        {
            VoidType = weaver.TypeSystem.VoidReference;
            ObjectType = weaver.TypeSystem.ObjectReference;
            BoolType = weaver.TypeSystem.BooleanReference;
            StringType = weaver.TypeSystem.StringReference;
            ExceptionType = GetBclType("System.Exception", weaver);

            ArgumentNullExceptionConstructor = GetBclConstructorWithStringParameters("System.ArgumentNullException", 1, weaver);
            NullReferenceExceptionConstructor = GetBclConstructorWithStringParameters("System.NullReferenceException", 1, weaver);
            GeneratedCodeAttributeConstructor = GetBclConstructorWithStringParameters("System.CodeDom.Compiler.GeneratedCodeAttribute", 2, weaver);
        }

        private MethodReference GetBclConstructorWithStringParameters(string typeName, int paramCount, ModuleWeaver weaver)
        {
            var typeDefinition = weaver.FindTypeDefinition(typeName) ?? throw new RuntimeNullablesException($"Unable to find required BCL type '{typeName}'.");

            var constructor = typeDefinition.Methods.FirstOrDefault(
                m => m.IsConstructor && m.Parameters.Count == paramCount && m.Parameters.All(p => p.ParameterType.FullName == StringType.FullName));

            if (constructor == null)
                throw new RuntimeNullablesException($"Unable to find required BCL type constructor on '{typeName}'.");

            return weaver.ModuleDefinition.ImportReference(constructor);
        }

        private static TypeReference GetBclType(string typeName, ModuleWeaver weaver)
        {
            var type = weaver.FindTypeDefinition(typeName) ?? throw new RuntimeNullablesException($"Unable to find required BCL type '{typeName}'.");
            return weaver.ModuleDefinition.ImportReference(type);
        }
    }
}
