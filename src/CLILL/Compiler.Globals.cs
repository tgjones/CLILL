using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using LLVMSharp.Interop;

namespace CLILL
{
    partial class Compiler
    {
        private unsafe void CompileGlobals(CompilationContext context)
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

                        var globalType = GetMsilType(valueType, context);

                        FieldBuilder globalField;
                        if (global.IsGlobalConstant)
                        {
                            var constantData = new byte[context.GetSizeOfTypeInBytes(valueType)];

                            switch (globalValue.Kind)
                            {
                                case LLVMValueKind.LLVMConstantAggregateZeroValueKind:
                                    // Nothing to do.
                                    break;

                                case LLVMValueKind.LLVMConstantDataArrayValueKind:
                                    var elementSizeInBytes = context.GetSizeOfTypeInBytes(valueType.ElementType);
                                    for (var i = 0; i < valueType.ArrayLength; i++)
                                    {
                                        var elementValue = globalValue.GetAggregateElement((uint)i);
                                        switch (elementValue.TypeOf.Kind)
                                        {
                                            case LLVMTypeKind.LLVMIntegerTypeKind:
                                                switch (elementValue.TypeOf.IntWidth)
                                                {
                                                    case 8:
                                                        constantData[i] = (byte)elementValue.ConstIntZExt;
                                                        break;

                                                    case 32:
                                                        Buffer.BlockCopy(
                                                            BitConverter.GetBytes((int)elementValue.ConstIntZExt), 
                                                            0, 
                                                            constantData, 
                                                            i * elementSizeInBytes, 
                                                            elementSizeInBytes);
                                                        break;

                                                    default:
                                                        throw new NotImplementedException($"Int width {elementValue.TypeOf.IntWidth} not implemented: {elementValue}");
                                                }
                                                break;

                                            default:
                                                throw new NotImplementedException();
                                        }
                                        
                                    }
                                    break;

                                default:
                                    throw new NotImplementedException($"Global value kind {globalValue.Kind} not implemented: {globalValue}");
                            }

                            globalField = context.TypeBuilder.DefineInitializedData(
                                global.Name.Replace(".", string.Empty),
                                constantData,
                                FieldAttributes.Private);
                        }
                        else
                        {
                            globalField = context.TypeBuilder.DefineField(
                                global.Name.Replace(".", string.Empty),
                                globalType,
                                FieldAttributes.Private | FieldAttributes.Static);

                            globalField.SetCustomAttribute(
                                new CustomAttributeBuilder(
                                    typeof(FixedAddressValueTypeAttribute).GetConstructor([]),
                                    []));

                            switch (globalValue.Kind)
                            {
                                case LLVMValueKind.LLVMConstantAggregateZeroValueKind:
                                case LLVMValueKind.LLVMConstantPointerNullValueKind:
                                    // Nothing to do.
                                    break;

                                case LLVMValueKind.LLVMConstantArrayValueKind:
                                case LLVMValueKind.LLVMConstantDataArrayValueKind:
                                    EmitLoadConstantArray(ilGenerator, context, globalValue, valueType);
                                    ilGenerator.Emit(OpCodes.Stsfld, globalField);
                                    break;

                                case LLVMValueKind.LLVMConstantFPValueKind:
                                case LLVMValueKind.LLVMConstantIntValueKind:
                                    EmitLoadConstant(ilGenerator, valueType, globalValue, context);
                                    ilGenerator.Emit(OpCodes.Stsfld, globalField);
                                    break;

                                case LLVMValueKind.LLVMConstantStructValueKind:
                                    EmitLoadConstantStruct(ilGenerator, context, globalValue, globalType);
                                    ilGenerator.Emit(OpCodes.Stsfld, globalField);
                                    break;

                                case LLVMValueKind.LLVMGlobalVariableValueKind:
                                    var otherGlobalField = context.Globals[globalValue];
                                    ilGenerator.Emit(OpCodes.Ldsflda, otherGlobalField);
                                    ilGenerator.Emit(OpCodes.Stsfld, globalField);
                                    break;

                                default:
                                    throw new NotImplementedException($"Global value kind {globalValue.Kind} not implemented: {globalValue}");
                            }
                        }

                        context.Globals.Add(global, globalField);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            ilGenerator.Emit(OpCodes.Ret);
        }

        private void EmitLoadConstantArray(
            ILGenerator ilGenerator,
            CompilationContext context,
            LLVMValueRef arrayValue,
            LLVMTypeRef arrayValueType)
        {
            EmitLoadConstantArrayOrVector(
                ilGenerator,
                context,
                arrayValue,
                arrayValueType,
                (int)arrayValueType.ArrayLength);
        }

        private void EmitLoadConstantArrayOrVector(
            ILGenerator ilGenerator,
            CompilationContext context,
            LLVMValueRef arrayOrVectorValue,
            LLVMTypeRef arrayOrVectorValueType,
            int length)
        {
            var msilType = GetMsilType(arrayOrVectorValueType, context);

            var local = ilGenerator.DeclareLocal(msilType);

            ilGenerator.Emit(OpCodes.Ldloca, local);
            ilGenerator.Emit(OpCodes.Initobj, msilType);

            var elementSizeInBytes = context.GetSizeOfTypeInBytes(arrayOrVectorValueType.ElementType);
            for (var i = 0; i < length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldloca, local);
                if (i > 0)
                {
                    ilGenerator.Emit(OpCodes.Ldc_I4, i * elementSizeInBytes);
                    ilGenerator.Emit(OpCodes.Conv_U);
                    ilGenerator.Emit(OpCodes.Add);
                }
                var elementValue = arrayOrVectorValue.GetAggregateElement((uint)i);
                EmitLoadConstant(ilGenerator, arrayOrVectorValueType.ElementType, elementValue, context);
                EmitStoreIndirect(ilGenerator, arrayOrVectorValueType.ElementType, context);
            }

            ilGenerator.Emit(OpCodes.Ldloc, local);
        }

        private void EmitLoadConstantStruct(
            ILGenerator ilGenerator, 
            CompilationContext context,
            LLVMValueRef structValue,
            Type structType)
        {
            var local = ilGenerator.DeclareLocal(structType);

            ilGenerator.Emit(OpCodes.Ldloca, local);
            ilGenerator.Emit(OpCodes.Initobj, structType);

            var structFields = structType.GetFields();
            for (var i = 0; i < structFields.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldloca, local);
                var elementValue = structValue.GetAggregateElement((uint)i);
                EmitLoadConstant(ilGenerator, elementValue.TypeOf, elementValue, context);
                ilGenerator.Emit(OpCodes.Stfld, structFields[i]);
            }

            ilGenerator.Emit(OpCodes.Ldloc, local);
        }

        private void EmitLoadConstantVector(
            ILGenerator ilGenerator,
            CompilationContext context,
            LLVMValueRef vectorValue,
            LLVMTypeRef vectorType)
        {
            switch ((context.GetSizeOfTypeInBytes(vectorType.ElementType), vectorType.VectorSize))
            {
                case (1, 4):
                    var vectorTypeMsil = GetMsilVectorType(vectorType, context);
                    EmitLoadConstantArrayOrVector(ilGenerator, context, vectorValue, vectorType, (int)vectorType.VectorSize);
                    break;

                case (4, 4):
                    var elementType = GetMsilType(vectorType.ElementType, context);
                    for (var i = 0u; i < vectorType.VectorSize; i++)
                    {
                        EmitLoadConstant(ilGenerator, vectorType.ElementType, vectorValue.GetAggregateElement(i), context);
                    }
                    ilGenerator.Emit(OpCodes.Call, typeof(Vector128).GetMethod(nameof(Vector128.Create), [elementType, elementType, elementType, elementType]));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void EmitLoadConstant(
            ILGenerator ilGenerator, 
            LLVMTypeRef type, 
            LLVMValueRef value,
            CompilationContext context)
        {
            switch (type.Kind)
            {
                case LLVMTypeKind.LLVMArrayTypeKind:
                    EmitLoadConstantArray(ilGenerator, context, value, value.TypeOf);
                    break;

                case LLVMTypeKind.LLVMFloatTypeKind:
                    ilGenerator.Emit(OpCodes.Ldc_R4, (float)value.GetConstRealDouble(out _));
                    break;

                case LLVMTypeKind.LLVMDoubleTypeKind:
                    ilGenerator.Emit(OpCodes.Ldc_R8, value.GetConstRealDouble(out _));
                    break;

                case LLVMTypeKind.LLVMIntegerTypeKind:
                    switch (type.IntWidth)
                    {
                        case 1:
                        case 8:
                        case 16:
                        case 32:
                            ilGenerator.Emit(OpCodes.Ldc_I4, (int)value.ConstIntZExt);
                            break;

                        case 64:
                            ilGenerator.Emit(OpCodes.Ldc_I8, value.ConstIntSExt);
                            break;

                        default:
                            throw new NotImplementedException($"Load constant integer width {type.IntWidth} not implemented: {value}");
                    }
                    break;

                case LLVMTypeKind.LLVMPointerTypeKind:
                    if (context.Globals.TryGetValue(value, out var globalField))
                    {
                        ilGenerator.Emit(OpCodes.Ldsflda, context.Globals[value]);
                    }
                    else if (value.Kind == LLVMValueKind.LLVMFunctionValueKind)
                    {
                        ilGenerator.Emit(OpCodes.Ldftn, GetOrCreateMethod(value, context));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    break;

                case LLVMTypeKind.LLVMStructTypeKind:
                    EmitLoadConstantStruct(ilGenerator, context, value, GetMsilType(type, context));
                    break;

                case LLVMTypeKind.LLVMVectorTypeKind:
                    EmitLoadConstantVector(ilGenerator, context, value, type);
                    break;

                default:
                    throw new NotImplementedException($"Type {type.Kind} not implemented: {type}");
            }
        }

        private static void EmitStoreIndirect(ILGenerator ilGenerator, LLVMTypeRef type, CompilationContext context)
        {
            switch (type.Kind)
            {
                case LLVMTypeKind.LLVMArrayTypeKind:
                case LLVMTypeKind.LLVMStructTypeKind:
                case LLVMTypeKind.LLVMVectorTypeKind:
                    ilGenerator.Emit(OpCodes.Stobj, GetMsilType(type, context));
                    break;

                case LLVMTypeKind.LLVMIntegerTypeKind:
                    ilGenerator.Emit(type.IntWidth switch
                    {
                        8 => OpCodes.Stind_I1,
                        16 => OpCodes.Stind_I2,
                        32 => OpCodes.Stind_I4,
                        64 => OpCodes.Stind_I8,
                        _ => throw new NotImplementedException($"Indirect store not implemented for integer width {type.IntWidth}: {type}")
                    });
                    break;

                case LLVMTypeKind.LLVMPointerTypeKind:
                    ilGenerator.Emit(OpCodes.Stind_I);
                    break;

                default:
                    throw new NotImplementedException($"Unexpected type {type.Kind}: {type}");
            }
        }
    }
}
