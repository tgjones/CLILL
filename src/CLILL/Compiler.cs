using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Versioning;
using CLILL.Helpers;

namespace CLILL;

public sealed partial class Compiler
{
    public static void Compile(string inputPath, string outputPath)
    {
        var compiler = new Compiler(inputPath, outputPath);

        compiler.Compile(out var mainMethod);

        compiler.Save(mainMethod);
    }

    private readonly string _inputPath;
    private readonly string _outputPath;

    private readonly PersistedAssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;

    private Compiler(string inputPath, string outputPath)
    {
        _inputPath = inputPath;
        _outputPath = outputPath;

        var outputName = Path.GetFileNameWithoutExtension(outputPath);

        var assemblyName = new AssemblyName(outputName);

        _assemblyBuilder = new PersistedAssemblyBuilder(
            assemblyName,
            typeof(object).Assembly);

        var targetFrameworkAttributeBuilder = new CustomAttributeBuilder(
            typeof(TargetFrameworkAttribute).GetConstructorStrict([typeof(string)]),
            [".NETCoreApp,Version=v9.0"],
            [typeof(TargetFrameworkAttribute).GetPropertyStrict(nameof(TargetFrameworkAttribute.FrameworkDisplayName))],
            [".NET 9.0"]);
        _assemblyBuilder.SetCustomAttribute(targetFrameworkAttributeBuilder);

        _moduleBuilder = _assemblyBuilder.DefineDynamicModule(outputName);
    }

    private void Compile(out MethodInfo? mainMethod)
    {
        using var moduleCompiler = new ModuleCompiler(_inputPath, _moduleBuilder);

        moduleCompiler.CompileModule(out mainMethod);
    }

    private void Save(MethodInfo? mainMethod)
    {
        var metadataBuilder = _assemblyBuilder.GenerateMetadata(
            out var ilStream,
            out var fieldData,
            out var pdbBuilder);

        var entryPointHandle = mainMethod != null
            ? MetadataTokens.MethodDefinitionHandle(mainMethod.MetadataToken)
            : default;

        var portablePdbBuilder = new PortablePdbBuilder(
            pdbBuilder,
            metadataBuilder.GetRowCounts(),
            entryPointHandle);

        var portablePdbBlob = new BlobBuilder();
        var pdbContentId = portablePdbBuilder.Serialize(portablePdbBlob);

        var pdbOutputPath = Path.ChangeExtension(_outputPath, ".pdb");
        using (var pdbFileStream = new FileStream(pdbOutputPath, FileMode.Create, FileAccess.Write))
        {
            portablePdbBlob.WriteContentTo(pdbFileStream);
        }

        var debugDirectoryBuilder = new DebugDirectoryBuilder();
        debugDirectoryBuilder.AddCodeViewEntry(Path.GetFileName(pdbOutputPath), pdbContentId, portablePdbBuilder.FormatVersion);

        var peHeaderBuilder = new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage);

        var peBuilder = new ManagedPEBuilder(
            header: peHeaderBuilder,
            metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
            ilStream: ilStream,
            mappedFieldData: fieldData,
            debugDirectoryBuilder: debugDirectoryBuilder,
            entryPoint: entryPointHandle);

        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);

        using (var fileStream = new FileStream(_outputPath, FileMode.Create, FileAccess.Write))
        {
            peBlob.WriteContentTo(fileStream);
        }

        // TODO: Make version dynamic.
        File.WriteAllText(
            Path.ChangeExtension(_outputPath, "runtimeconfig.json"),
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
            Path.Combine(Path.GetDirectoryName(_outputPath) ?? "", runtimeDll),
            true);
    }
}