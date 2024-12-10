namespace CLILL.Intrinsics;

internal sealed class LLVMDbgDeclareIntrinsicFunction : IntrinsicFunction
{
    public override void BuildCall(IntrinsicFunctionCallContext context)
    {
        var value = context.Operands[0].MDNodeOperands[0];

        var diLocalVariable = context.Operands[1];
        var diLocalVariableName = diLocalVariable.GetDILocalVariableName();

        var diLocalVariableArg = diLocalVariable.GetDILocalVariableArg();
        if (diLocalVariableArg != null)
        {
            // Parameter information. We'll have already used this to set the parameter name.
        }
        else
        {
            if (context.Locals.TryGetValue(value, out var local))
            {
                // Local information.
                local.SetLocalSymInfo(diLocalVariableName);
            }
            //else
            //{
            //    // Global information
            //    var global = context.CompilationContext.Globals[value];
            //}
        }

        //var expression = instruction.GetOperand(2).AsMetadata();

        //var dbgMetadata = instruction.GetMetadata("dbg");

        //var line = dbgMetadata.GetDILocationLine();
        //var scope = dbgMetadata.GetDILocationScope();

        //var file = scope.GetDIScopeFile();

        //var symbolDocumentWriter = TypeSystem.GetDocument(file);
    }
}
