using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;

namespace CLILL
{
    partial class Compiler
    {
        private static unsafe void CompileGlobals(CompilationContext context)
        {
            var globals = context.LLVMModule.GetGlobals().ToList();

            if (globals.Count == 0)
            {
                return;
            }

            var constructor = context.TypeBuilder.DefineTypeInitializer();
            var ilGenerator = constructor.GetILGenerator();

            foreach (var global in globals)
            {
                switch (global.Kind)
                {
                    case LLVMValueKind.LLVMGlobalVariableValueKind:
                        var valueType = (LLVMTypeRef)LLVM.GlobalGetValueType(global);
                        var globalValue = global.GetOperand(0);
                        switch (globalValue.Kind)
                        {
                            case LLVMValueKind.LLVMConstantDataArrayValueKind:
                                var elementType = GetMsilType(valueType.ElementType, context);

                                var globalField = context.TypeBuilder.DefineField(
                                    global.Name.Replace(".", string.Empty),
                                    elementType.MakePointerType(),
                                    FieldAttributes.Private | FieldAttributes.Static);

                                context.Globals.Add(global, globalField);

                                ilGenerator.Emit(OpCodes.Ldc_I4, context.GetSizeOfTypeInBytes(valueType));
                                ilGenerator.Emit(OpCodes.Conv_U);
                                ilGenerator.Emit(OpCodes.Call, typeof(NativeMemory).GetMethod("Alloc", [typeof(UIntPtr)]));
                                ilGenerator.Emit(OpCodes.Stsfld, globalField);

                                var elementSizeInBytes = context.GetSizeOfTypeInBytes(valueType.ElementType);

                                for (var i = 0; i < valueType.ArrayLength; i++)
                                {
                                    ilGenerator.Emit(OpCodes.Ldsfld, globalField);
                                    //ilGenerator.Emit(OpCodes.Conv_U);
                                    if (i > 0)
                                    {
                                        ilGenerator.Emit(OpCodes.Ldc_I4, i * elementSizeInBytes);
                                        ilGenerator.Emit(OpCodes.Add);
                                    }
                                    var elementValue = globalValue.GetAggregateElement((uint)i);
                                    EmitLoadConstantAndStoreIndirect(ilGenerator, valueType.ElementType, elementValue);
                                }
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void EmitLoadConstantAndStoreIndirect(ILGenerator ilGenerator, LLVMTypeRef type, LLVMValueRef value)
        {
            switch (type.Kind)
            {
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    ilGenerator.Emit(OpCodes.Ldc_I4, (int)value.ConstIntSExt);
                    break;

                default:
                    throw new NotImplementedException();
            }

            EmitStoreIndirect(ilGenerator, type);
        }

        private static void EmitStoreIndirect(ILGenerator ilGenerator, LLVMTypeRef type)
        {
            switch (type.Kind)
            {
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    ilGenerator.Emit(type.IntWidth switch
                    {
                        8 => OpCodes.Stind_I1,
                        32 => OpCodes.Stind_I4,
                        _ => throw new NotImplementedException()
                    });
                    break;

                case LLVMTypeKind.LLVMPointerTypeKind:
                    ilGenerator.Emit(OpCodes.Stind_I);
                    break;

                default:
                    throw new NotImplementedException($"Unexpected type {type.Kind}");
            }
        }
    }
}
