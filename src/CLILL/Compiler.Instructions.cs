using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Intrinsics;
using LLVMSharp.Interop;

namespace CLILL
{
    partial class Compiler
    {
        private static void CompileInstruction(
            LLVMValueRef instruction,
            FunctionCompilationContext context)
        {
            CompileInstructionValue(instruction, context);

            if (instruction.TypeOf.Kind != LLVMTypeKind.LLVMVoidTypeKind
                && instruction.InstructionOpcode != LLVMOpcode.LLVMAlloca)
            {
                if (instruction.GetUses().Any())
                {
                    EmitStoreResult(context.ILGenerator, instruction, context);
                }
                else
                {
                    context.ILGenerator.Emit(OpCodes.Pop);
                }
            }
        }

        private static void CompileInstructionValue(
            LLVMValueRef instruction,
            FunctionCompilationContext context)
        {
            if (instruction.Kind != LLVMValueKind.LLVMInstructionValueKind)
            {
                throw new InvalidOperationException();
            }

            var ilGenerator = context.ILGenerator;

            switch (instruction.InstructionOpcode)
            {
                case LLVMOpcode.LLVMAdd:
                case LLVMOpcode.LLVMFAdd:
                    EmitBinaryOperation(instruction, OpCodes.Add, "Add", context);
                    break;

                case LLVMOpcode.LLVMAnd:
                    {
                        EmitValue(instruction.GetOperand(0), context);
                        EmitValue(instruction.GetOperand(1), context);
                        ilGenerator.Emit(OpCodes.And);
                        break;
                    }

                case LLVMOpcode.LLVMAlloca:
                    {
                        var numElements = instruction.GetOperand(0);
                        if (numElements.Kind != LLVMValueKind.LLVMConstantIntValueKind || numElements.ConstIntSExt != 1)
                        {
                            // TODO: Implement array allocation.
                            // TODO: Handle non-constant NumElements by using localloc.
                            throw new NotImplementedException();
                        }

                        // TODO: Alignment
                        var allocatedType = instruction.GetAllocatedType();
                        var local = ilGenerator.DeclareLocal(GetMsilType(allocatedType, context.CompilationContext));
                        context.Locals.Add(instruction, local);
                        if (allocatedType.Kind == LLVMTypeKind.LLVMArrayTypeKind)
                        {
                            ilGenerator.Emit(OpCodes.Ldc_I4, context.CompilationContext.GetSizeOfTypeInBytes(allocatedType));
                            ilGenerator.Emit(OpCodes.Conv_U);
                            ilGenerator.Emit(OpCodes.Localloc);
                            ilGenerator.Emit(OpCodes.Stloc, local);
                        }
                        break;
                    }

                case LLVMOpcode.LLVMStore:
                    {
                        var value = instruction.GetOperand(0);
                        var ptr = instruction.GetOperand(1);

                        if (context.Locals.TryGetValue(ptr, out var local) && (local.LocalType.IsPrimitive || local.LocalType.IsPointer))
                        {
                            EmitValue(value, context);
                            ilGenerator.Emit(OpCodes.Stloc, local);
                        }
                        else
                        {
                            EmitValue(ptr, context);
                            EmitValue(value, context);
                            EmitStoreIndirect(context.ILGenerator, value.TypeOf);
                        }
                        break;
                    }

                case LLVMOpcode.LLVMBr:
                    EmitBr(instruction, context);
                    break;

                case LLVMOpcode.LLVMGetElementPtr:
                    EmitGetElementPtr(instruction, context);
                    break;

                case LLVMOpcode.LLVMICmp:
                    {
                        EmitValue(instruction.GetOperand(0), context);
                        EmitValue(instruction.GetOperand(1), context);
                        switch (instruction.ICmpPredicate)
                        {
                            case LLVMIntPredicate.LLVMIntEQ:
                                ilGenerator.Emit(OpCodes.Ceq);
                                break;

                            case LLVMIntPredicate.LLVMIntSLE:
                                ilGenerator.Emit(OpCodes.Cgt);
                                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                ilGenerator.Emit(OpCodes.Ceq);
                                break;

                            case LLVMIntPredicate.LLVMIntSLT:
                                ilGenerator.Emit(OpCodes.Clt);
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;
                    }

                case LLVMOpcode.LLVMLoad:
                    EmitLoad(instruction, context);
                    break;

                case LLVMOpcode.LLVMCall:
                    EmitCall(instruction, context);
                    break;

                case LLVMOpcode.LLVMFDiv:
                    EmitBinaryOperation(instruction, OpCodes.Div, "Divide", context);
                    break;

                case LLVMOpcode.LLVMPHI:
                    ilGenerator.Emit(OpCodes.Ldloc, context.PhiLocals[instruction]);
                    break;

                case LLVMOpcode.LLVMMul:
                case LLVMOpcode.LLVMFMul:
                    EmitBinaryOperation(instruction, OpCodes.Mul, "Multiply", context);
                    break;

                case LLVMOpcode.LLVMRet:
                    {
                        if (instruction.OperandCount > 0)
                        {
                            var returnOperand = instruction.GetOperand(0);
                            EmitValue(returnOperand, context);
                        }
                        ilGenerator.Emit(OpCodes.Ret);
                        break;
                    }

                case LLVMOpcode.LLVMSDiv:
                    EmitBinaryOperation(instruction, OpCodes.Div, "Divide", context);
                    break;

                case LLVMOpcode.LLVMSExt:
                    {
                        var fromType = instruction.GetOperand(0).TypeOf;
                        var toType = instruction.TypeOf;
                        switch (fromType.Kind, toType.Kind)
                        {
                            case (LLVMTypeKind.LLVMIntegerTypeKind, LLVMTypeKind.LLVMIntegerTypeKind):
                                switch (fromType.IntWidth, toType.IntWidth)
                                {
                                    case (8, 32):
                                        EmitValue(instruction.GetOperand(0), context);
                                        ilGenerator.Emit(OpCodes.Conv_I4);
                                        break;

                                    case (32, 64):
                                        EmitValue(instruction.GetOperand(0), context);
                                        ilGenerator.Emit(OpCodes.Conv_I8);
                                        break;

                                    default:
                                        throw new NotImplementedException($"Integer conversion from {fromType.IntWidth} to {toType.IntWidth} not implemented");
                                }
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;
                    }

                case LLVMOpcode.LLVMSub:
                case LLVMOpcode.LLVMFSub:
                    EmitBinaryOperation(instruction, OpCodes.Sub, "Subtract", context);
                    break;

                case LLVMOpcode.LLVMSwitch:
                    EmitSwitch(instruction, context);
                    break;

                case LLVMOpcode.LLVMUDiv:
                    EmitBinaryOperation(instruction, OpCodes.Div_Un, "Divide", context);
                    break;

                case LLVMOpcode.LLVMZExt:
                    {
                        EmitValue(instruction.GetOperand(0), context);

                        var fromType = instruction.GetOperand(0).TypeOf;
                        var toType = instruction.TypeOf;
                        switch (fromType.Kind, toType.Kind)
                        {
                            case (LLVMTypeKind.LLVMIntegerTypeKind, LLVMTypeKind.LLVMIntegerTypeKind):
                                switch (fromType.IntWidth, toType.IntWidth)
                                {
                                    case (32, 64):
                                        ilGenerator.Emit(OpCodes.Conv_U8);
                                        break;

                                    default:
                                        throw new NotImplementedException();
                                }
                                break;

                            default:
                                throw new NotImplementedException();
                        }

                        break;
                    }

                default:
                    throw new NotImplementedException($"Instruction {instruction.InstructionOpcode} is not implemented");
            }
        }

        private static void EmitBinaryOperation(
            LLVMValueRef instruction, 
            OpCode scalarOpCode,
            string vectorMethodName,
            FunctionCompilationContext context)
        {
            EmitValue(instruction.GetOperand(0), context);
            EmitValue(instruction.GetOperand(1), context);
            switch (instruction.TypeOf.Kind)
            {
                case LLVMTypeKind.LLVMDoubleTypeKind:
                case LLVMTypeKind.LLVMFloatTypeKind:
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    context.ILGenerator.Emit(scalarOpCode);
                    break;

                case LLVMTypeKind.LLVMVectorTypeKind:
                    if (instruction.TypeOf.ElementType.Kind != LLVMTypeKind.LLVMIntegerTypeKind
                        || instruction.TypeOf.ElementType.IntWidth != 32)
                    {
                        throw new NotImplementedException();
                    }
                    context.ILGenerator.EmitCall(
                        OpCodes.Call,
                        typeof(Vector128).GetMethod(vectorMethodName).MakeGenericMethod(GetMsilType(instruction.TypeOf.ElementType, context.CompilationContext)),
                        null);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private static void EmitBr(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            if (instruction.IsConditional)
            {
                var condition = instruction.Condition;

                OpCode branchOpcode;
                if (context.CanPushToStackLookup[condition]
                    && condition.InstructionOpcode == LLVMOpcode.LLVMICmp)
                {
                    EmitValue(condition.GetOperand(0), context);
                    EmitValue(condition.GetOperand(1), context);

                    branchOpcode = condition.ICmpPredicate switch
                    {
                        LLVMIntPredicate.LLVMIntEQ => OpCodes.Beq,
                        LLVMIntPredicate.LLVMIntSLT => OpCodes.Blt,
                        LLVMIntPredicate.LLVMIntSLE => OpCodes.Ble,
                        LLVMIntPredicate.LLVMIntSGT => OpCodes.Bgt,
                        LLVMIntPredicate.LLVMIntULT => OpCodes.Blt_Un,
                        _ => throw new NotImplementedException(),
                    };
                }
                else
                {
                    EmitValue(condition, context);

                    branchOpcode = OpCodes.Brtrue;

                }

                var trueBlock = instruction.GetSuccessor(0);

                Label? trueBlockPhiLabel = null;
                if (trueBlock.ContainsPhiNodes())
                {
                    trueBlockPhiLabel = context.ILGenerator.DefineLabel();
                    context.ILGenerator.Emit(branchOpcode, trueBlockPhiLabel.Value);
                }
                else
                {
                    context.ILGenerator.Emit(branchOpcode, context.GetOrCreateLabel(trueBlock));
                }

                EmitBranchUnconditional(instruction, instruction.GetSuccessor(1), context);

                if (trueBlockPhiLabel != null)
                {
                    context.ILGenerator.MarkLabel(trueBlockPhiLabel.Value);
                    EmitPhiValues(instruction.InstructionParent, trueBlock, context);
                    context.ILGenerator.Emit(OpCodes.Br, context.GetOrCreateLabel(trueBlock));
                }
            }
            else
            {
                EmitBranchUnconditional(instruction, instruction.GetSuccessor(0), context);
            }
        }

        private static void EmitBranchUnconditional(LLVMValueRef brInstruction, LLVMBasicBlockRef to, FunctionCompilationContext context)
        {
            if (to.ContainsPhiNodes())
            {
                EmitPhiValues(brInstruction.InstructionParent, to, context);
            }
            context.ILGenerator.Emit(OpCodes.Br, context.GetOrCreateLabel(to));
        }

        private static void EmitCall(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            var operands = instruction.GetOperands().ToList();

            for (var i = 0; i < operands.Count - 1; i++)
            {
                EmitValue(operands[i], context);
            }

            var method = GetOrCreateMethod(operands[^1], context.CompilationContext);

            var varArgsParameterTypes = Array.Empty<Type>();
            if ((method.CallingConvention & CallingConventions.VarArgs) != 0)
            {
                var parameters = method.GetParameters();
                varArgsParameterTypes = new Type[operands.Count - 1 - parameters.Length];
                for (var i = 0; i < varArgsParameterTypes.Length; i++)
                {
                    varArgsParameterTypes[i] = GetMsilType(operands[i + parameters.Length].TypeOf, context.CompilationContext);
                }
            }

            context.ILGenerator.EmitCall(
                OpCodes.Call,
                method,
                varArgsParameterTypes);
        }

        private static unsafe void EmitGetElementPtr(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            var pointer = instruction.GetOperand(0);
            EmitValue(pointer, context);

            var sourceElementType = (LLVMTypeRef)LLVM.GetGEPSourceElementType(instruction);
            var currentType = sourceElementType;

            // First index operand always indexes into the source element pointer type.
            EmitIndexedPtr(instruction.GetOperand(1), currentType, context);

            for (var i = 2u; i < instruction.OperandCount; i++)
            {
                var index = instruction.GetOperand(i);

                switch (currentType.Kind)
                {
                    case LLVMTypeKind.LLVMArrayTypeKind:
                        EmitIndexedPtr(index, currentType, context);
                        currentType = currentType.ElementType;
                        break;

                    case LLVMTypeKind.LLVMIntegerTypeKind:
                        EmitIndexedPtr(index, currentType, context);
                        break;

                    case LLVMTypeKind.LLVMStructTypeKind:
                        var structType = GetMsilType(currentType, context.CompilationContext);
                        uint fieldIndex;
                        if (index.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                        {
                            fieldIndex = (uint)index.ConstIntSExt;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        var field = structType.GetFields()[fieldIndex];
                        context.ILGenerator.Emit(OpCodes.Ldflda, field);
                        context.ILGenerator.Emit(OpCodes.Conv_U);
                        currentType = currentType.StructGetTypeAtIndex(fieldIndex);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private static void EmitIndexedPtr(LLVMValueRef index, LLVMTypeRef currentType, FunctionCompilationContext context)
        {
            var sizeInBytes = (long)(context.CompilationContext.GetSizeOfTypeInBytes(currentType));

            if (index.Kind == LLVMValueKind.LLVMConstantIntValueKind)
            {
                if (index.ConstIntSExt == 0)
                {
                    // No need to do anything.
                }
                else
                {
                    if (index.ConstIntSExt > 0)
                    {
                        context.ILGenerator.Emit(OpCodes.Ldc_I8, sizeInBytes * index.ConstIntSExt);
                        context.ILGenerator.Emit(OpCodes.Conv_U);
                        context.ILGenerator.Emit(OpCodes.Add);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            else
            {
                EmitValue(index, context);

                if (index.TypeOf.IntWidth != 64)
                {
                    throw new NotImplementedException();
                }

                if (sizeInBytes != 1)
                {
                    context.ILGenerator.Emit(OpCodes.Ldc_I8, sizeInBytes);
                    context.ILGenerator.Emit(OpCodes.Mul);
                }

                context.ILGenerator.Emit(OpCodes.Conv_U);
                context.ILGenerator.Emit(OpCodes.Add);
            }
        }

        private static void EmitPhiValues(LLVMBasicBlockRef from, LLVMBasicBlockRef to, FunctionCompilationContext context)
        {
            var phiInstructions = to.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMPHI);

            foreach (var instruction in phiInstructions)
            {
                EmitValue(instruction.GetIncomingValueForBlock(from), context);
                context.ILGenerator.Emit(OpCodes.Stloc, context.PhiLocals[instruction]);
            }
        }

        private readonly record struct SwitchCase(int ConstantValue, LLVMValueRef Value, LLVMBasicBlockRef Destination)
            : IComparable<SwitchCase>
        {
            public int CompareTo(SwitchCase other)
            {
                return ConstantValue.CompareTo(other.ConstantValue);
            }
        }

        private static void EmitSwitch(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            // TODO: We could do a better job here at filling in the gaps if we have good enough density,
            // similar to what Roslyn does with jump tables:
            // https://github.com/dotnet/roslyn/blob/36e1fe3c27adb70b3ad49c9d51d7cc19d88e656e/src/Compilers/Core/Portable/CodeGen/SwitchIntegralJumpTableEmitter.cs#L67C23-L67C36

            var operands = instruction.GetOperands().ToList();

            // Operand 0 is the condition value.
            var condition = operands[0];

            if (condition.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind
                || condition.TypeOf.IntWidth != 32)
            {
                throw new NotImplementedException();
            }

            // Operand 1 is the default destination.

            // Operand 2+ are the cases in the format:
            // - {n+0} = case value
            // - {n+1} = case destination
            var cases = new List<SwitchCase>();
            for (var i = 2; i < operands.Count; i += 2)
            {
                var caseValueType = operands[i].Kind;

                if (operands[i].Kind != LLVMValueKind.LLVMConstantIntValueKind
                    || operands[i].TypeOf.IntWidth != 32)
                {
                    throw new NotImplementedException();
                }

                var caseValue = (int)operands[i].ConstIntSExt;

                cases.Add(new SwitchCase(caseValue, operands[i], operands[i + 1].AsBasicBlock()));
            }
            cases.Sort();

            while (cases.Count > 0)
            {
                var caseValue = cases[0].ConstantValue;

                // Include as many sequential values as we can.
                var endIndex = 1;
                while (endIndex < cases.Count && cases[endIndex].ConstantValue == caseValue + endIndex)
                {
                    endIndex++;
                }

                EmitValue(condition, context);
                if (caseValue != 0)
                {
                    EmitValue(cases[0].Value, context);
                }

                // If we have 2 or more cases, make a switch instruction.
                if (endIndex > 1)
                {
                    if (caseValue != 0)
                    {
                        context.ILGenerator.Emit(OpCodes.Sub);
                    }
                    var jumpTable = cases
                        .Take(endIndex)
                        .Select(x => context.GetOrCreateLabel(x.Destination))
                        .ToArray();
                    context.ILGenerator.Emit(OpCodes.Switch, jumpTable);
                    cases.RemoveRange(0, endIndex);
                }
                else // Otherwise, do a normal conditional branch.
                {
                    if (caseValue == 0)
                    {
                        context.ILGenerator.Emit(OpCodes.Ldc_I4, 0);
                    }
                    context.ILGenerator.Emit(OpCodes.Beq, context.GetOrCreateLabel(cases[0].Destination));
                    cases.RemoveAt(0);
                }
            }

            context.ILGenerator.Emit(OpCodes.Br, context.GetOrCreateLabel(instruction.SwitchDefaultDest));
        }

        private static void EmitValue(
            LLVMValueRef valueRef,
            FunctionCompilationContext context)
        {
            if (valueRef.IsConstant)
            {
                switch (valueRef.Kind)
                {
                    case LLVMValueKind.LLVMConstantAggregateZeroValueKind:
                        switch (valueRef.TypeOf.Kind)
                        {
                            case LLVMTypeKind.LLVMVectorTypeKind:
                                context.ILGenerator.Emit(OpCodes.Call, GetMsilVectorType(valueRef.TypeOf, context.CompilationContext).GetProperty("Zero").GetGetMethod());
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case LLVMValueKind.LLVMConstantFPValueKind:
                        switch (valueRef.TypeOf.Kind)
                        {
                            case LLVMTypeKind.LLVMDoubleTypeKind:
                                context.ILGenerator.Emit(OpCodes.Ldc_R8, valueRef.GetConstRealDouble(out _));
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case LLVMValueKind.LLVMConstantIntValueKind:
                        switch (valueRef.TypeOf.IntWidth)
                        {
                            case 1:
                            case 8:
                            case 32:
                                context.ILGenerator.Emit(OpCodes.Ldc_I4, (int)valueRef.ConstIntSExt);
                                break;

                            case 64:
                                context.ILGenerator.Emit(OpCodes.Ldc_I8, (long)valueRef.ConstIntSExt);
                                break;

                            default:
                                throw new NotImplementedException($"Constant int width {valueRef.TypeOf.IntWidth} not implemented");
                        }
                        break;

                    case LLVMValueKind.LLVMGlobalVariableValueKind:
                        var staticField = context.CompilationContext.Globals[valueRef];
                        context.ILGenerator.Emit(OpCodes.Ldsfld, staticField);
                        break;

                    default:
                        throw new NotImplementedException($"Unsupported value {valueRef.Kind}");
                }
            }
            else if (context.Locals.TryGetValue(valueRef, out var local))
            {
                // TODO: Don't do this Vector128 hack
                if (local.LocalType.IsValueType && !local.LocalType.IsPrimitive && !local.LocalType.IsPointer && !local.LocalType.Name.Contains("Vector128"))
                {
                    context.ILGenerator.Emit(OpCodes.Ldloca, local);
                }
                else
                {
                    context.ILGenerator.Emit(OpCodes.Ldloc, local);
                }
            }
            else if (context.PhiLocals.TryGetValue(valueRef, out var phiLocal))
            {
                context.ILGenerator.Emit(OpCodes.Ldloc, phiLocal);
            }
            else if (context.Parameters.TryGetValue(valueRef, out var parameter))
            {
                context.ILGenerator.Emit(OpCodes.Ldarg, parameter.Position - 1);
            }
            else if (context.CanPushToStackLookup[valueRef])
            {
                CompileInstructionValue(valueRef, context);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static void EmitLoad(
            LLVMValueRef instruction,
            FunctionCompilationContext context)
        {
            var valueRef = instruction.GetOperand(0);

            EmitValue(valueRef, context);

            // TODO: Find better way to do this.
            if (valueRef.IsAAllocaInst != null)
            {
                // We'll have emitted ldloc instruction, no need to dereference pointer.
                return;
            }

            switch (instruction.TypeOf.Kind)
            {
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    switch (instruction.TypeOf.IntWidth)
                    {
                        case 32:
                            context.ILGenerator.Emit(OpCodes.Ldind_I4);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case LLVMTypeKind.LLVMPointerTypeKind:
                    context.ILGenerator.Emit(OpCodes.Ldind_I);
                    break;

                case LLVMTypeKind.LLVMVectorTypeKind:
                    context.ILGenerator.Emit(OpCodes.Ldobj, GetMsilType(instruction.TypeOf, context.CompilationContext));
                    break;

                default:
                    throw new NotImplementedException($"Unsupported type {instruction.TypeOf.Kind}");
            }
        }

        private static void EmitStoreResult(
            ILGenerator ilGenerator, 
            LLVMValueRef instruction, 
            FunctionCompilationContext context)
        {
            var local = ilGenerator.DeclareLocal(GetMsilType(instruction.TypeOf, context.CompilationContext));
            context.Locals.Add(instruction, local);
            EmitStloc(ilGenerator, instruction, instruction.TypeOf, context);
        }

        private static void EmitStloc(
            ILGenerator ilGenerator,
            LLVMValueRef valueRef, 
            LLVMTypeRef type,
            FunctionCompilationContext context)
        {
            if (context.Locals.TryGetValue(valueRef, out var local))
            {
                ilGenerator.Emit(OpCodes.Stloc, local);
            }
            else
            {
                EmitValue(valueRef, context);
                EmitStoreIndirect(context.ILGenerator, type);
            }
        }
    }
}
