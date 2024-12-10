using System.Reflection.Emit;

namespace IR2IL.Intrinsics;

internal sealed class LLVMStackSaveIntrinsicFunction : IntrinsicFunction
{
    public override void BuildCall(IntrinsicFunctionCallContext context)
    {
        // TODO: We don't really support this. Ideally emit a warning.

        context.ILGenerator.Emit(OpCodes.Ldc_I4_0);
        context.ILGenerator.Emit(OpCodes.Conv_U);
    }
}