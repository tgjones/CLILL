namespace IR2IL.Intrinsics;

public sealed class NoOpIntrinsicFunction : IntrinsicFunction
{
    public static readonly NoOpIntrinsicFunction Instance = new();

    public override void BuildCall(IntrinsicFunctionCallContext context)
    {
        // Nothing to do.
    }
}
