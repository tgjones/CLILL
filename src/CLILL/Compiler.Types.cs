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

            var vectorSize = context.GetSizeOfTypeInBytes(typeRef);

            return vectorSize switch
            {
                8 or 16 or 32 or 64 => GetGenericVectorType(typeRef, context).MakeGenericType(GetMsilType(typeRef.ElementType, context)),
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
                ? System.Reflection.Emit.PackingSize.Size1
                : System.Reflection.Emit.PackingSize.Unspecified;

            var structType = context.ModuleBuilder.DefineType(
                structName,
                TypeAttributes.Public | TypeAttributes.SequentialLayout,
                typeof(ValueType),
                packingSize);

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
                    typeof(InlineArrayAttribute).GetConstructor([typeof(int)]),
                    [length]);

                structType.SetCustomAttribute(customAttributeBuilder);

                structType.DefineField(
                    $"_element0",
                    elementType,
                    FieldAttributes.Private);
            }

            return structType.CreateType();
        }

        private static Type GetNonGenericVectorType(LLVMTypeRef vectorType, CompilationContext context)
        {
            var vectorSize = context.GetSizeOfTypeInBytes(vectorType);

            return vectorSize switch
            {
                8 => typeof(Vector64),
                16 => typeof(Vector128),
                32 => typeof(Vector256),
                64 => typeof(Vector512),
                _ => throw new NotImplementedException()
            };
        }

        private static Type GetNonGenericDoubleWidthVectorType(LLVMTypeRef vectorType, CompilationContext context)
        {
            var vectorSize = context.GetSizeOfTypeInBytes(vectorType);

            return vectorSize switch
            {
                8 => typeof(Vector128),
                16 => typeof(Vector256),
                32 => typeof(Vector512),
                _ => throw new NotImplementedException()
            };
        }

        private static Type GetGenericVectorType(LLVMTypeRef vectorType, CompilationContext context)
        {
            var vectorSize = context.GetSizeOfTypeInBytes(vectorType);

            return vectorSize switch
            {
                8 => typeof(Vector64<>),
                16 => typeof(Vector128<>),
                32 => typeof(Vector256<>),
                64 => typeof(Vector512<>),
                _ => throw new NotImplementedException($"Vector size {vectorSize} not implemented")
            };
        }

        private static Type GetGenericDoubleWidthVectorType(LLVMTypeRef vectorType, CompilationContext context)
        {
            var vectorSize = context.GetSizeOfTypeInBytes(vectorType);

            return vectorSize switch
            {
                8 => typeof(Vector128<>),
                16 => typeof(Vector256<>),
                32 => typeof(Vector512<>),
                _ => throw new NotImplementedException($"Vector size {vectorSize} not implemented")
            };
        }
    }
}
