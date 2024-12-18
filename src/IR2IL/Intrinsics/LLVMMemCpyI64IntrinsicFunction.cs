using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using IR2IL.Helpers;

namespace IR2IL.Intrinsics;

internal sealed class LLVMMemCpyI64IntrinsicFunction : IntrinsicFunction
{
    private static readonly MethodInfo Method = typeof(NativeMemory).GetStaticMethodStrict(nameof(NativeMemory.Copy));

    public override void BuildCall(IntrinsicFunctionCallContext context)
    {
        // declare void @llvm.memcpy.p0.p0.i64(ptr <dest>, ptr <src>, i64<len>, i1<isvolatile>)
        // public static void NativeMemory.Copy(void* source, void* destination, nuint byteCount);

        // source
        context.EmitValue(context.Operands[1]);

        // destination
        context.EmitValue(context.Operands[0]);

        // byteCount
        context.EmitValue(context.Operands[2]);
        context.ILGenerator.Emit(OpCodes.Conv_U);

        context.ILGenerator.Emit(OpCodes.Call, Method);

        // TODO: isVolatile
    }
}
