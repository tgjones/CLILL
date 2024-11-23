using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using LLVMSharp.Interop;

namespace CLILL;

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

                    var globalField = context.TypeBuilder.DefineField(
                        global.Name.Replace(".", string.Empty),
                        globalType,
                        FieldAttributes.Private | FieldAttributes.Static);

                    globalField.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            typeof(FixedAddressValueTypeAttribute).GetConstructor([]),
                            []));

                    EmitConstantValue(globalValue, valueType, ilGenerator, context);
                    ilGenerator.Emit(OpCodes.Stsfld, globalField);

                    context.Globals.Add(global, globalField);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void GetConstantIntData(LLVMValueRef value, byte[] dest, int destIndex)
    {
        var sizeInBytes = (int)value.TypeOf.IntWidth / 8;

        switch (sizeInBytes)
        {
            case 1:
                dest[destIndex] = (byte)value.ConstIntZExt;
                break;

            case 2:
                Buffer.BlockCopy(
                    BitConverter.GetBytes((short)value.ConstIntZExt),
                    0,
                    dest,
                    destIndex * sizeInBytes,
                    sizeInBytes);
                break;

            case 4:
                Buffer.BlockCopy(
                    BitConverter.GetBytes((int)value.ConstIntZExt),
                    0,
                    dest,
                    destIndex * sizeInBytes,
                    sizeInBytes);
                break;

            default:
                throw new NotImplementedException($"Int width {value.TypeOf.IntWidth} not implemented: {value}");
        }
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

        switch (arrayOrVectorValue.Kind)
        {
            case LLVMValueKind.LLVMConstantArrayValueKind:
            case LLVMValueKind.LLVMConstantDataArrayValueKind:
            case LLVMValueKind.LLVMConstantDataVectorValueKind:
            case LLVMValueKind.LLVMConstantVectorValueKind:
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
                    EmitConstantValue(elementValue, arrayOrVectorValueType.ElementType, ilGenerator, context);
                    EmitStoreIndirect(ilGenerator, arrayOrVectorValueType.ElementType, context);
                }
                break;
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

        if (structValue.Kind == LLVMValueKind.LLVMConstantStructValueKind)
        {
            var structFields = structType.GetFields();
            for (var i = 0; i < structFields.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldloca, local);
                var elementValue = structValue.GetAggregateElement((uint)i);
                EmitConstantValue(elementValue, elementValue.TypeOf, ilGenerator, context);
                ilGenerator.Emit(OpCodes.Stfld, structFields[i]);
            }
        }

        ilGenerator.Emit(OpCodes.Ldloc, local);
    }

    private void EmitLoadConstantVector(
        ILGenerator ilGenerator,
        CompilationContext context,
        LLVMValueRef vectorValue,
        LLVMTypeRef vectorType)
    {
        var vectorSizeInBytes = context.GetSizeOfTypeInBytes(vectorType);

        switch (vectorSizeInBytes)
        {
            case 8:
            case 16:
            case 32:
            case 64:
                var elementType = GetMsilType(vectorType.ElementType, context);
                for (var i = 0u; i < vectorType.VectorSize; i++)
                {
                    EmitConstantValue(vectorValue.GetAggregateElement(i), vectorType.ElementType, ilGenerator, context);
                }
                var createMethodParameterTypes = new Type[vectorType.VectorSize];
                Array.Fill(createMethodParameterTypes, elementType);
                ilGenerator.Emit(OpCodes.Call, GetNonGenericVectorType(vectorType, context).GetMethod("Create", createMethodParameterTypes));
                break;

            default:
                EmitLoadConstantArrayOrVector(ilGenerator, context, vectorValue, vectorType, (int)vectorType.VectorSize);
                break;
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

            case LLVMTypeKind.LLVMDoubleTypeKind:
                ilGenerator.Emit(OpCodes.Stind_R8);
                break;

            case LLVMTypeKind.LLVMFloatTypeKind:
                ilGenerator.Emit(OpCodes.Stind_R4);
                break;

            case LLVMTypeKind.LLVMIntegerTypeKind:
                ilGenerator.Emit(type.IntWidth switch
                {
                    1 => OpCodes.Stind_I1,
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