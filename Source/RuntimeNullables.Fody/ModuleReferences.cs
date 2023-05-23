using System;
using Mono.Cecil;

namespace RuntimeNullables.Fody;

internal class ModuleReferences
{
    private readonly MethodReference? _throwOutputNullMethod;
    private readonly MethodReference? _getAsyncResultNullExceptionMethod;

    public MethodReference ThrowArgumentNullMethod { get; }

    public MethodReference ThrowOutputNullMethod => _throwOutputNullMethod ?? throw new InvalidOperationException("Output throw helper not set.");

    public MethodReference GetAsyncResultNullExceptionMethod => _getAsyncResultNullExceptionMethod ?? throw new InvalidOperationException("Output throw helper not set.");

    public ModuleReferences(MethodReference throwArgumentNullMethod, MethodReference? throwOutputNullMethod, MethodReference? getAsyncResultNullExceptionMethod)
    {
        ThrowArgumentNullMethod = throwArgumentNullMethod;
        _throwOutputNullMethod = throwOutputNullMethod;
        _getAsyncResultNullExceptionMethod = getAsyncResultNullExceptionMethod;
    }
}