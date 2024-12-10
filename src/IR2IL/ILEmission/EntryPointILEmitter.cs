using System;
using System.Diagnostics;
using System.Reflection.Emit;
using IR2IL.Helpers;

namespace IR2IL.ILEmission;

internal sealed class EntryPointILEmitter(CompiledModule compiledModule, ILGenerator ilGenerator, CompiledFunction entryPoint)
    : ILEmitter(compiledModule, ilGenerator)
{
    public void EmitEntryPoint()
    {
        // TODO:

        // nint* argv = (nint*)NativeMemory.Alloc((nuint)args.Length, (nuint)sizeof(nint));
        //
        // for (var i = 0; i < args.Length; i++)
        // {
        //     argv[i] = Marshal.StringToHGlobalAnsi(args[i]);
        // }
        //
        // return main(args.Length, argv);

        //ILGenerator.Emit(OpCodes.Call, typeof(Debugger).GetMethodStrict(nameof(Debugger.Launch)));
        //ILGenerator.Emit(OpCodes.Pop);

        var entryPointMethod = entryPoint.MethodInfo;

        foreach (var parameter in entryPointMethod.GetParameters())
        {
            if (parameter.ParameterType == typeof(int))
            {
                ILGenerator.Emit(OpCodes.Ldc_I4_0);
            }
            else if (parameter.ParameterType == typeof(void*))
            {
                ILGenerator.Emit(OpCodes.Ldc_I4_0);
                ILGenerator.Emit(OpCodes.Conv_U);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        ILGenerator.Emit(OpCodes.Call, entryPointMethod);

        if (entryPointMethod.ReturnType == typeof(void))
        {
            ILGenerator.Emit(OpCodes.Ldc_I4_0);
        }

        ILGenerator.Emit(OpCodes.Ret);
    }
}
