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
        public readonly TypeBuilder TypeBuilder;

        public readonly Dictionary<LLVMValueRef, FieldInfo> Globals = new Dictionary<LLVMValueRef, FieldInfo>();
        public readonly Dictionary<LLVMValueRef, MethodInfo> Functions = new Dictionary<LLVMValueRef, MethodInfo>();

        public CompilationContext(
            LLVMModuleRef llvmModule,
            AssemblyBuilder assemblyBuilder,
            TypeBuilder typeBuilder)
        {
            LLVMModule = llvmModule;
            AssemblyBuilder = assemblyBuilder;
            TypeBuilder = typeBuilder;
        }
    }
}
