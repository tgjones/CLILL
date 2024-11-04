using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using LLVMSharp.Interop;

namespace CLILL
{
    internal sealed class CompilationContext
    {
        public readonly LLVMModuleRef LLVMModule;

        public readonly AssemblyBuilder AssemblyBuilder;
        public readonly ModuleBuilder ModuleBuilder;
        public readonly TypeBuilder TypeBuilder;

        public readonly Dictionary<LLVMValueRef, FieldInfo> Globals = [];
        public readonly Dictionary<LLVMValueRef, MethodInfo> Functions = [];

        public readonly ConcurrentDictionary<LLVMTypeRef, Type> StructTypes = [];

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
    }
}
