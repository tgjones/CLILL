﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;

namespace CLILL
{
    partial class Compiler
    {
        private static unsafe MethodInfo GetOrCreateMethod(LLVMValueRef function, CompilationContext context)
        {
            if (!context.Functions.TryGetValue(function, out var method))
            {
                // TODO: If this is a definition, need to define it.
                if (function.IsDeclaration)
                {
                    // TODO: We assume all extern function are part of C runtime.
                    // That isn't generally true...

                    var functionType = (LLVMTypeRef)LLVM.GlobalGetValueType(function);
                    
                    method = CreateExternMethod(
                        context,
                        function.Name,
                        functionType.IsFunctionVarArg ? CallingConventions.VarArgs : CallingConventions.Standard,
                        GetMsilType(functionType.ReturnType),
                        functionType.ParamTypes.Select(GetMsilType).ToArray());
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

            var methodBuilder = context.TypeBuilder.DefineMethod(
                function.Name,
                MethodAttributes.Static | MethodAttributes.Public, // TODO
                GetMsilType(functionType.ReturnType),
                []); // TODO: parameters

            var ilGenerator = methodBuilder.GetILGenerator();

            var functionCompilationContext = new FunctionCompilationContext(context, ilGenerator);

            foreach (var basicBlock in function.BasicBlocks)
            {
                var basicBlockLabel = functionCompilationContext.GetOrCreateLabel(basicBlock.AsValue());
                ilGenerator.MarkLabel(basicBlockLabel);

                var instruction = basicBlock.FirstInstruction;
                while (instruction.Handle != IntPtr.Zero)
                {
                    CompileInstruction(instruction, functionCompilationContext);
                    instruction = instruction.NextInstruction;

                }
            }

            return methodBuilder;
        }

        private sealed class FunctionCompilationContext
        {
            public readonly CompilationContext CompilationContext;

            public readonly ILGenerator ILGenerator;

            public readonly Dictionary<LLVMValueRef, LocalBuilder> Locals = new Dictionary<LLVMValueRef, LocalBuilder>();
            public readonly Dictionary<LLVMValueRef, Label> Labels = new Dictionary<LLVMValueRef, Label>();

            public FunctionCompilationContext(
                CompilationContext compilationContext,
                ILGenerator ilGenerator)
            {
                CompilationContext = compilationContext;
                ILGenerator = ilGenerator;
            }

            public Label GetOrCreateLabel(LLVMValueRef valueRef)
            {
                if (!Labels.TryGetValue(valueRef, out var result))
                {
                    Labels.Add(valueRef, result = ILGenerator.DefineLabel());
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
