using LLVMSharp.API;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CLILL
{
    public sealed partial class Compiler : IDisposable
    {
        private readonly Context _context;

        public Compiler()
        {
            _context = Context.Create();
        }

        public void Compile(LLVMSourceCode source, string outputName)
        {
            using (var module = _context.ParseIR(source.MemoryBuffer))
            {
                var assemblyName = new AssemblyName(outputName);
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    assemblyName,
                    AssemblyBuilderAccess.RunAndSave);

                var dynamicModule = assemblyBuilder.DefineDynamicModule(
                    assemblyName.Name,
                    $"{assemblyName.Name}.exe");

                var typeBuilder = dynamicModule.DefineType(
                    "MyType",
                    TypeAttributes.Public);

                var compilationContext = new CompilationContext(
                    module,
                    assemblyBuilder,
                    typeBuilder);

                CompileGlobals(compilationContext);

                var function = module.GetFirstFunction();
                while (function != null)
                {
                    GetOrCreateMethod(function, compilationContext);

                    function = function.NextFunction;
                }

                typeBuilder.CreateType();

                assemblyBuilder.Save($"{assemblyName.Name}.exe");
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
