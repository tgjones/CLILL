using System.Reflection.Emit;

namespace CLILL.Intrinsics;

internal sealed class LLVMVaStartIntrinsicFunction : IntrinsicFunction
{
    public override void BuildCall(IntrinsicFunctionCallContext context)
    {
        context.EmitValue(context.Operands[0]);

        context.ILGenerator.Emit(OpCodes.Arglist);
        context.ILGenerator.Emit(OpCodes.Conv_U);

        // This is rather specific to the layout used by CoreCLR for varargs.
        // The actual args are stored at an offset of 8 bytes + (8 * <NumberOfFixedParams>)
        // from the arglist pointer.
        long argsOffset = 8 + 8 * context.Callee.GetParameters().Length;
        context.ILGenerator.Emit(OpCodes.Ldc_I8, argsOffset);
        context.ILGenerator.Emit(OpCodes.Conv_U);

        context.ILGenerator.Emit(OpCodes.Add);
        context.ILGenerator.Emit(OpCodes.Stind_I);
    }
}
