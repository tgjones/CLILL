using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using IR2IL.Helpers;
using IR2IL.ILEmission;
using LLVMSharp.Interop;

namespace IR2IL;

internal sealed class ModuleCompiler : IDisposable
{
    private readonly LLVMContextRef _context;
    private readonly LLVMModuleRef _module;

    private readonly TypeSystem _typeSystem;

    private readonly ModuleBuilder _moduleBuilder;
    private readonly TypeBuilder _typeBuilder;

    public ModuleCompiler(string inputPath, ModuleBuilder moduleBuilder)
    {
        _context = LLVMContextRef.Create();

        using var source = LLVMSourceCode.FromFile(inputPath);

        _module = _context.ParseIR(source.MemoryBuffer);

        _typeSystem = new TypeSystem(moduleBuilder, _module);

        _moduleBuilder = moduleBuilder;

        _typeBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public,
            typeof(ValueType));
    }

    public void CompileModule(out MethodInfo? mainMethod)
    {
        var compiledGlobalVariables = CompileGlobals();
        var compiledFunctions = CompileFunctions(out var entryPoint);

        var compiledModule = new CompiledModule(
            _typeSystem,
            compiledGlobalVariables,
            compiledFunctions);

        var ilEmitter = new GlobalsILEmitter(compiledModule, _typeBuilder, compiledGlobalVariables);
        ilEmitter.EmitGlobalVariablesInitializer();

        foreach (var function in compiledFunctions)
        {
            if (function is CompiledFunctionDefinition functionDefinition)
            {
                var functionCompiler = new FunctionILEmitter(compiledModule, functionDefinition);
                functionCompiler.Compile();
            }
        }

        mainMethod = entryPoint != null
            ? CreateMainMethod(compiledModule, entryPoint)
            : null;

        _typeBuilder.CreateType();
    }

    private unsafe CompiledGlobalVariable[] CompileGlobals()
    {
        var globals = _module.GetGlobals().ToList();

        var result = new List<CompiledGlobalVariable>();

        foreach (var global in globals)
        {
            switch (global.Kind)
            {
                case LLVMValueKind.LLVMGlobalVariableValueKind:
                    var valueType = (LLVMTypeRef)LLVM.GlobalGetValueType(global);
                    var globalValue = global.GetOperand(0);

                    var globalType = _typeSystem.GetMsilType(valueType);

                    var globalField = _typeBuilder.DefineField(
                        global.Name.Replace(".", string.Empty),
                        globalType,
                        FieldAttributes.Private | FieldAttributes.Static);

                    globalField.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            typeof(FixedAddressValueTypeAttribute).GetConstructorStrict([]),
                            []));

                    result.Add(new CompiledGlobalVariable(global, valueType, globalValue, globalField));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        return result.ToArray();
    }

    private CompiledFunction[] CompileFunctions(out CompiledFunction? entryPoint)
    {
        var result = new List<CompiledFunction>();

        entryPoint = null;

        var function = _module.FirstFunction;
        while (function.Handle != IntPtr.Zero)
        {
            if (function.IntrinsicID != 0)
            {
                function = function.NextFunction;
                continue;
            }

            var compiledFunction = CompileFunction(function);

            result.Add(compiledFunction);

            if (function.Name == "main")
            {
                entryPoint = compiledFunction;
            }

            function = function.NextFunction;
        }

        return result.ToArray();
    }

    internal unsafe CompiledFunction CompileFunction(LLVMValueRef function)
    {
        if (function.IsDeclaration)
        {
            return new CompiledFunction(function, CreateMethodDeclaration(function));
        }
        else
        {
            return new CompiledFunctionDefinition(function, CompileMethod(function));
        }
    }

    private unsafe MethodInfo CreateMethodDeclaration(LLVMValueRef function)
    {
        // TODO: We assume all extern function are part of C runtime.
        // That isn't generally true...

        var functionType = (LLVMTypeRef)LLVM.GlobalGetValueType(function);

        return CreateExternMethod(
            function.Name,
            functionType.IsFunctionVarArg ? CallingConventions.VarArgs : CallingConventions.Standard,
            _typeSystem.GetMsilType(functionType.ReturnType),
            functionType.ParamTypes.Select(x => _typeSystem.GetMsilType(x)).ToArray());
    }

    private unsafe MethodBuilder CompileMethod(LLVMValueRef function)
    {
        var functionType = (LLVMTypeRef)LLVM.GlobalGetValueType(function);

        var parameters = function.Params;
        var parameterTypes = new Type[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameterTypes[i] = _typeSystem.GetMsilType(parameters[i].TypeOf);
        }

        var result = _typeBuilder.DefineMethod(
            function.Name,
            MethodAttributes.Static | MethodAttributes.Public, // TODO
            functionType.IsFunctionVarArg ? CallingConventions.VarArgs : CallingConventions.Standard,
            _typeSystem.GetMsilType(functionType.ReturnType),
            parameterTypes);

        var skipLocalsInitAttribute = new CustomAttributeBuilder(
            typeof(SkipLocalsInitAttribute).GetConstructorStrict([]),
            []);
        result.SetCustomAttribute(skipLocalsInitAttribute);

        return result;
    }

    private MethodBuilder CreateExternMethod(
        string name,
        CallingConventions callingConventions,
        Type returnType,
        Type[] parameterTypes)
    {
        var methodInfo = _typeBuilder.DefinePInvokeMethod(
            name,
            "ucrtbase.dll",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            callingConventions,
            returnType,
            parameterTypes,
            CallingConvention.Winapi,
            CharSet.None);

        methodInfo.SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.Managed | MethodImplAttributes.PreserveSig);

        return methodInfo;
    }

    private MethodBuilder CreateMainMethod(CompiledModule compiledModule, CompiledFunction entryPoint)
    {
        var method = _moduleBuilder.DefineGlobalMethod(
            "Main",
            MethodAttributes.Static | MethodAttributes.Public,
            CallingConventions.Standard,
            typeof(int),
            [typeof(string[])]);

        method.DefineParameter(1, ParameterAttributes.None, "args");

        var entryPointILEmitter = new EntryPointILEmitter(compiledModule, method.GetILGenerator(), entryPoint);
        entryPointILEmitter.EmitEntryPoint();

        return method;
    }

    public void Dispose()
    {
        _module.Dispose();
        _context.Dispose();
    }
}
