using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CLILL.Helpers;
using CLILL.Runtime;
using LLVMSharp.Interop;

namespace CLILL;

partial class Compiler
{
    private unsafe MethodInfo GetOrCreateMethod(LLVMValueRef function, CompilationContext context)
    {
        if (!context.Functions.TryGetValue(function, out var method))
        {
            if (function.IsDeclaration)
            {
                method = GetOrCreateMethodDeclaration(function, context);
            }
            else
            {
                var methodBuilder = CompileMethod(function, context);

                _methodsToCompile.Enqueue((function, methodBuilder));

                method = methodBuilder;
            }

            context.Functions.Add(function, method);
        }

        return method;
    }

    private static unsafe MethodInfo GetOrCreateMethodDeclaration(LLVMValueRef function, CompilationContext context)
    {
        switch (function.Name)
        {
            case var _ when function.Name.StartsWith("llvm."):
                return GetOrCreateLlvmIntrinsicMethod(function);

            default:
                // TODO: We assume all extern function are part of C runtime.
                // That isn't generally true...

                var functionType = (LLVMTypeRef)LLVM.GlobalGetValueType(function);

                return CreateExternMethod(
                    context,
                    function.Name,
                    functionType.IsFunctionVarArg ? CallingConventions.VarArgs : CallingConventions.Standard,
                    GetMsilType(functionType.ReturnType, context),
                    functionType.ParamTypes.Select(x => GetMsilType(x, context)).ToArray());
        }
    }

    private static MethodInfo GetOrCreateLlvmIntrinsicMethod(LLVMValueRef function)
    {
        var methodName = function.Name switch
        {
            "llvm.assume" => nameof(LLVMIntrinsics.Assume),
            "llvm.fabs.f32" => nameof(LLVMIntrinsics.FAbsF32),
            "llvm.fmuladd.f32" => nameof(LLVMIntrinsics.FMulAddF32),
            "llvm.fmuladd.f64" => nameof(LLVMIntrinsics.FMulAddF64),
            "llvm.fmuladd.v2f32" => nameof(LLVMIntrinsics.FMulAddV2F32),
            "llvm.fmuladd.v2f64" => nameof(LLVMIntrinsics.FMulAddV2F64),
            "llvm.lifetime.end.p0" => nameof(LLVMIntrinsics.LifetimeEndP0),
            "llvm.lifetime.start.p0" => nameof(LLVMIntrinsics.LifetimeStartP0),
            "llvm.memcpy.p0.p0.i64" => nameof(LLVMIntrinsics.MemCpyI64),
            "llvm.memset.p0.i64" => nameof(LLVMIntrinsics.MemSetI64),
            "llvm.smax.i32" => nameof(LLVMIntrinsics.SMaxI32),
            "llvm.smax.v4i32" => nameof(LLVMIntrinsics.SMaxV4I32),
            "llvm.sqrt.f32" => nameof(LLVMIntrinsics.SqrtF32),
            "llvm.sqrt.f64" => nameof(LLVMIntrinsics.SqrtF64),
            "llvm.stackrestore" or "llvm.stackrestore.p0" => nameof(LLVMIntrinsics.StackRestore),
            "llvm.stacksave" or "llvm.stacksave.p0" => nameof(LLVMIntrinsics.StackSave),
            "llvm.usub.sat.i32" => nameof(LLVMIntrinsics.USubSatI32),
            "llvm.vector.reduce.add.v4i32" => nameof(LLVMIntrinsics.VectorReduceAddV4I32),
            "llvm.vector.reduce.mul.v4i32" => nameof(LLVMIntrinsics.VectorReduceMulV4I32),
            "llvm.vector.reduce.smax.v4i32" => nameof(LLVMIntrinsics.VectorReduceSMaxV4I32),
            _ => throw new NotImplementedException($"Unknown LLVM intrinsic: {function.Name}"),
        };

        return typeof(LLVMIntrinsics).GetMethodStrict(methodName);
    }

    private static unsafe MethodBuilder CompileMethod(LLVMValueRef function, CompilationContext context)
    {
        var functionType = (LLVMTypeRef)LLVM.GlobalGetValueType(function);

        var parameters = function.Params;
        var parameterTypes = new Type[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameterTypes[i] = GetMsilType(parameters[i].TypeOf, context);
        }

        var result = context.TypeBuilder.DefineMethod(
            function.Name,
            MethodAttributes.Static | MethodAttributes.Public, // TODO
            functionType.IsFunctionVarArg ? CallingConventions.VarArgs : CallingConventions.Standard,
            GetMsilType(functionType.ReturnType, context),
            parameterTypes);

        var skipLocalsInitAttribute = new CustomAttributeBuilder(
            typeof(SkipLocalsInitAttribute).GetConstructorStrict([]),
            []);
        result.SetCustomAttribute(skipLocalsInitAttribute);

        return result;
    }

    private unsafe MethodBuilder CompileMethodBody(
        LLVMValueRef function,
        MethodBuilder methodBuilder,
        CompilationContext context)
    {
        var ilGenerator = methodBuilder.GetILGenerator();

        // Figure out which instructions need their results stored in local variables,
        // and which can be pushed to the stack.
        var canPushToStackLookup = new Dictionary<LLVMValueRef, bool>();
        foreach (var basicBlock in function.BasicBlocks)
        {
            var blockInstructions = basicBlock.GetInstructions().ToList();
            for (var i = 0; i < blockInstructions.Count; i++)
            {
                canPushToStackLookup.Add(blockInstructions[i], CanPushToStack(blockInstructions, i));
            }
        }

        var functionCompilationContext = new FunctionCompilationContext(context, ilGenerator, canPushToStackLookup);

        for (var i = 0; i < function.Params.Length; i++)
        {
            var parameter = function.Params[i];
            var parameterIndex = i + 1;

            var parameterName = GetParameterName(function, parameterIndex);

            var parameterBuilder = methodBuilder.DefineParameter(
                parameterIndex,
                ParameterAttributes.None, // TODO
                parameterName);

            functionCompilationContext.Parameters.Add(parameter, parameterBuilder);
        }

        foreach (var basicBlock in function.BasicBlocks)
        {
            foreach (var instruction in basicBlock.GetInstructions())
            {
                switch (instruction.InstructionOpcode)
                {
                    case LLVMOpcode.LLVMPHI:
                        functionCompilationContext.PhiLocals.Add(
                            instruction,
                            ilGenerator.DeclareLocal(GetMsilType(instruction.TypeOf, context)));
                        break;
                }
            }
        }

        foreach (var basicBlock in function.BasicBlocks)
        {
            var basicBlockLabel = functionCompilationContext.GetOrCreateLabel(basicBlock);
            ilGenerator.MarkLabel(basicBlockLabel);

            foreach (var instruction in basicBlock.GetInstructions())
            {
                if (!functionCompilationContext.CanPushToStack(instruction)
                    && instruction.InstructionOpcode != LLVMOpcode.LLVMPHI)
                {
                    CompileInstruction(instruction, functionCompilationContext);
                }
            }
        }

        return methodBuilder;
    }

    private static string? GetParameterName(LLVMValueRef function, int parameterIndex)
    {
        foreach (var basicBlock in function.BasicBlocks)
        {
            foreach (var instruction in basicBlock.GetInstructions())
            {
                if (instruction.InstructionOpcode == LLVMOpcode.LLVMCall)
                {
                    var operands = instruction.GetOperands().ToList();

                    switch (operands[^1].Name)
                    {
                        case "llvm.dbg.declare":
                        case "llvm.dbg.value":
                            var diLocalVariable = instruction.GetOperand(1);
                            var diLocalVariableArg = diLocalVariable.GetDILocalVariableArg();
                            if (diLocalVariableArg == parameterIndex)
                            {
                                return diLocalVariable.GetDILocalVariableName();
                            }
                            break;
                    }
                }
            }
        }

        return null;
    }

    private static bool CanPushToStack(List<LLVMValueRef> instructions, int index)
    {
        var instruction = instructions[index];
        var users = instruction.GetUses().ToList();

        if (users.Count != 1)
        {
            return false;
        }

        var user = users[0];

        // If it's used in a different block from where it's executed,
        // we can't push it to the stack because we can't guarantee the
        // order of execution. (With more effort we perhaps could.)
        if (user.InstructionParent != instruction.InstructionParent)
        {
            return false;
        }

        // We can never inline an alloca instruction.
        if (instruction.InstructionOpcode == LLVMOpcode.LLVMAlloca)
        {
            return false;
        }

        // If user is next instruction, then we can always inline it,
        // regardless of the current or next instruction types.
        if (user == instruction.NextInstruction)
        {
            return true;
        }

        if (instruction.InstructionOpcode == LLVMOpcode.LLVMLoad)
        {
            // Make sure the result of this load instruction is used
            // before anything that might change its value.
            for (var j = index + 1; j < instructions.Count; j++)
            {
                if (instructions[j] == user)
                {
                    return true;
                }
                if (!instructions[j].HasNoSideEffects())
                {
                    return false;
                }
            }
            throw new InvalidOperationException("Shouldn't be here");
        }
        else if (instruction.HasNoSideEffects())
        {
            return true;
        }

        return false;
    }

    private sealed class FunctionCompilationContext
    {
        public readonly CompilationContext CompilationContext;

        public readonly ILGenerator ILGenerator;

        private readonly Dictionary<LLVMValueRef, bool> CanPushToStackLookup;

        public readonly Dictionary<LLVMValueRef, ParameterBuilder> Parameters = [];
        public readonly Dictionary<LLVMValueRef, LocalBuilder> Locals = [];
        public readonly Dictionary<LLVMBasicBlockRef, Label> Labels = [];

        public readonly Dictionary<LLVMValueRef, LocalBuilder> PhiLocals = [];

        public FunctionCompilationContext(
            CompilationContext compilationContext,
            ILGenerator ilGenerator,
            Dictionary<LLVMValueRef, bool> canPushToStackLookup)
        {
            CompilationContext = compilationContext;
            ILGenerator = ilGenerator;
            CanPushToStackLookup = canPushToStackLookup;
        }

        public Label GetOrCreateLabel(LLVMBasicBlockRef basicBlock)
        {
            if (!Labels.TryGetValue(basicBlock, out var result))
            {
                Labels.Add(basicBlock, result = ILGenerator.DefineLabel());
            }
            return result;
        }

        public bool CanPushToStack(LLVMValueRef valueRef)
        {
            return CanPushToStackLookup.TryGetValue(valueRef, out var value) && value;
        }
    }

    private static MethodBuilder CreateExternMethod(
        CompilationContext context,
        string name,
        CallingConventions callingConventions,
        System.Type returnType,
        System.Type[] parameterTypes)
    {
        var methodInfo = context.TypeBuilder.DefinePInvokeMethod(
            name,
            "ucrtbase.dll",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            callingConventions,
            returnType,
            parameterTypes,
            CallingConvention.Winapi,
            CharSet.None);

        methodInfo.SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.Managed | MethodImplAttributes.PreserveSig);

        return methodInfo;
    }
}