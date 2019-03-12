using LLVMSharp.API;
using LLVMSharp.API.Values.Constants.GlobalValues.GlobalObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace CLILL
{
    partial class Compiler
    {
        private static MethodInfo GetOrCreateMethod(Function function, CompilationContext context)
        {
            if (!context.Functions.TryGetValue(function, out var method))
            {
                // TODO: If this is a definition, need to define it.
                if (function.IsDeclaration)
                {
                    // TODO: We assume all extern function are part of C runtime.
                    // That isn't generally true...
                    method = CreateExternMethod(
                        context,
                        function.Name,
                        function.FunctionType.IsVarArg ? CallingConventions.VarArgs : CallingConventions.Standard,
                        GetMsilType(function.FunctionType.ReturnType),
                        function.FunctionType.ParamTypes.Select(GetMsilType).ToArray());
                }
                else
                {
                    method = CompileMethod(function, context);
                }

                context.Functions.Add(function, method);
            }

            return method;
        }

        private static MethodInfo CompileMethod(Function function, CompilationContext context)
        {
            var methodBuilder = context.TypeBuilder.DefineMethod(
                function.Name,
                MethodAttributes.Static | MethodAttributes.Public, // TODO
                GetMsilType(function.FunctionType.ReturnType),
                Array.Empty<System.Type>()); // TODO: parameters

            if (function.Name == "main")
            {
                context.AssemblyBuilder.SetEntryPoint(methodBuilder);
            }

            var ilGenerator = methodBuilder.GetILGenerator();

            var functionCompilationContext = new FunctionCompilationContext(context, ilGenerator);

            foreach (var basicBlock in function.BasicBlocks)
            {
                var basicBlockLabel = functionCompilationContext.GetOrCreateLabel(basicBlock);
                ilGenerator.MarkLabel(basicBlockLabel);

                foreach (var instruction in basicBlock.Instructions)
                {
                    CompileInstruction(instruction, functionCompilationContext);
                }
            }

            return methodBuilder;
        }

        private sealed class FunctionCompilationContext
        {
            public readonly CompilationContext CompilationContext;

            public readonly ILGenerator ILGenerator;

            public readonly Dictionary<Value, LocalBuilder> Locals = new Dictionary<Value, LocalBuilder>();
            public readonly Dictionary<Value, Label> Labels = new Dictionary<Value, Label>();

            public FunctionCompilationContext(
                CompilationContext compilationContext,
                ILGenerator ilGenerator)
            {
                CompilationContext = compilationContext;
                ILGenerator = ilGenerator;
            }

            public Label GetOrCreateLabel(Value valueRef)
            {
                if (!Labels.TryGetValue(valueRef, out var result))
                {
                    Labels.Add(valueRef, result = ILGenerator.DefineLabel());
                }
                return result;
            }
        }

        private static MethodInfo CreateExternMethod(
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
