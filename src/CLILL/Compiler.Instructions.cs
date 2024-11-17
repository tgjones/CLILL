using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using LLVMSharp.Interop;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CLILL
{
    partial class Compiler
    {
        private void CompileInstruction(
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

        private void CompileInstructionValue(
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
                    EmitBinaryOperation(instruction, OpCodes.Add, nameof(Vector128.Add), context);
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
                        //if (allocatedType.Kind == LLVMTypeKind.LLVMArrayTypeKind)
                        //{
                        //    ilGenerator.Emit(OpCodes.Ldc_I4, context.CompilationContext.GetSizeOfTypeInBytes(allocatedType));
                        //    ilGenerator.Emit(OpCodes.Conv_U);
                        //    ilGenerator.Emit(OpCodes.Localloc);
                        //    ilGenerator.Emit(OpCodes.Stloc, local);
                        //}
                        break;
                    }

                case LLVMOpcode.LLVMStore:
                    {
                        var value = instruction.GetOperand(0);
                        var ptr = instruction.GetOperand(1);

                        if (ptr.IsAAllocaInst != null && context.Locals.TryGetValue(ptr, out var local) && (local.LocalType.IsPrimitive || local.LocalType.IsPointer))
                        {
                            // But only if value came from alloca?
                            EmitValue(value, context, forceLdloca: value.IsAAllocaInst != null /*&& value.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind*/);
                            ilGenerator.Emit(OpCodes.Stloc, local);
                        }
                        else
                        {
                            EmitValue(ptr, context);
                            EmitValue(value, context);
                            EmitStoreIndirect(context.ILGenerator, value.TypeOf, context.CompilationContext);
                        }
                        break;
                    }

                case LLVMOpcode.LLVMBr:
                    EmitBr(instruction, context);
                    break;

                case LLVMOpcode.LLVMCall:
                    EmitCall(instruction, context);
                    break;

                case LLVMOpcode.LLVMFCmp:
                    EmitFCmp(instruction, context);
                    break;

                case LLVMOpcode.LLVMGetElementPtr:
                    EmitGetElementPtr(instruction, context);
                    break;

                case LLVMOpcode.LLVMICmp:
                    EmitICmp(instruction, context);
                    break;

                case LLVMOpcode.LLVMInsertElement:
                    EmitInsertElement(instruction, context);
                    break;

                case LLVMOpcode.LLVMLoad:
                    EmitLoad(instruction, context);
                    break;

                case LLVMOpcode.LLVMFDiv:
                    EmitBinaryOperation(instruction, OpCodes.Div, nameof(Vector128.Divide), context);
                    break;

                case LLVMOpcode.LLVMFPExt:
                    EmitConversion(instruction, context, Signedness.Unsigned);
                    break;

                case LLVMOpcode.LLVMFPTrunc:
                    EmitConversion(instruction, context, Signedness.Unsigned);
                    break;

                case LLVMOpcode.LLVMLShr:
                    EmitBinaryOperation(instruction, OpCodes.Shr_Un, nameof(Vector128.ShiftRightLogical), context);
                    break;

                case LLVMOpcode.LLVMPHI:
                    ilGenerator.Emit(OpCodes.Ldloc, context.PhiLocals[instruction]);
                    break;

                case LLVMOpcode.LLVMMul:
                case LLVMOpcode.LLVMFMul:
                    EmitBinaryOperation(instruction, OpCodes.Mul, nameof(Vector128.Multiply), context);
                    break;

                case LLVMOpcode.LLVMOr:
                    EmitBinaryOperation(instruction, OpCodes.Or, nameof(Vector128.BitwiseOr), context);
                    break;

                case LLVMOpcode.LLVMPtrToInt:
                    EmitConversion(instruction, context, Signedness.Unsigned);
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
                    EmitBinaryOperation(instruction, OpCodes.Div, nameof(Vector128.Divide), context);
                    break;

                case LLVMOpcode.LLVMSelect:
                    EmitSelect(instruction, context);
                    break;

                case LLVMOpcode.LLVMSExt:
                    EmitConversion(instruction, context, Signedness.Signed);
                    break;

                case LLVMOpcode.LLVMShl:
                    EmitBinaryOperation(instruction, OpCodes.Shl, nameof(Vector128.ShiftLeft), context);
                    break;

                case LLVMOpcode.LLVMShuffleVector:
                    EmitShuffleVector(instruction, context);
                    break;

                case LLVMOpcode.LLVMSIToFP:
                    EmitConversion(instruction, context, Signedness.Unsigned);
                    break;

                case LLVMOpcode.LLVMSub:
                case LLVMOpcode.LLVMFSub:
                    EmitBinaryOperation(instruction, OpCodes.Sub, nameof(Vector128.Subtract), context);
                    break;

                case LLVMOpcode.LLVMSRem:
                    EmitBinaryOperation(instruction, OpCodes.Rem, context);
                    break;

                case LLVMOpcode.LLVMSwitch:
                    EmitSwitch(instruction, context);
                    break;

                case LLVMOpcode.LLVMTrunc:
                    EmitConversion(instruction, context, Signedness.Unsigned);
                    break;

                case LLVMOpcode.LLVMUDiv:
                    EmitBinaryOperation(instruction, OpCodes.Div_Un, nameof(Vector128.Divide), context);
                    break;

                case LLVMOpcode.LLVMUnreachable:
                    ilGenerator.Emit(OpCodes.Ldstr, "Unreachable instruction");
                    ilGenerator.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor([typeof(string)]));
                    ilGenerator.Emit(OpCodes.Throw);
                    break;

                case LLVMOpcode.LLVMURem:
                    EmitBinaryOperation(instruction, OpCodes.Rem_Un, context);
                    break;

                case LLVMOpcode.LLVMXor:
                    EmitBinaryOperation(instruction, OpCodes.Xor, nameof(Vector128.Xor), context);
                    break;

                case LLVMOpcode.LLVMZExt:
                    EmitConversion(instruction, context, Signedness.Unsigned);
                    break;

                default:
                    throw new NotImplementedException($"Instruction {instruction.InstructionOpcode} is not implemented: {instruction}");
            }
        }

        private void EmitShuffleVector(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            // This is a workaround for there not yet being a Vector128.Shuffle that takes two input vectors.
            // It's being discussed in https://github.com/dotnet/runtime/issues/63331

            if (instruction.GetOperand(0).TypeOf.VectorSize != 4
                || instruction.GetOperand(0).TypeOf.ElementType.Kind != LLVMTypeKind.LLVMIntegerTypeKind
                || instruction.GetOperand(0).TypeOf.ElementType.IntWidth != 32)
            {
                throw new NotImplementedException();
            }

            var elementType = GetMsilType(instruction.TypeOf.ElementType, context.CompilationContext);
            var vectorType = typeof(Vector128<>).MakeGenericType(elementType);

            // Combine input vectors into one Vector256.
            EmitValue(instruction.GetOperand(0), context);
            EmitValue(instruction.GetOperand(1), context);
            context.ILGenerator.Emit(OpCodes.Call, typeof(Vector256).GetMethod(nameof(Vector256.Create), [vectorType, vectorType]));

            // Indices
            var maskValues = instruction.GetShuffleVectorMaskValues();
            if (maskValues.Length != 4)
            {
                throw new NotImplementedException();
            }
            foreach (var maskValue in maskValues)
            {
                context.ILGenerator.Emit(OpCodes.Ldc_I4, maskValue);
            }
            context.ILGenerator.Emit(OpCodes.Call, typeof(Vector128).GetMethod(nameof(Vector128.Create), [typeof(int), typeof(int), typeof(int), typeof(int)]));
            context.ILGenerator.Emit(OpCodes.Call, typeof(Vector128).GetMethod(nameof(Vector128.ToVector256)).MakeGenericMethod(elementType));

            // Do the shuffle
            context.ILGenerator.Emit(OpCodes.Call, typeof(Vector256).GetMethod(nameof(Vector256.Shuffle), [typeof(Vector256<>).MakeGenericType(elementType), typeof(Vector256<>).MakeGenericType(typeof(int))]));

            // Extract the Vector128 result
            context.ILGenerator.Emit(OpCodes.Call, typeof(Vector256).GetMethod(nameof(Vector256.GetLower)).MakeGenericMethod(elementType));
        }

        private void EmitInsertElement(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            // Vector
            EmitValue(instruction.GetOperand(0), context);

            // Index
            EmitValue(instruction.GetOperand(2), context);
            context.ILGenerator.Emit(OpCodes.Conv_I4);

            // Value
            EmitValue(instruction.GetOperand(1), context);

            var withElementMethod = typeof(Vector128).GetMethod(nameof(Vector128.WithElement));
            context.ILGenerator.Emit(OpCodes.Call, withElementMethod);
        }

        private void EmitICmp(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            var left = instruction.GetOperand(0);

            if (left.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind
                && left.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
            {
                throw new NotImplementedException($"Unsupported icmp operand type {left.TypeOf.Kind}: {left.TypeOf}");
            }

            EmitValue(left, context);
            EmitValue(instruction.GetOperand(1), context);

            switch (instruction.ICmpPredicate)
            {
                case LLVMIntPredicate.LLVMIntEQ:
                    context.ILGenerator.Emit(OpCodes.Ceq);
                    break;

                case LLVMIntPredicate.LLVMIntNE:
                    context.ILGenerator.Emit(OpCodes.Ceq);
                    context.ILGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.ILGenerator.Emit(OpCodes.Ceq);
                    break;

                case LLVMIntPredicate.LLVMIntSGE:
                    context.ILGenerator.Emit(OpCodes.Clt);
                    context.ILGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.ILGenerator.Emit(OpCodes.Ceq);
                    break;

                case LLVMIntPredicate.LLVMIntSGT:
                    context.ILGenerator.Emit(OpCodes.Cgt);
                    break;

                case LLVMIntPredicate.LLVMIntSLE:
                    context.ILGenerator.Emit(OpCodes.Cgt);
                    context.ILGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.ILGenerator.Emit(OpCodes.Ceq);
                    break;

                case LLVMIntPredicate.LLVMIntSLT:
                    context.ILGenerator.Emit(OpCodes.Clt);
                    break;

                case LLVMIntPredicate.LLVMIntUGT:
                    context.ILGenerator.Emit(OpCodes.Cgt_Un);
                    break;

                case LLVMIntPredicate.LLVMIntULT:
                    context.ILGenerator.Emit(OpCodes.Clt_Un);
                    break;

                default:
                    throw new NotImplementedException($"Integer comparison predicate {instruction.ICmpPredicate} not implemented: {instruction}");
            }
        }

        private void EmitFCmp(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            var operand0 = instruction.GetOperand(0);

            switch (operand0.TypeOf.Kind)
            {
                case LLVMTypeKind.LLVMFloatTypeKind:
                case LLVMTypeKind.LLVMDoubleTypeKind:
                    break;

                default:
                    throw new NotImplementedException();
            }

            EmitValue(operand0, context);
            EmitValue(instruction.GetOperand(1), context);

            // TODO: Do the right thing for ordered / unordered equality comparisons.

            switch (instruction.FCmpPredicate)
            {
                case LLVMRealPredicate.LLVMRealOEQ:
                    context.ILGenerator.Emit(OpCodes.Ceq);
                    break;

                case LLVMRealPredicate.LLVMRealOLT:
                    context.ILGenerator.Emit(OpCodes.Clt);
                    break;

                default:
                    throw new NotImplementedException($"Float comparison predicate {instruction.FCmpPredicate} not implemented: {instruction}");
            }
        }

        private enum Signedness
        {
            Signed,
            Unsigned,
        }

        private void EmitConversion(LLVMValueRef instruction, FunctionCompilationContext context, Signedness signedness)
        {
            EmitValue(instruction.GetOperand(0), context);

            var toType = instruction.TypeOf;
            switch (toType.Kind)
            {
                case LLVMTypeKind.LLVMDoubleTypeKind:
                    context.ILGenerator.Emit(OpCodes.Conv_R8);
                    break;

                case LLVMTypeKind.LLVMFloatTypeKind:
                    context.ILGenerator.Emit(OpCodes.Conv_R4);
                    break;

                case LLVMTypeKind.LLVMIntegerTypeKind:
                    switch (toType.IntWidth, signedness)
                    {
                        case (8, Signedness.Signed):
                            context.ILGenerator.Emit(OpCodes.Conv_I1);
                            break;

                        case (8, Signedness.Unsigned):
                            context.ILGenerator.Emit(OpCodes.Conv_U1);
                            break;

                        case (16, Signedness.Signed):
                            context.ILGenerator.Emit(OpCodes.Conv_I2);
                            break;

                        case (16, Signedness.Unsigned):
                            context.ILGenerator.Emit(OpCodes.Conv_U2);
                            break;

                        case (32, Signedness.Signed):
                            context.ILGenerator.Emit(OpCodes.Conv_I4);
                            break;

                        case (32, Signedness.Unsigned):
                            context.ILGenerator.Emit(OpCodes.Conv_U4);
                            break;

                        case (64, Signedness.Signed):
                            context.ILGenerator.Emit(OpCodes.Conv_I8);
                            break;

                        case (64, Signedness.Unsigned):
                            context.ILGenerator.Emit(OpCodes.Conv_U8);
                            break;

                        default:
                            throw new NotImplementedException($"Conversion not implemented to {toType.IntWidth}: {instruction}");
                    }
                    break;

                default:
                    throw new NotImplementedException($"Conversion not implemented to {toType}: {instruction}");
            }
        }

        private void EmitBinaryOperation(
            LLVMValueRef instruction,
            OpCode scalarOpCode,
            FunctionCompilationContext context)
        {
            EmitBinaryOperation(instruction, scalarOpCode, null, context);
        }

        private void EmitBinaryOperation(
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
                    if (vectorMethodName == null)
                    {
                        throw new NotImplementedException();
                    }
                    if (instruction.TypeOf.ElementType.Kind != LLVMTypeKind.LLVMIntegerTypeKind
                        || instruction.TypeOf.ElementType.IntWidth != 32)
                    {
                        throw new NotImplementedException();
                    }
                    context.ILGenerator.EmitCall(
                        OpCodes.Call,
                        // TODO: Don't do this.
                        typeof(Vector128).GetMethods().Single(x => x.Name == vectorMethodName && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType.Name.Contains("Vector128") && x.GetParameters()[1].ParameterType.Name.Contains("Vector128")),
                        null);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void EmitBr(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            if (instruction.IsConditional)
            {
                var branchOpcode = EmitBranchCondition(instruction.Condition, context);

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

        private OpCode EmitBranchCondition(LLVMValueRef condition, FunctionCompilationContext context)
        {
            if (context.CanPushToStack(condition)
                && condition.InstructionOpcode == LLVMOpcode.LLVMICmp)
            {
                EmitValue(condition.GetOperand(0), context);
                EmitValue(condition.GetOperand(1), context);

                return condition.ICmpPredicate switch
                {
                    LLVMIntPredicate.LLVMIntEQ => OpCodes.Beq,
                    LLVMIntPredicate.LLVMIntNE => OpCodes.Bne_Un,
                    LLVMIntPredicate.LLVMIntSGE => OpCodes.Bge,
                    LLVMIntPredicate.LLVMIntSGT => OpCodes.Bgt,
                    LLVMIntPredicate.LLVMIntSLT => OpCodes.Blt,
                    LLVMIntPredicate.LLVMIntSLE => OpCodes.Ble,
                    LLVMIntPredicate.LLVMIntUGT => OpCodes.Bgt_Un,
                    LLVMIntPredicate.LLVMIntULT => OpCodes.Blt_Un,
                    _ => throw new NotImplementedException($"Branch condition integer comparison {condition.ICmpPredicate} not implemented: {condition}"),
                };
            }
            else
            {
                EmitValue(condition, context);

                return OpCodes.Brtrue;
            }
        }

        private void EmitBranchUnconditional(LLVMValueRef brInstruction, LLVMBasicBlockRef to, FunctionCompilationContext context)
        {
            if (to.ContainsPhiNodes())
            {
                EmitPhiValues(brInstruction.InstructionParent, to, context);
            }
            context.ILGenerator.Emit(OpCodes.Br, context.GetOrCreateLabel(to));
        }

        private unsafe void EmitCall(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            var operands = instruction.GetOperands().ToList();

            switch (operands[^1].Name)
            {
                case "llvm.va_start":
                    EmitValue(operands[0], context, forceLdloca: true);
                    context.ILGenerator.Emit(OpCodes.Arglist);
                    context.ILGenerator.Emit(OpCodes.Conv_U);

                    // This is rather specific to the layout used by CoreCLR for varargs.
                    // The actual args are stored at an offset of 8 bytes + (8 * <NumberOfFixedParams>)
                    // from the arglist pointer.
                    long argsOffset = 8 + (8 * context.Parameters.Count);
                    context.ILGenerator.Emit(OpCodes.Ldc_I8, argsOffset);
                    context.ILGenerator.Emit(OpCodes.Conv_U);

                    context.ILGenerator.Emit(OpCodes.Add);
                    context.ILGenerator.Emit(OpCodes.Stind_I);
                    return;

                case "llvm.lifetime.start.p0":
                case "llvm.lifetime.end.p0":
                    // No-op.
                    return;
            }

            for (var i = 0; i < operands.Count - 1; i++)
            {
                EmitValue(operands[i], context);
            }

            var functionToCall = operands[^1];

            var varArgsParameterTypes = Array.Empty<Type>();
            var isVarArg = false;
            int numParameters;
            if (functionToCall.Kind != LLVMValueKind.LLVMConstantPointerNullValueKind)
            {
                var functionType = (LLVMTypeRef)LLVM.GlobalGetValueType(functionToCall);

                isVarArg = functionType.IsFunctionVarArg;
                if (isVarArg)
                {
                    var parameters = functionType.ParamTypes;
                    varArgsParameterTypes = new Type[operands.Count - 1 - parameters.Length];
                    for (var i = 0; i < varArgsParameterTypes.Length; i++)
                    {
                        varArgsParameterTypes[i] = GetMsilType(operands[i + parameters.Length].TypeOf, context.CompilationContext);
                    }
                }

                numParameters = (int)functionType.ParamTypesCount;
            }
            else
            {
                numParameters = operands.Count - 1;
            }

            if (functionToCall.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            {
                // This is a function pointer invocation.

                EmitValue(functionToCall, context);

                var returnType = GetMsilType(instruction.TypeOf, context.CompilationContext);

                var parameterTypes = new Type[numParameters];
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    parameterTypes[i] = GetMsilType(operands[i].TypeOf, context.CompilationContext);
                }

                if (isVarArg)
                {
                    context.ILGenerator.EmitCalli(
                        OpCodes.Calli,
                        CallingConventions.VarArgs,
                        returnType,
                        parameterTypes,
                        varArgsParameterTypes);
                }
                else
                {
                    context.ILGenerator.EmitCalli(
                        OpCodes.Calli,
                        System.Runtime.InteropServices.CallingConvention.Cdecl,
                        returnType,
                        parameterTypes);
                }

                return;
            }

            var method = GetOrCreateMethod(functionToCall, context.CompilationContext);

            context.ILGenerator.EmitCall(
                OpCodes.Call,
                method,
                varArgsParameterTypes);
        }

        private static unsafe int GetElementPtrConst(LLVMValueRef constExpr, FunctionCompilationContext context)
        {
            var sourceElementType = (LLVMTypeRef)LLVM.GetGEPSourceElementType(constExpr);
            var currentType = sourceElementType;

            var result = 0;

            if (constExpr.GetOperand(1).Kind != LLVMValueKind.LLVMConstantIntValueKind)
            {
                throw new NotImplementedException();
            }

            var sizeInBytes = (long)(context.CompilationContext.GetSizeOfTypeInBytes(currentType));
            result += (int)(constExpr.GetOperand(1).ConstIntSExt * sizeInBytes);

            for (var i = 2u; i < constExpr.OperandCount; i++)
            {
                var index = constExpr.GetOperand(i);

                if (index.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                {
                    throw new NotImplementedException();
                }

                switch (currentType.Kind)
                {
                    case LLVMTypeKind.LLVMArrayTypeKind:
                        result += (int)index.ConstIntSExt * context.CompilationContext.GetSizeOfTypeInBytes(currentType.ElementType);
                        currentType = currentType.ElementType;
                        break;

                    case LLVMTypeKind.LLVMIntegerTypeKind:
                        result += (int)index.ConstIntSExt;
                        break;

                    case LLVMTypeKind.LLVMStructTypeKind:
                        uint fieldIndex = (uint)index.ConstIntSExt;
                        var targetData = LLVM.CreateTargetData(LLVM.GetDataLayout(context.CompilationContext.LLVMModule));
                        ulong fieldOffset = LLVM.OffsetOfElement(
                            targetData,
                            currentType,
                            fieldIndex);
                        LLVM.DisposeTargetData(targetData);
                        result += (int)fieldOffset;
                        currentType = currentType.StructGetTypeAtIndex(fieldIndex);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            return result;
        }

        private unsafe void EmitGetElementPtr(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            // TODO: If every operand is const, call GetElementPtrConst

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
                        EmitIndexedPtr(index, currentType.ElementType, context);
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

        private void EmitIndexedPtr(LLVMValueRef index, LLVMTypeRef currentType, FunctionCompilationContext context)
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
                    //if (index.ConstIntSExt > 0)
                    {
                        context.ILGenerator.Emit(OpCodes.Ldc_I8, sizeInBytes * index.ConstIntSExt);
                        context.ILGenerator.Emit(OpCodes.Conv_U);
                        context.ILGenerator.Emit(OpCodes.Add);
                    }
                    //else
                    //{
                    //    throw new NotImplementedException($"Index not implemented: {index}");
                    //}
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

        private void EmitPhiValues(LLVMBasicBlockRef from, LLVMBasicBlockRef to, FunctionCompilationContext context)
        {
            // Phi values might refer to each other, e.g.
            // %2 = phi i32 [ 1, %0 ], [ %5, %1 ]
            // %3 = phi i32 [ 0, %0 ], [ %2, %1 ]
            //
            // So we need to emit all reads from phi locals
            // before the value is written.

            var phiInstructionAndIncomingValues = to.GetInstructions()
                .Where(x => x.InstructionOpcode == LLVMOpcode.LLVMPHI)
                .Select(x => new PhiInstructionAndIncomingValue(x, x.GetIncomingValueForBlock(from)))
                .ToList();

            var emittedPhiValues = new HashSet<LLVMValueRef>();

            while (phiInstructionAndIncomingValues.Count > 0)
            {
                // Find phi instructions that are not used as the incoming value for any
                // other phi instructions that we haven't emitted yet.
                // TODO
                var next = phiInstructionAndIncomingValues
                    .Where(x =>
                    {
                        foreach (var y in phiInstructionAndIncomingValues)
                        {
                            if (x == y)
                            {
                                continue;
                            }

                            if (y.IncomingValue == x.PhiInstruction
                                && !emittedPhiValues.Contains(y.PhiInstruction))
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();

                if (next.Count == 0)
                {
                    throw new InvalidOperationException($"Invalid phi values from block {from} to block {to}");
                }

                foreach (var phiInstructionAndIncomingValue in next)
                {
                    EmitValue(phiInstructionAndIncomingValue.IncomingValue, context);
                    context.ILGenerator.Emit(OpCodes.Stloc, context.PhiLocals[phiInstructionAndIncomingValue.PhiInstruction]);

                    phiInstructionAndIncomingValues.Remove(phiInstructionAndIncomingValue);
                    emittedPhiValues.Add(phiInstructionAndIncomingValue.PhiInstruction);
                }
            }
        }

        private readonly record struct PhiInstructionAndIncomingValue(LLVMValueRef PhiInstruction, LLVMValueRef IncomingValue);

        private void EmitSelect(LLVMValueRef instruction, FunctionCompilationContext context)
        {
            var trueLabel = context.ILGenerator.DefineLabel();
            var endLabel = context.ILGenerator.DefineLabel();

            var branchOpcode = EmitBranchCondition(instruction.GetOperand(0), context);
            context.ILGenerator.Emit(branchOpcode, trueLabel);

            EmitValue(instruction.GetOperand(2), context);
            context.ILGenerator.Emit(OpCodes.Br, endLabel);

            context.ILGenerator.MarkLabel(trueLabel);
            EmitValue(instruction.GetOperand(1), context);

            context.ILGenerator.MarkLabel(endLabel);
        }

        private readonly record struct SwitchCase(int ConstantValue, LLVMValueRef Value, LLVMBasicBlockRef Destination)
            : IComparable<SwitchCase>
        {
            public int CompareTo(SwitchCase other)
            {
                return ConstantValue.CompareTo(other.ConstantValue);
            }
        }

        private void EmitSwitch(LLVMValueRef instruction, FunctionCompilationContext context)
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

        private void EmitValue(
            LLVMValueRef valueRef,
            FunctionCompilationContext context,
            bool forceLdloca = false)
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
                                throw new NotImplementedException($"Constant aggregate zero value not implemented: {valueRef}");
                        }
                        break;

                    case LLVMValueKind.LLVMConstantDataVectorValueKind:
                    case LLVMValueKind.LLVMConstantFPValueKind:
                    case LLVMValueKind.LLVMConstantIntValueKind:
                        EmitLoadConstant(context.ILGenerator, valueRef.TypeOf, valueRef, context.CompilationContext);
                        break;

                    case LLVMValueKind.LLVMConstantExprValueKind:
                        switch (valueRef.ConstOpcode)
                        {
                            case LLVMOpcode.LLVMGetElementPtr:
                                EmitValue(valueRef.GetOperand(0), context);
                                context.ILGenerator.Emit(OpCodes.Ldc_I4, GetElementPtrConst(valueRef, context));
                                context.ILGenerator.Emit(OpCodes.Conv_U);
                                context.ILGenerator.Emit(OpCodes.Add);
                                break;

                            default:
                                throw new NotImplementedException($"Const opcode {valueRef.ConstOpcode} not implemented: {valueRef}");
                        }
                        break;

                    case LLVMValueKind.LLVMConstantPointerNullValueKind:
                        context.ILGenerator.Emit(OpCodes.Ldc_I4_0);
                        context.ILGenerator.Emit(OpCodes.Conv_U);
                        break;

                    case LLVMValueKind.LLVMFunctionValueKind:
                        context.ILGenerator.Emit(OpCodes.Ldftn, GetOrCreateMethod(valueRef, context.CompilationContext));
                        break;

                    case LLVMValueKind.LLVMGlobalVariableValueKind:
                        var staticField = context.CompilationContext.Globals[valueRef];
                        context.ILGenerator.Emit(OpCodes.Ldsflda, staticField);
                        break;

                    case LLVMValueKind.LLVMPoisonValueValueKind:
                    case LLVMValueKind.LLVMUndefValueValueKind:
                        switch (valueRef.TypeOf.Kind)
                        {
                            case LLVMTypeKind.LLVMPointerTypeKind:
                                context.ILGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ILGenerator.Emit(OpCodes.Conv_U);
                                break;

                            case LLVMTypeKind.LLVMVectorTypeKind:
                                context.ILGenerator.Emit(OpCodes.Call, GetMsilVectorType(valueRef.TypeOf, context.CompilationContext).GetProperty("Zero").GetGetMethod());
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    default:
                        throw new NotImplementedException($"Unsupported value kind {valueRef.Kind}: {valueRef}");
                }
            }
            else if (context.Locals.TryGetValue(valueRef, out var local))
            {
                // TODO: Don't do this Vector128 hack
                //if (forceLdloca || (local.LocalType.IsValueType && !local.LocalType.IsPrimitive && !local.LocalType.IsPointer && !local.LocalType.Name.Contains("Vector128")))
                if (valueRef.IsAAllocaInst != null)
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
            else if (context.CanPushToStack(valueRef))
            {
                CompileInstructionValue(valueRef, context);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected value: {valueRef}");
            }
        }

        private void EmitLoad(
            LLVMValueRef instruction,
            FunctionCompilationContext context)
        {
            var valueRef = instruction.GetOperand(0);

            EmitValue(valueRef, context);

            // TODO: Find better way to do this.
            //if (valueRef.IsAAllocaInst != null)
            //{

            //    // We'll have emitted ldloc instruction, no need to dereference pointer.
            //    return;
            //}

            switch (instruction.TypeOf.Kind)
            {
                case LLVMTypeKind.LLVMFloatTypeKind:
                    context.ILGenerator.Emit(OpCodes.Ldind_R4);
                    break;

                case LLVMTypeKind.LLVMDoubleTypeKind:
                    context.ILGenerator.Emit(OpCodes.Ldind_R8);
                    break;

                case LLVMTypeKind.LLVMIntegerTypeKind:
                    switch (instruction.TypeOf.IntWidth)
                    {
                        case 8:
                            context.ILGenerator.Emit(OpCodes.Ldind_I1);
                            break;

                        case 16:
                            context.ILGenerator.Emit(OpCodes.Ldind_I2);
                            break;

                        case 32:
                            context.ILGenerator.Emit(OpCodes.Ldind_I4);
                            break;

                        case 64:
                            context.ILGenerator.Emit(OpCodes.Ldind_I8);
                            break;

                        default:
                            throw new NotImplementedException($"Int width {instruction.TypeOf.IntWidth} not implemented");
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

        private void EmitStoreResult(
            ILGenerator ilGenerator, 
            LLVMValueRef instruction, 
            FunctionCompilationContext context)
        {
            var local = ilGenerator.DeclareLocal(GetMsilType(instruction.TypeOf, context.CompilationContext));
            context.Locals.Add(instruction, local);
            EmitStloc(ilGenerator, instruction, instruction.TypeOf, context);
        }

        private void EmitStloc(
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
                EmitStoreIndirect(context.ILGenerator, type, context.CompilationContext);
            }
        }
    }
}
