using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using LLVMSharp.Interop;

namespace CLILL
{
    partial class Compiler
    {
        private static unsafe MethodInfo GetOrCreateMethod(LLVMValueRef function, CompilationContext context)
        {
            if (!context.Functions.TryGetValue(function, out var method))
            {
                if (function.IsDeclaration && !function.Name.StartsWith("llvm.memcpy"))
                {
                    switch (function.Name)
                    {
                        case "llvm.vector.reduce.add.v4i32":
                            return typeof(Vector128).GetMethod("Sum").MakeGenericMethod(typeof(int));

                        default:
                            // TODO: We assume all extern function are part of C runtime.
                            // That isn't generally true...

                            var functionType = (LLVMTypeRef)LLVM.GlobalGetValueType(function);

                            method = CreateExternMethod(
                                context,
                                function.Name,
                                functionType.IsFunctionVarArg ? CallingConventions.VarArgs : CallingConventions.Standard,
                                GetMsilType(functionType.ReturnType, context),
                                functionType.ParamTypes.Select(x => GetMsilType(x, context)).ToArray());
                            break;
                    }
                }
                else
                {
                    method = CompileMethod(function, context);
                }

                context.Functions.Add(function, method);
            }

            return method;
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

            var methodBuilder = context.TypeBuilder.DefineMethod(
                function.Name,
                MethodAttributes.Static | MethodAttributes.Public, // TODO
                GetMsilType(functionType.ReturnType, context),
                parameterTypes);

            var ilGenerator = methodBuilder.GetILGenerator();

            if (function.Name == "llvm.memcpy.p0.p0.i64")
            {
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Conv_U);
                ilGenerator.Emit(OpCodes.Call, typeof(NativeMemory).GetMethod("Copy"));
                ilGenerator.Emit(OpCodes.Ret);
                return methodBuilder;
            }

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

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                var parameterName = parameter.Name;
                //if (parameterName == "this")
                //{
                //    parameterName = "this_";
                //}

                var parameterBuilder = methodBuilder.DefineParameter(
                    i + 1,
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
                    if (!functionCompilationContext.CanPushToStackLookup[instruction]
                        && instruction.InstructionOpcode != LLVMOpcode.LLVMPHI)
                    {
                        CompileInstruction(instruction, functionCompilationContext);
                    }
                }
            }

            return methodBuilder;
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

            public readonly Dictionary<LLVMValueRef, bool> CanPushToStackLookup;

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
                "msvcr120.dll",
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
}
