using System;
using System.Collections.Concurrent;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using CLILL.Helpers;
using CLILL.Runtime;
using LLVMSharp.Interop;

namespace CLILL;

internal sealed class TypeSystem
{
    private readonly ConcurrentDictionary<LLVMTypeRef, Type> _structTypes = [];
    private readonly ConcurrentDictionary<(LLVMTypeRef, int), Type> _arrayTypes = [];

    private readonly ConcurrentDictionary<LLVMMetadataRef, ISymbolDocumentWriter> _documents = [];

    private readonly ModuleBuilder _moduleBuilder;
    private readonly LLVMModuleRef _module;

    public TypeSystem(ModuleBuilder moduleBuilder, LLVMModuleRef module)
    {
        _moduleBuilder = moduleBuilder;
        _module = module;
    }

    public Type GetMsilType(LLVMTypeRef typeRef)
    {
        switch (typeRef.Kind)
        {
            case LLVMTypeKind.LLVMArrayTypeKind:
                return GetArrayType(typeRef.ElementType, (int)typeRef.ArrayLength);

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
                return _structTypes.GetOrAdd(typeRef, CreateStructType);

            case LLVMTypeKind.LLVMVectorTypeKind:
                return GetMsilVectorType(typeRef);

            case LLVMTypeKind.LLVMVoidTypeKind:
                return typeof(void);

            default:
                throw new NotImplementedException($"Type {typeRef.Kind} is not implemented");
        }
    }

    public Type GetMsilVectorType(LLVMTypeRef typeRef)
    {
        if (typeRef.Kind != LLVMTypeKind.LLVMVectorTypeKind)
        {
            throw new InvalidOperationException();
        }

        var vectorSize = GetSizeOfTypeInBytes(typeRef);

        return vectorSize switch
        {
            2 or 8 or 16 or 32 or 64 => GetGenericVectorType(typeRef).MakeGenericType(GetMsilVectorElementType(typeRef.ElementType)),
            _ => GetArrayType(typeRef.ElementType, (int)typeRef.VectorSize),
        };
    }

    public Type GetMsilVectorElementType(LLVMTypeRef elementTypeRef)
    {
        var result = GetMsilType(elementTypeRef);

        if (result == typeof(bool))
        {
            result = typeof(byte);
        }

        return result;
    }

    private static int AnonymousStructIndex = 0;

    private Type CreateStructType(LLVMTypeRef typeRef)
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

        var structType = _moduleBuilder.DefineType(
            structName,
            TypeAttributes.Public | TypeAttributes.SequentialLayout,
            typeof(ValueType),
            packingSize);

        for (var i = 0; i < typeRef.StructElementTypes.Length; i++)
        {
            var structElementTypeRef = typeRef.StructElementTypes[i];
            structType.DefineField(
                $"Field{i}",
                GetMsilType(structElementTypeRef),
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

    public Type GetArrayType(LLVMTypeRef elementType, int arrayLength)
    {
        return _arrayTypes.GetOrAdd((elementType, arrayLength), _ => CreateArrayType(elementType, arrayLength));
    }

    private Type CreateArrayType(LLVMTypeRef elementTypeRef, int length)
    {
        var elementType = GetMsilType(elementTypeRef);

        if (elementType.IsPointer)
        {
            elementType = typeof(IntPtr);
        }

        var structType = _moduleBuilder.DefineType(
            $"Array_{elementType.Name}_{length}",
            TypeAttributes.Public | TypeAttributes.SequentialLayout,
            typeof(ValueType));

        // [InlineArray] doesn't allow zero lengths.
        // So when the length is zero, we just create an empty normal struct instead.
        // This will still have the correct size / alignment.
        if (length > 0)
        {
            var customAttributeBuilder = new CustomAttributeBuilder(
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

    public Type GetNonGenericVectorType(LLVMTypeRef vectorType)
    {
        var vectorSizeInBits = vectorType.VectorSize * RoundUpToTypeSize(GetSizeOfTypeInBits(vectorType.ElementType));
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

    public Type GetGenericVectorType(LLVMTypeRef vectorType)
    {
        var vectorSizeInBits = vectorType.VectorSize * RoundUpToTypeSize(GetSizeOfTypeInBits(vectorType.ElementType));
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

    public unsafe int GetSizeOfTypeInBits(LLVMTypeRef type) => (int)LLVM.SizeOfTypeInBits(
        LLVM.GetModuleDataLayout(_module), type);

    public unsafe int GetSizeOfTypeInBytes(LLVMTypeRef type) => GetSizeOfTypeInBits(type) / 8;

    public unsafe int GetStructFieldOffset(LLVMTypeRef structType, uint fieldIndex)
    {
        var targetData = LLVM.CreateTargetData(LLVM.GetDataLayout(_module));

        var fieldOffset = LLVM.OffsetOfElement(
            targetData,
            structType,
            fieldIndex);

        LLVM.DisposeTargetData(targetData);

        return (int)fieldOffset;
    }

    public unsafe ISymbolDocumentWriter GetDocument(LLVMMetadataRef diFile)
    {
        return _documents.GetOrAdd(diFile, _ =>
        {
            var directory = diFile.GetDIFileDirectory();
            var filename = diFile.GetDIFileFilename();

            var fullPath = Path.Combine(directory, filename);

            var diFileValue = (LLVMValueRef)LLVM.MetadataAsValue(_module.Context, diFile);

            var checksum = diFileValue.GetOperand(2).GetMDString(out var _);

            var language = Path.GetExtension(filename) switch
            {
                ".c" or ".h" => SymLanguageType.C,
                ".cpp" => SymLanguageType.CPlusPlus,
                ".cs" => SymLanguageType.CSharp,
                _ => Guid.Empty,
            };

            var result = _moduleBuilder.DefineDocument(fullPath, language);

            var checksumBytes = Convert.FromHexString(checksum);

            // I can't find a way to get the checksumKind using the LLVM C API.
            // So we assume it's CSK_MD5 for now.
            var checksumAlgorithm = new Guid("406EA660-64CF-4C82-B6F0-42D48172A799");

            // TODO: Uncomment when this issue is fixed:
            // https://github.com/dotnet/runtime/issues/110096
            //result.SetCheckSum(checksumAlgorithm, checksumBytes);

            return result;
        });
    }
}
