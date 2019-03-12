using LLVMSharp.API;
using LLVMSharp.API.Values.Constants.GlobalValues.GlobalObjects;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CLILL
{
    internal sealed class CompilationContext
    {
        public readonly LLVMSharp.API.Module LLVMModule;

        public readonly AssemblyBuilder AssemblyBuilder;
        public readonly TypeBuilder TypeBuilder;

        public readonly Dictionary<Value, FieldInfo> Globals = new Dictionary<Value, FieldInfo>();
        public readonly Dictionary<Function, MethodInfo> Functions = new Dictionary<Function, MethodInfo>();

        public CompilationContext(
            LLVMSharp.API.Module llvmModule,
            AssemblyBuilder assemblyBuilder,
            TypeBuilder typeBuilder)
        {
            LLVMModule = llvmModule;
            AssemblyBuilder = assemblyBuilder;
            TypeBuilder = typeBuilder;
        }
    }
}
