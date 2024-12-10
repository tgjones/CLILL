using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using LLVMSharp.Interop;

namespace IR2IL.Intrinsics;

public readonly ref struct IntrinsicFunctionCallContext(
    MethodInfo callee,
    IReadOnlyDictionary<LLVMValueRef, LocalBuilder> locals,
    ILGenerator ilGenerator,
    LLVMValueRef[] operands,
    Action<LLVMValueRef> emitValue)
{
    public MethodInfo Callee { get; } = callee;
    public IReadOnlyDictionary<LLVMValueRef, LocalBuilder> Locals { get; } = locals;
    public ILGenerator ILGenerator { get; } = ilGenerator;
    public LLVMValueRef[] Operands { get; } = operands;
    public Action<LLVMValueRef> EmitValue { get; } = emitValue;
}
