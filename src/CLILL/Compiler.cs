using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using LLVMSharp.Interop;

namespace CLILL
{
    public sealed partial class Compiler : IDisposable
    {
        private readonly LLVMContextRef _context;

        public Compiler()
        {
            _context = LLVMContextRef.Create();
        }

        public void Compile(LLVMSourceCode source, string outputName)
        {
            using var module = _context.ParseIR(source.MemoryBuffer);

            var assemblyName = new AssemblyName(outputName);
            var assemblyBuilder = new PersistedAssemblyBuilder(
                assemblyName,
                typeof(object).Assembly);

            var dynamicModule = assemblyBuilder.DefineDynamicModule(
                assemblyName.Name);

            var typeBuilder = dynamicModule.DefineType(
                "MyType",
                TypeAttributes.Public);

            var compilationContext = new CompilationContext(
                module,
                assemblyBuilder,
                typeBuilder);

            CompileGlobals(compilationContext);

            MethodInfo entryPoint = null;

            var function = module.FirstFunction;
            while (function.Handle != IntPtr.Zero)
            {
                var methodInfo = GetOrCreateMethod(function, compilationContext);

                if (methodInfo.Name == "main")
                {
                    entryPoint = methodInfo;
                }

                function = function.NextFunction;
            }

            typeBuilder.CreateType();

            var metadataBuilder = assemblyBuilder.GenerateMetadata(
                out BlobBuilder ilStream, 
                out BlobBuilder fieldData);

            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage);

            var peBuilder = new ManagedPEBuilder(
                header: peHeaderBuilder,
                metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                ilStream: ilStream,
                mappedFieldData: fieldData,
                entryPoint: MetadataTokens.MethodDefinitionHandle(entryPoint.MetadataToken));

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);

            using var fileStream = new FileStream($"{outputName}.exe", FileMode.Create, FileAccess.Write);
            peBlob.WriteContentTo(fileStream);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
