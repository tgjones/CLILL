using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using IR2IL.Helpers;

namespace IR2IL.Intrinsics;

internal sealed class LLVMMemSetI64IntrinsicFunction : IntrinsicFunction
{
    private static readonly MethodInfo Method = typeof(NativeMemory).GetStaticMethodStrict(nameof(NativeMemory.Fill));

    public override void BuildCall(IntrinsicFunctionCallContext context)
    {
        // declare void @llvm.memset.p0.i64(ptr <dest>, i8 <val>, i64<len>, i1<isvolatile>)
        // public static void NativeMemory.Fill(void* ptr, nuint byteCount, byte value);

        // ptr
        context.EmitValue(context.Operands[0]);

        // byteCount
        context.EmitValue(context.Operands[2]);
        context.ILGenerator.Emit(OpCodes.Conv_U);

        // value
        context.EmitValue(context.Operands[1]);

        context.ILGenerator.Emit(OpCodes.Call, Method);

        // TODO: isVolatile
    }
}
