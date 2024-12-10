using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using LLVMSharp.Interop;

namespace IR2IL;

internal sealed class CompiledModule
{
    private readonly TypeSystem _typeSystem;

    private readonly Dictionary<LLVMValueRef, MethodInfo> _functionLookup = [];
    private readonly Dictionary<LLVMValueRef, FieldInfo> _globalLookup = [];

    public CompiledModule(
        TypeSystem typeSystem,
        ReadOnlySpan<CompiledGlobalVariable> globalVariables,
        ReadOnlySpan<CompiledFunction> functions)
    {
        _typeSystem = typeSystem;

        foreach (var globalVariable in globalVariables)
        {
            _globalLookup.Add(globalVariable.Global, globalVariable.Field);
        }

        foreach (var function in functions)
        {
            _functionLookup.Add(function.Function, function.MethodInfo);
        }
    }

    public TypeSystem TypeSystem => _typeSystem;

    public MethodInfo GetFunction(LLVMValueRef function) => _functionLookup[function];

    public FieldInfo GetGlobal(LLVMValueRef global) => _globalLookup[global];
}

internal sealed record CompiledGlobalVariable(LLVMValueRef Global, LLVMTypeRef Type, LLVMValueRef Value, FieldInfo Field);

internal record CompiledFunction(LLVMValueRef Function, MethodInfo MethodInfo);

internal sealed record CompiledFunctionDefinition(LLVMValueRef Function, MethodBuilder MethodBuilder)
    : CompiledFunction(Function, MethodBuilder);