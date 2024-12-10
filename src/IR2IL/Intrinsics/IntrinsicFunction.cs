namespace IR2IL.Intrinsics;

public abstract class IntrinsicFunction
{
    public abstract void BuildCall(IntrinsicFunctionCallContext context);
}
