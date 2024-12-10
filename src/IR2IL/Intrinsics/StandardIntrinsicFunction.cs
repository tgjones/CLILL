using System;
using System.Reflection;
using System.Reflection.Emit;
using IR2IL.Helpers;

namespace IR2IL.Intrinsics;

public sealed class StandardIntrinsicFunction(MethodInfo method) : IntrinsicFunction
{
    public static StandardIntrinsicFunction Create(Type type, string methodName, params Type[] parameterTypes)
    {
        return parameterTypes.Length > 0
            ? new StandardIntrinsicFunction(type.GetMethodStrict(methodName, parameterTypes))
            : new StandardIntrinsicFunction(type.GetMethodStrict(methodName));
    }

    public static StandardIntrinsicFunction CreateGeneric(Type type, string methodName, params Type[] typeArguments)
    {
        return new StandardIntrinsicFunction(type.GetMethodStrict(methodName).MakeGenericMethod(typeArguments));
    }

    public override void BuildCall(IntrinsicFunctionCallContext context)
    {
        for (var i = 0; i < context.Operands.Length - 1; i++)
        {
            context.EmitValue(context.Operands[i]);
        }

        context.ILGenerator.Emit(OpCodes.Call, method);
    }
}
