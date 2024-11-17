using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Versioning;
using LLVMSharp.Interop;

namespace CLILL
{
    public sealed partial class Compiler : IDisposable
    {
        private readonly LLVMContextRef _context;

        private readonly Queue<(LLVMValueRef, MethodBuilder)> _methodsToCompile = new();

        public Compiler()
        {
            _context = LLVMContextRef.Create();
        }

        public void Compile(LLVMSourceCode source, string outputPath)
        {
            using var module = _context.ParseIR(source.MemoryBuffer);

            var outputName = Path.GetFileNameWithoutExtension(outputPath);

            var assemblyName = new AssemblyName(outputName);

            var assemblyBuilder = new PersistedAssemblyBuilder(
                assemblyName,
                typeof(object).Assembly);

            var targetFrameworkAttributeBuilder = new CustomAttributeBuilder(
                typeof(TargetFrameworkAttribute).GetConstructor([ typeof(string) ]),
                [ ".NETCoreApp,Version=v9.0" ],
                [ typeof(TargetFrameworkAttribute).GetProperty("FrameworkDisplayName") ],
                [ ".NET 9.0" ]);
            assemblyBuilder.SetCustomAttribute(targetFrameworkAttributeBuilder);

            var dynamicModule = assemblyBuilder.DefineDynamicModule(
                assemblyName.Name);

            var typeBuilder = dynamicModule.DefineType(
                "MyType",
                TypeAttributes.Public);

            var compilationContext = new CompilationContext(
                module,
                assemblyBuilder,
                dynamicModule,
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

            while (_methodsToCompile.Count > 0)
            {
                var (functionToCompile, methodToCompile) = _methodsToCompile.Dequeue();

                // TODO: Better way to know we've already compiled the method body.
                if (methodToCompile.GetILGenerator().ILOffset != 0)
                {
                    continue;
                }

                CompileMethodBody(functionToCompile, methodToCompile, compilationContext);
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
                entryPoint: entryPoint != null ? MetadataTokens.MethodDefinitionHandle(entryPoint.MetadataToken) : default);

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);

            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            peBlob.WriteContentTo(fileStream);

            // TODO: Make version dynamic.
            File.WriteAllText(
                Path.ChangeExtension(outputPath, "runtimeconfig.json"),
                """
                {
                  "runtimeOptions": {
                    "tfm": "net9.0",
                    "framework": {
                      "name": "Microsoft.NETCore.App",
                      "version": "9.0.0-rc.2.24473.5"
                    },
                    "configProperties": {
                      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false
                    }
                  }
                }
                """);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
