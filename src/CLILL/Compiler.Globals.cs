using System;
using System.Reflection;
using System.Reflection.Emit;
using LLVMSharp.Interop;

namespace CLILL
{
    partial class Compiler
    {
        private static unsafe void CompileGlobals(CompilationContext context)
        {
            var constructor = context.TypeBuilder.DefineTypeInitializer();
            var ilGenerator = constructor.GetILGenerator();

            var global = context.LLVMModule.FirstGlobal;
            while (global != null)
            {
                // TODO
                var fieldType = GetMsilType((LLVMTypeRef)LLVM.GlobalGetValueType(global));
                var globalField = context.TypeBuilder.DefineField(
                    global.Name.Replace(".", string.Empty), 
                    fieldType, 
                    FieldAttributes.Private | FieldAttributes.Static);

                var globalValue = global.GetOperand(0);
                switch (globalValue.Kind)
                {
                    case LLVMValueKind.LLVMConstantDataArrayValueKind:
                        var arrayType = globalValue.TypeOf;
                        ilGenerator.Emit(OpCodes.Ldc_I4, (int) arrayType.ArrayLength);
                        ilGenerator.Emit(OpCodes.Newarr, GetMsilType(arrayType.ElementType));
                        for (var i = 0u; i < arrayType.ArrayLength; i++)
                        {
                            ilGenerator.Emit(OpCodes.Dup);
                            ilGenerator.Emit(OpCodes.Ldc_I4, i);
                            EmitLoadConstantAndStoreElement(ilGenerator, globalValue.GetAggregateElement(i));
                        }
                        break;
                }

                ilGenerator.Emit(OpCodes.Stsfld, globalField);
                ilGenerator.Emit(OpCodes.Ret);

                context.Globals.Add(global, globalField);

                global = global.NextGlobal;
            }
        }

        private static void EmitLoadConstantAndStoreElement(ILGenerator ilGenerator, LLVMValueRef value)
        {
            switch (value.Kind)
            {
                case LLVMValueKind.LLVMConstantIntValueKind:
                    var integerType = value.TypeOf;
                    switch (integerType.IntWidth)
                    {
                        case 8:
                            ilGenerator.Emit(OpCodes.Ldc_I4, (int)value.ConstIntSExt);
                            ilGenerator.Emit(OpCodes.Stelem_I1);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
