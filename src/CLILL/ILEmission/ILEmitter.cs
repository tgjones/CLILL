using System;
using System.Reflection.Emit;
using System.Runtime.Intrinsics;
using CLILL.Helpers;
using LLVMSharp.Interop;

namespace CLILL.ILEmission;

internal abstract class ILEmitter
{
    protected CompiledModule CompiledModule { get; }
    protected TypeSystem TypeSystem { get; }
    protected ILGenerator ILGenerator { get; }

    public ILEmitter(CompiledModule compiledModule, ILGenerator ilGenerator)
    {
        CompiledModule = compiledModule;
        TypeSystem = compiledModule.TypeSystem;
        ILGenerator = ilGenerator;
    }

    protected void EmitConstantValue(LLVMValueRef valueRef, LLVMTypeRef valueTypeRef)
    {
        switch (valueRef.Kind)
        {
            case LLVMValueKind.LLVMConstantAggregateZeroValueKind:
                switch (valueTypeRef.Kind)
                {
                    case LLVMTypeKind.LLVMArrayTypeKind:
                        EmitLoadConstantArray(valueRef, valueTypeRef);
                        break;

                    case LLVMTypeKind.LLVMStructTypeKind:
                        EmitLoadConstantStruct(valueRef, TypeSystem.GetMsilType(valueTypeRef));
                        break;

                    case LLVMTypeKind.LLVMVectorTypeKind:
                        ILGenerator.Emit(OpCodes.Call, TypeSystem.GetMsilVectorType(valueTypeRef).GetMethodStrict("get_Zero"));
                        break;

                    default:
                        throw new NotImplementedException($"Constant aggregate zero value {valueTypeRef.Kind} not implemented: {valueRef}");
                }
                break;

            case LLVMValueKind.LLVMConstantDataArrayValueKind:
            case LLVMValueKind.LLVMConstantArrayValueKind:
                EmitLoadConstantArray(valueRef, valueTypeRef);
                break;

            case LLVMValueKind.LLVMConstantDataVectorValueKind:
            case LLVMValueKind.LLVMConstantVectorValueKind:
                EmitLoadConstantVector(valueRef, valueTypeRef);
                break;

            case LLVMValueKind.LLVMConstantFPValueKind:
                switch (valueTypeRef.Kind)
                {
                    case LLVMTypeKind.LLVMDoubleTypeKind:
                        ILGenerator.Emit(OpCodes.Ldc_R8, valueRef.GetConstRealDouble(out var losesInfo));
                        if (losesInfo)
                        {
                            throw new InvalidOperationException();
                        }
                        break;

                    case LLVMTypeKind.LLVMFloatTypeKind:
                        ILGenerator.Emit(OpCodes.Ldc_R4, (float)valueRef.GetConstRealDouble(out var losesInfo2));
                        if (losesInfo2)
                        {
                            throw new InvalidOperationException();
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
                break;

            case LLVMValueKind.LLVMConstantIntValueKind:
                switch (valueTypeRef.IntWidth)
                {
                    case 1:
                    case 8:
                    case 16:
                    case 32:
                        ILGenerator.Emit(OpCodes.Ldc_I4, (int)valueRef.ConstIntZExt);
                        break;

                    case 64:
                        ILGenerator.Emit(OpCodes.Ldc_I8, valueRef.ConstIntSExt);
                        break;

                    default:
                        throw new NotImplementedException($"Load constant integer width {valueTypeRef.IntWidth} not implemented: {valueRef}");
                }
                break;

            case LLVMValueKind.LLVMConstantExprValueKind:
                switch (valueRef.ConstOpcode)
                {
                    case LLVMOpcode.LLVMGetElementPtr:
                        EmitConstantValue(valueRef.GetOperand(0), valueRef.GetOperand(0).TypeOf);
                        ILGenerator.Emit(OpCodes.Ldc_I4, GetElementPtrConst(valueRef));
                        ILGenerator.Emit(OpCodes.Conv_U);
                        ILGenerator.Emit(OpCodes.Add);
                        break;

                    default:
                        throw new NotImplementedException($"Const opcode {valueRef.ConstOpcode} not implemented: {valueRef}");
                }
                break;

            case LLVMValueKind.LLVMConstantPointerNullValueKind:
                ILGenerator.Emit(OpCodes.Ldc_I4_0);
                ILGenerator.Emit(OpCodes.Conv_U);
                break;

            case LLVMValueKind.LLVMConstantStructValueKind:
                EmitLoadConstantStruct(valueRef, TypeSystem.GetMsilType(valueTypeRef));
                break;

            case LLVMValueKind.LLVMFunctionValueKind:
                ILGenerator.Emit(OpCodes.Ldftn, CompiledModule.GetFunction(valueRef));
                break;

            case LLVMValueKind.LLVMGlobalVariableValueKind:
                var staticField = CompiledModule.GetGlobal(valueRef);
                ILGenerator.Emit(OpCodes.Ldsflda, staticField);
                break;

            case LLVMValueKind.LLVMPoisonValueValueKind:
            case LLVMValueKind.LLVMUndefValueValueKind:
                switch (valueTypeRef.Kind)
                {
                    case LLVMTypeKind.LLVMArrayTypeKind:
                        EmitLoadConstantArray(valueRef, valueTypeRef);
                        break;

                    case LLVMTypeKind.LLVMFloatTypeKind:
                        ILGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
                        break;

                    case LLVMTypeKind.LLVMIntegerTypeKind:
                        switch (valueTypeRef.IntWidth)
                        {
                            case 32:
                                ILGenerator.Emit(OpCodes.Ldc_I4_0);
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case LLVMTypeKind.LLVMPointerTypeKind:
                        ILGenerator.Emit(OpCodes.Ldc_I4_0);
                        ILGenerator.Emit(OpCodes.Conv_U);
                        break;

                    case LLVMTypeKind.LLVMStructTypeKind:
                        EmitLoadConstantStruct(valueRef, TypeSystem.GetMsilType(valueTypeRef));
                        break;

                    case LLVMTypeKind.LLVMVectorTypeKind:
                        ILGenerator.Emit(OpCodes.Call, TypeSystem.GetMsilVectorType(valueTypeRef).GetMethodStrict("get_Zero"));
                        break;

                    default:
                        throw new NotImplementedException($"Unsupported poison / undef value type kind {valueTypeRef.Kind}: {valueRef}");
                }
                break;

            default:
                throw new NotImplementedException($"Unsupported value kind {valueRef.Kind}: {valueRef}");
        }
    }

    private unsafe int GetElementPtrConst(LLVMValueRef constExpr)
    {
        var sourceElementType = (LLVMTypeRef)LLVM.GetGEPSourceElementType(constExpr);
        var currentType = sourceElementType;

        var result = 0;

        if (constExpr.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
        {
            throw new NotImplementedException();
        }

        var sizeInBytes = (long)TypeSystem.GetSizeOfTypeInBytes(currentType);
        result += (int)(constExpr.GetOperand(1).ConstIntSExt * sizeInBytes);

        for (var i = 2u; i < constExpr.OperandCount; i++)
        {
            var index = constExpr.GetOperand(i);

            if (index.Kind != LLVMValueKind.LLVMConstantIntValueKind)
            {
                throw new NotImplementedException();
            }

            switch (currentType.Kind)
            {
                case LLVMTypeKind.LLVMArrayTypeKind:
                    result += (int)index.ConstIntSExt * TypeSystem.GetSizeOfTypeInBytes(currentType.ElementType);
                    currentType = currentType.ElementType;
                    break;

                case LLVMTypeKind.LLVMIntegerTypeKind:
                    result += (int)index.ConstIntSExt;
                    break;

                case LLVMTypeKind.LLVMStructTypeKind:
                    var fieldIndex = (uint)index.ConstIntSExt;
                    result += TypeSystem.GetStructFieldOffset(currentType, fieldIndex);
                    currentType = currentType.StructGetTypeAtIndex(fieldIndex);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        return result;
    }

    private void EmitLoadConstantVector(LLVMValueRef vectorValue, LLVMTypeRef vectorType)
    {
        var vectorSizeInBytes = TypeSystem.GetSizeOfTypeInBytes(vectorType);

        switch (vectorSizeInBytes)
        {
            case 8:
            case 16:
            case 32:
            case 64:
                var elementType = TypeSystem.GetMsilType(vectorType.ElementType);
                for (var i = 0u; i < vectorType.VectorSize; i++)
                {
                    EmitConstantValue(vectorValue.GetAggregateElement(i), vectorType.ElementType);
                }
                var createMethodParameterTypes = new Type[vectorType.VectorSize];
                Array.Fill(createMethodParameterTypes, elementType);
                ILGenerator.Emit(OpCodes.Call, TypeSystem.GetNonGenericVectorType(vectorType).GetMethodStrict(nameof(Vector128.Create), createMethodParameterTypes));
                break;

            default:
                EmitLoadConstantArrayOrVector(vectorValue, vectorType, (int)vectorType.VectorSize);
                break;
        }
    }

    private void EmitLoadConstantArray(LLVMValueRef arrayValue, LLVMTypeRef arrayValueType)
    {
        EmitLoadConstantArrayOrVector(
            arrayValue,
            arrayValueType,
            (int)arrayValueType.ArrayLength);
    }

    private void EmitLoadConstantArrayOrVector(
        LLVMValueRef arrayOrVectorValue,
        LLVMTypeRef arrayOrVectorValueType,
        int length)
    {
        var msilType = TypeSystem.GetMsilType(arrayOrVectorValueType);

        var local = ILGenerator.DeclareLocal(msilType);

        ILGenerator.Emit(OpCodes.Ldloca, local);
        ILGenerator.Emit(OpCodes.Initobj, msilType);

        switch (arrayOrVectorValue.Kind)
        {
            case LLVMValueKind.LLVMConstantArrayValueKind:
            case LLVMValueKind.LLVMConstantDataArrayValueKind:
            case LLVMValueKind.LLVMConstantDataVectorValueKind:
            case LLVMValueKind.LLVMConstantVectorValueKind:
                var elementSizeInBytes = TypeSystem.GetSizeOfTypeInBytes(arrayOrVectorValueType.ElementType);
                for (var i = 0; i < length; i++)
                {
                    ILGenerator.Emit(OpCodes.Ldloca, local);
                    if (i > 0)
                    {
                        ILGenerator.Emit(OpCodes.Ldc_I4, i * elementSizeInBytes);
                        ILGenerator.Emit(OpCodes.Conv_U);
                        ILGenerator.Emit(OpCodes.Add);
                    }
                    var elementValue = arrayOrVectorValue.GetAggregateElement((uint)i);
                    EmitConstantValue(elementValue, arrayOrVectorValueType.ElementType);
                    EmitStoreIndirect(arrayOrVectorValueType.ElementType);
                }
                break;
        }

        ILGenerator.Emit(OpCodes.Ldloc, local);
    }

    private void EmitLoadConstantStruct(LLVMValueRef structValue, Type structType)
    {
        var local = ILGenerator.DeclareLocal(structType);

        ILGenerator.Emit(OpCodes.Ldloca, local);
        ILGenerator.Emit(OpCodes.Initobj, structType);

        if (structValue.Kind == LLVMValueKind.LLVMConstantStructValueKind)
        {
            var structFields = structType.GetFields();
            for (var i = 0; i < structFields.Length; i++)
            {
                ILGenerator.Emit(OpCodes.Ldloca, local);
                var elementValue = structValue.GetAggregateElement((uint)i);
                EmitConstantValue(elementValue, elementValue.TypeOf);
                ILGenerator.Emit(OpCodes.Stfld, structFields[i]);
            }
        }

        ILGenerator.Emit(OpCodes.Ldloc, local);
    }

    protected void EmitStoreIndirect(LLVMTypeRef type)
    {
        switch (type.Kind)
        {
            case LLVMTypeKind.LLVMArrayTypeKind:
            case LLVMTypeKind.LLVMStructTypeKind:
            case LLVMTypeKind.LLVMVectorTypeKind:
                ILGenerator.Emit(OpCodes.Stobj, TypeSystem.GetMsilType(type));
                break;

            case LLVMTypeKind.LLVMDoubleTypeKind:
                ILGenerator.Emit(OpCodes.Stind_R8);
                break;

            case LLVMTypeKind.LLVMFloatTypeKind:
                ILGenerator.Emit(OpCodes.Stind_R4);
                break;

            case LLVMTypeKind.LLVMIntegerTypeKind:
                ILGenerator.Emit(type.IntWidth switch
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
                ILGenerator.Emit(OpCodes.Stind_I);
                break;

            default:
                throw new NotImplementedException($"Unexpected type {type.Kind}: {type}");
        }
    }
}
