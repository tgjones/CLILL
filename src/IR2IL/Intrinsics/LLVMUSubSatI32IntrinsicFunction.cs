using System;
using System.Reflection;
using System.Reflection.Emit;
using IR2IL.Helpers;

namespace IR2IL.Intrinsics;

internal sealed class LLVMUSubSatI32IntrinsicFunction : IntrinsicFunction
{
    private static readonly MethodInfo Method = typeof(Math).GetMethodStrict(nameof(Math.Max), [typeof(int), typeof(int)]);

    public override void BuildCall(IntrinsicFunctionCallContext context)
    {
        // declare i32 @llvm.usub.sat.i32(i32 %a, i32 %b)
        // int USubSatI32(int a, int b) => Math.Max(a - b, 0);

        context.EmitValue(context.Operands[0]);
        context.EmitValue(context.Operands[1]);

        context.ILGenerator.Emit(OpCodes.Sub);

        context.ILGenerator.Emit(OpCodes.Ldc_I4_0);

        context.ILGenerator.Emit(OpCodes.Call, Method);
    }
}
