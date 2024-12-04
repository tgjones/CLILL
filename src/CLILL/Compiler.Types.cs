using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using CLILL.Helpers;
using CLILL.Runtime;
using LLVMSharp.Interop;

namespace CLILL;

partial class Compiler
{
    private static Type GetMsilType(LLVMTypeRef typeRef, CompilationContext context)
    {
        switch (typeRef.Kind)
        {
            case LLVMTypeKind.LLVMArrayTypeKind:
                return context.ArrayTypes.GetOrAdd(typeRef, x => CreateArrayType(x, context));

            case LLVMTypeKind.LLVMDoubleTypeKind:
                return typeof(double);

            case LLVMTypeKind.LLVMFloatTypeKind:
                return typeof(float);

            case LLVMTypeKind.LLVMIntegerTypeKind:
                var intTypeWidth = typeRef.IntWidth;
                return intTypeWidth switch
                {
                    1 => typeof(bool),
                    32 => typeof(int),
                    8 => typeof(byte),
                    16 => typeof(short),
                    64 => typeof(long),
                    _ => throw new NotImplementedException($"Integer width {intTypeWidth} not implemented: {typeRef}"),
                };

            case LLVMTypeKind.LLVMPointerTypeKind:
                return typeof(void*);

            case LLVMTypeKind.LLVMStructTypeKind:
                return context.StructTypes.GetOrAdd(typeRef, x => CreateStructType(x, context));

            case LLVMTypeKind.LLVMVectorTypeKind:
                return GetMsilVectorType(typeRef, context);

            case LLVMTypeKind.LLVMVoidTypeKind:
                return typeof(void);

            default:
                throw new NotImplementedException($"Type {typeRef.Kind} is not implemented");
        }
    }

    private static Type GetMsilVectorType(LLVMTypeRef typeRef, CompilationContext context)
    {
        if (typeRef.Kind != LLVMTypeKind.LLVMVectorTypeKind)
        {
            throw new InvalidOperationException();
        }

        var vectorSize = context.GetSizeOfTypeInBytes(typeRef);

        return vectorSize switch
        {
            2 or 8 or 16 or 32 or 64 => GetGenericVectorType(typeRef, context).MakeGenericType(GetMsilType(typeRef.ElementType, context)),
            _ => CreateArrayOrVectorType(typeRef.ElementType, context, (int)typeRef.VectorSize),
        };
    }

    private static int AnonymousStructIndex = 0;

    private static Type CreateStructType(LLVMTypeRef typeRef, CompilationContext context)
    {
        if (typeRef.IsOpaqueStruct)
        {
            throw new NotImplementedException();
        }

        var structName = typeRef.StructName;
        if (string.IsNullOrEmpty(structName))
        {
            structName = $"AnonymousStruct{AnonymousStructIndex++}";
        }

        var packingSize = typeRef.IsPackedStruct
            ? PackingSize.Size1
            : PackingSize.Unspecified;

        var structType = context.ModuleBuilder.DefineType(
            structName,
            TypeAttributes.Public | TypeAttributes.SequentialLayout,
            typeof(ValueType),
            packingSize);

        for (var i = 0; i < typeRef.StructElementTypes.Length; i++)
        {
            var structElementTypeRef = typeRef.StructElementTypes[i];
            structType.DefineField(
                $"Field{i}",
                GetMsilType(structElementTypeRef, context),
                FieldAttributes.Public);
        }

        var builtType = structType.CreateType();

        //var size = Marshal.SizeOf(builtType);

        //if (context.GetSizeOfTypeInBytes(typeRef) != size)
        //{
        //    throw new InvalidOperationException();
        //}

        return builtType;
    }

    private static Type GetAllocaArrayType(LLVMTypeRef elementType, int arrayLength, CompilationContext context)
    {
        return context.AllocaArrayTypes.GetOrAdd((elementType, arrayLength), _ => CreateArrayOrVectorType(elementType, context, arrayLength));
    }

    private static Type CreateArrayType(LLVMTypeRef arrayTypeRef, CompilationContext context)
    {
        return CreateArrayOrVectorType(arrayTypeRef.ElementType, context, (int)arrayTypeRef.ArrayLength);
    }

    private static Type CreateArrayOrVectorType(
        LLVMTypeRef elementTypeRef,
        CompilationContext context,
        int length)
    {
        var elementType = GetMsilType(elementTypeRef, context);

        if (elementType.IsPointer)
        {
            elementType = typeof(IntPtr);
        }

        var structType = context.ModuleBuilder.DefineType(
            $"Array_{elementType.Name}_{length}",
            TypeAttributes.Public | TypeAttributes.SequentialLayout,
            typeof(ValueType));

        // [InlineArray] doesn't allow zero lengths.
        // So when the length is zero, we just create an empty normal struct instead.
        // This will still have the correct size / alignment.
        if (length > 0)
        {
            var customAttributeBuilder = new System.Reflection.Emit.CustomAttributeBuilder(
                typeof(InlineArrayAttribute).GetConstructorStrict([typeof(int)]),
                [length]);

            structType.SetCustomAttribute(customAttributeBuilder);

            structType.DefineField(
                $"_element0",
                elementType,
                FieldAttributes.Private);
        }

        var zeroPropertyGetter = structType.DefineMethod(
            "get_Zero",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
            CallingConventions.Standard,
            structType,
            []);

        var zeroPropertyGetterILGenerator = zeroPropertyGetter.GetILGenerator();
        var local = zeroPropertyGetterILGenerator.DeclareLocal(structType);
        zeroPropertyGetterILGenerator.Emit(OpCodes.Ldloca_S, local);
        zeroPropertyGetterILGenerator.Emit(OpCodes.Initobj, structType);
        zeroPropertyGetterILGenerator.Emit(OpCodes.Ldloc_0);
        zeroPropertyGetterILGenerator.Emit(OpCodes.Ret);

        var zeroProperty = structType.DefineProperty(
            "Zero",
            PropertyAttributes.None,
            structType,
            []);

        zeroProperty.SetGetMethod(zeroPropertyGetter);

        return structType.CreateType();
    }

    private static Type GetNonGenericVectorType(LLVMTypeRef vectorType, CompilationContext context)
    {
        var vectorSizeInBits = vectorType.VectorSize * RoundUpToTypeSize(context.GetSizeOfTypeInBits(vectorType.ElementType));
        if (vectorSizeInBits > MaxVectorSize)
        {
            throw new NotImplementedException();
        }

        return vectorSizeInBits switch
        {
            16 => typeof(Vector16),
            32 => typeof(Vector32),
            64 => typeof(Vector64),
            128 => typeof(Vector128),
            256 => typeof(Vector256),
            512 => typeof(Vector512),
            _ => throw new NotImplementedException($"Vector size {vectorSizeInBits} not implemented: {vectorType}")
        };
    }

    private static Type GetGenericVectorType(LLVMTypeRef vectorType, CompilationContext context)
    {
        var vectorSizeInBits = vectorType.VectorSize * RoundUpToTypeSize(context.GetSizeOfTypeInBits(vectorType.ElementType));
        if (vectorSizeInBits > MaxVectorSize)
        {
            throw new NotImplementedException();
        }

        return vectorSizeInBits switch
        {
            16 => typeof(Vector16<>),
            64 => typeof(Vector64<>),
            128 => typeof(Vector128<>),
            256 => typeof(Vector256<>),
            512 => typeof(Vector512<>),
            _ => throw new NotImplementedException($"Vector size {vectorSizeInBits} not implemented: {vectorType}")
        };
    }

    private const int MaxVectorSize = 512;

    private static int RoundUpToTypeSize(int sizeInBits)
    {
        if (sizeInBits > 512)
        {
            throw new NotImplementedException();
        }
        else if (sizeInBits > 256)
        {
            return 512;
        }
        else if (sizeInBits > 128)
        {
            return 256;
        }
        else if (sizeInBits > 64)
        {
            return 128;
        }
        else if (sizeInBits > 32)
        {
            return 64;
        }
        else if (sizeInBits > 16)
        {
            return 32;
        }
        else if (sizeInBits > 8)
        {
            return 16;
        }
        else
        {
            return 8;
        }
    }
}