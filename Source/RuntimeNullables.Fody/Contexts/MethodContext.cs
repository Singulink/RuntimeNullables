using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using RuntimeNullables.Fody.Extensions;

namespace RuntimeNullables.Fody.Contexts
{
    internal class MethodContext : NullableContext
    {
        private List<GenericParameterInfo>? _genericParameters;

        public MethodDefinition Method { get; }

        public TypeContext TypeContext { get; }

        public ICollection<GenericParameterInfo> GenericParameters => _genericParameters ??
            throw new InvalidOperationException("Method does not have generic parameters.");

        public MethodContext(MethodDefinition method, TypeContext typeContext) : base(method, typeContext)
        {
            Method = method;
            TypeContext = typeContext;
        }

        public MethodContext(MethodDefinition method, PropertyContext propertyContext) : base(method, propertyContext)
        {
            Method = method;
            TypeContext = propertyContext.TypeContext;
        }

        public override void Build()
        {
            base.Build();

            if (Method.HasGenericParameters)
                _genericParameters = Method.GenericParameters.Select(p => new GenericParameterInfo(p, IsContextItemNullable(p))).ToList();
        }

        public bool IsParameterNullable(ParameterDefinition parameter)
        {
            if (IsContextItemNullable(parameter))
                return true;

            return parameter.ParameterType.GetElementType() is GenericParameter genericParameter && GetGenericParameterIsNullable(genericParameter);
        }

        public bool IsReturnValueNullable()
        {
            if (IsContextItemNullable(Method.MethodReturnType))
                return true;

            return Method.ReturnType is GenericParameter genericParameter && GetGenericParameterIsNullable(genericParameter);
        }

        public bool IsReturnValueGenericArgumentNullableOrValueType()
        {
            if (!(Method.ReturnType is GenericInstanceType genericReturnType))
                throw new InvalidOperationException("Return type is not a generic instance type.");

            if (!genericReturnType.GenericArguments[0].IsReferenceType())
                return true;

            if (IsContextInnerItemNullable(Method.MethodReturnType, 0))
                return true;

            return genericReturnType.GenericArguments[0] is GenericParameter genericParameter && GetGenericParameterIsNullable(genericParameter);
        }

        /// <summary>
        /// Walks the parent hierarchy of contexts to find the cached generic parameter nullability.
        /// </summary>
        public bool GetGenericParameterIsNullable(GenericParameter parameter)
        {
            if (parameter.Owner != Method)
                return TypeContext.GetGenericParameterIsNullable(parameter);

            var paramInfo = GenericParameters.FirstOrDefault(p => p.Parameter == parameter);

            if (paramInfo.Parameter == null) {
                WeavingContext.WriteError($"Could not resolve generic parameter '{parameter}': parameter not found.");
                return true;
            }

            return paramInfo.Nullable;
        }
    }
}