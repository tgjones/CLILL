using System.Reflection.Emit;

namespace IR2IL.ILEmission;

internal sealed class GlobalsILEmitter(CompiledModule compiledModule, TypeBuilder typeBuilder, CompiledGlobalVariable[] globalVariables)
    : ILEmitter(compiledModule, typeBuilder.DefineTypeInitializer().GetILGenerator())
{
    public void EmitGlobalVariablesInitializer()
    {
        foreach (var globalVariable in globalVariables)
        {
            EmitConstantValue(globalVariable.Value, globalVariable.Type);
            ILGenerator.Emit(OpCodes.Stsfld, globalVariable.Field);
        }

        ILGenerator.Emit(OpCodes.Ret);
    }
}