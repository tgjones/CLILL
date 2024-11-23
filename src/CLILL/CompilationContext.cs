using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using LLVMSharp.Interop;

namespace CLILL;

internal sealed class CompilationContext
{
    public readonly LLVMModuleRef LLVMModule;

    public readonly AssemblyBuilder AssemblyBuilder;
    public readonly ModuleBuilder ModuleBuilder;
    public readonly TypeBuilder TypeBuilder;

    public readonly Dictionary<LLVMValueRef, FieldInfo> Globals = [];
    public readonly Dictionary<LLVMValueRef, MethodBuilder> Functions = [];

    public readonly ConcurrentDictionary<LLVMTypeRef, Type> StructTypes = [];
    public readonly ConcurrentDictionary<LLVMTypeRef, Type> ArrayTypes = [];
    public readonly ConcurrentDictionary<(LLVMTypeRef, int), Type> AllocaArrayTypes = [];

    private readonly ConcurrentDictionary<LLVMMetadataRef, ISymbolDocumentWriter> Documents = [];

    public CompilationContext(
        LLVMModuleRef llvmModule,
        AssemblyBuilder assemblyBuilder,
        ModuleBuilder moduleBuilder,
        TypeBuilder typeBuilder)
    {
        LLVMModule = llvmModule;
        AssemblyBuilder = assemblyBuilder;
        ModuleBuilder = moduleBuilder;
        TypeBuilder = typeBuilder;
    }

    public unsafe int GetSizeOfTypeInBytes(LLVMTypeRef type)
    {
        var sizeInBits = (int)LLVM.SizeOfTypeInBits(
            LLVM.GetModuleDataLayout(LLVMModule),
            type);

        return sizeInBits / 8;
    }

    public unsafe ISymbolDocumentWriter DefineDocument(LLVMMetadataRef diFile)
    {
        return Documents.GetOrAdd(diFile, _ =>
        {
            var directory = diFile.GetDIFileDirectory();
            var filename = diFile.GetDIFileFilename();

            var fullPath = Path.Combine(directory, filename);

            var diFileValue = (LLVMValueRef)LLVM.MetadataAsValue(LLVMModule.Context, diFile);

            var checksum = diFileValue.GetOperand(2).GetMDString(out var _);

            var language = Path.GetExtension(filename) switch
            {
                ".c" or ".h" => SymLanguageType.C,
                ".cpp" => SymLanguageType.CPlusPlus,
                ".cs" => SymLanguageType.CSharp,
                _ => Guid.Empty,
            };

            var result = ModuleBuilder.DefineDocument(fullPath, language);

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