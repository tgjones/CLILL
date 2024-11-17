using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using LLVMSharp.Interop;

namespace CLILL
{
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
                    switch (intTypeWidth)
                    {
                        case 1: // TODO: really?
                        case 32:
                            return typeof(int);

                        case 8:
                            return typeof(byte);

                        case 16:
                            return typeof(short);

                        case 64:
                            return typeof(long);

                        default:
                            throw new NotImplementedException($"Integer width {intTypeWidth} not implemented: {typeRef}");
                    }

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
            if (typeRef.ElementType.Kind != LLVMTypeKind.LLVMIntegerTypeKind)
            {
                throw new InvalidOperationException();
            }
            switch ((context.GetSizeOfTypeInBytes(typeRef.ElementType), typeRef.VectorSize))
            {
                case (1, 4):
                    return CreateArrayOrVectorType(typeRef, context, (int)typeRef.VectorSize);

                case (4, 4):
                    return typeof(Vector128<>).MakeGenericType(GetMsilType(typeRef.ElementType, context));

                default:
                    throw new NotImplementedException();
            }
        }

        private static Type CreateStructType(LLVMTypeRef typeRef, CompilationContext context)
        {
            if (typeRef.IsPackedStruct || typeRef.IsOpaqueStruct)
            {
                throw new NotImplementedException();
            }

            var structType = context.ModuleBuilder.DefineType(
                typeRef.StructName,
                TypeAttributes.Public | TypeAttributes.SequentialLayout,
                typeof(ValueType));

            for (var i = 0; i < typeRef.StructElementTypes.Length; i++)
            {
                var structElementTypeRef = typeRef.StructElementTypes[i];
                var field = structType.DefineField(
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

        private static Type CreateArrayType(LLVMTypeRef arrayTypeRef, CompilationContext context)
        {
            return CreateArrayOrVectorType(arrayTypeRef, context, (int)arrayTypeRef.ArrayLength);
        }

        private static Type CreateArrayOrVectorType(
            LLVMTypeRef arrayOrVectorTypeRef, 
            CompilationContext context,
            int length)
        {
            var elementType = GetMsilType(arrayOrVectorTypeRef.ElementType, context);

            if (elementType.IsPointer)
            {
                elementType = typeof(IntPtr);
            }

            var structType = context.ModuleBuilder.DefineType(
                $"Array_{elementType.Name}_{length}",
                TypeAttributes.Public | TypeAttributes.SequentialLayout,
                typeof(ValueType));

            var customAttributeBuilder = new System.Reflection.Emit.CustomAttributeBuilder(
                typeof(InlineArrayAttribute).GetConstructor([typeof(int)]),
                [length]);

            structType.SetCustomAttribute(customAttributeBuilder);

            structType.DefineField(
                $"_element0",
                elementType,
                FieldAttributes.Private);

            return structType.CreateType();
        }
    }
}
