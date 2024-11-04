using System;
using System.Reflection;
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
                    return GetMsilType(typeRef.ElementType, context).MakePointerType();

                case LLVMTypeKind.LLVMDoubleTypeKind:
                    return typeof(double);

                case LLVMTypeKind.LLVMIntegerTypeKind:
                    var intTypeWidth = typeRef.IntWidth;
                    switch (intTypeWidth)
                    {
                        case 1: // TODO: really?
                        case 32:
                            return typeof(int);

                        case 8:
                            return typeof(byte);

                        case 64:
                            return typeof(long);

                        default:
                            throw new NotImplementedException();
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
            switch (typeRef.VectorSize)
            {
                case 4:
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
    }
}
