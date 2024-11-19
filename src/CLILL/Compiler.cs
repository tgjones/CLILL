using System;
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
        public static void Compile(string inputPath, string outputPath)
        {
            using var compiler = new Compiler(inputPath);

            compiler.Compile(outputPath);
        }

        private readonly LLVMContextRef _context;
        private readonly LLVMModuleRef _module;

        private readonly Queue<(LLVMValueRef, MethodBuilder)> _methodsToCompile = new();

        public Compiler(string inputPath)
        {
            _context = LLVMContextRef.Create();

            using var source = LLVMSourceCode.FromFile(inputPath);

            _module = _context.ParseIR(source.MemoryBuffer);
        }

        private void Compile(string outputPath)
        {
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
                _module,
                assemblyBuilder,
                dynamicModule,
                typeBuilder);

            CompileGlobals(compilationContext);

            MethodInfo entryPoint = null;

            var function = _module.FirstFunction;
            while (function.Handle != IntPtr.Zero)
            {
                if (function.Name.StartsWith("llvm."))
                {
                    function = function.NextFunction;
                    continue;
                }

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

            var mainMethod = entryPoint != null
                ? CreateMainMethod(typeBuilder, entryPoint)
                : null;

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
                entryPoint: mainMethod != null ? MetadataTokens.MethodDefinitionHandle(mainMethod.MetadataToken) : default);

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

            var runtimeDll = "CLILL.Runtime.dll";
            File.Copy(
                runtimeDll, 
                Path.Combine(Path.GetDirectoryName(outputPath), runtimeDll),
                true);
        }

        private static MethodBuilder CreateMainMethod(TypeBuilder typeBuilder, MethodInfo entryPoint)
        {
            var method = typeBuilder.DefineMethod(
                "Main",
                MethodAttributes.Static | MethodAttributes.Public,
                CallingConventions.Standard,
                typeof(int),
                [typeof(string[])]);

            var ilGenerator = method.GetILGenerator();

            // TODO:

            // nint* argv = (nint*)NativeMemory.Alloc((nuint)args.Length, (nuint)sizeof(nint));
            // 
            // for (var i = 0; i < args.Length; i++)
            // {
            //     argv[i] = Marshal.StringToHGlobalAnsi(args[i]);
            // }
            // 
            // return main(args.Length, argv);

            foreach (var parameter in entryPoint.GetParameters())
            {
                if (parameter.ParameterType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                }
                else if (parameter.ParameterType == typeof(void*))
                {
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.Emit(OpCodes.Conv_U);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            ilGenerator.Emit(OpCodes.Call, entryPoint);

            if (entryPoint.ReturnType == typeof(void))
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }

            ilGenerator.Emit(OpCodes.Ret);

            return method;
        }

        public void Dispose()
        {
            _module.Dispose();
            _context.Dispose();
        }
    }
}
