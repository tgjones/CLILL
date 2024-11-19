using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp.Interop;

namespace CLILL;

internal static class LLVMExtensions
{
    public static IEnumerable<LLVMValueRef> GetGlobals(this LLVMModuleRef module)
    {
        var global = module.FirstGlobal;
        while (global != null)
        {
            yield return global;
            global = global.NextGlobal;
        }
    }

    public static IEnumerable<LLVMValueRef> GetInstructions(this LLVMBasicBlockRef basicBlock)
    {
        var instruction = basicBlock.FirstInstruction;
        while (instruction != null)
        {
            yield return instruction;
            instruction = instruction.NextInstruction;
        }
    }

    public static unsafe IEnumerable<LLVMValueRef> GetUses(this LLVMValueRef instruction)
    {
        if (instruction.Kind != LLVMValueKind.LLVMInstructionValueKind)
        {
            throw new ArgumentException("Not an instruction", nameof(instruction));
        }

        var use = instruction.FirstUse;
        while (use != null)
        {
            LLVMValueRef user;
            unsafe
            {
                user = (LLVMValueRef)LLVM.GetUser(use);
            }
            yield return user;
            unsafe
            {
                use = LLVM.GetNextUse(use);
            }
        }
    }

    public static unsafe IEnumerable<LLVMValueRef> GetOperands(this LLVMValueRef value)
    {
        if (value.Kind != LLVMValueKind.LLVMInstructionValueKind
            && value.Kind != LLVMValueKind.LLVMConstantExprValueKind)
        {
            throw new ArgumentException("Not an instruction", nameof(value));
        }

        for (var i = 0u; i < value.OperandCount; i++)
        {
            yield return value.GetOperand(i);
        }
    }

    public static unsafe LLVMTypeRef GetAllocatedType(this LLVMValueRef instruction)
    {
        if (instruction.Kind != LLVMValueKind.LLVMInstructionValueKind
            || instruction.InstructionOpcode != LLVMOpcode.LLVMAlloca)
        {
            throw new ArgumentException("Not an alloca instruction", nameof(instruction));
        }

        return (LLVMTypeRef)LLVM.GetAllocatedType(instruction);
    }

    public static unsafe int[] GetShuffleVectorMaskValues(this LLVMValueRef instruction)
    {
        if (instruction.Kind != LLVMValueKind.LLVMInstructionValueKind
            || instruction.InstructionOpcode != LLVMOpcode.LLVMShuffleVector)
        {
            throw new ArgumentException("Not a shufflevector instruction", nameof(instruction));
        }

        var result = new int[LLVM.GetNumMaskElements(instruction)];

        for (var i = 0u; i < result.Length; i++)
        {
            result[i] = LLVM.GetMaskValue(instruction, i);
        }

        return result;
    }

    public static LLVMValueRef GetIncomingValueForBlock(this LLVMValueRef instruction, LLVMBasicBlockRef basicBlock)
    {
        if (instruction.Kind != LLVMValueKind.LLVMInstructionValueKind
            || instruction.InstructionOpcode != LLVMOpcode.LLVMPHI)
        {
            throw new ArgumentException("Not a phi instruction", nameof(instruction));
        }

        for (var i = 0u; i < instruction.IncomingCount; i++)
        {
            var b = instruction.GetIncomingBlock(i);

            if (b == basicBlock)
            {
                return instruction.GetIncomingValue(i);
            }
        }

        throw new InvalidOperationException();
    }

    public static bool ContainsPhiNodes(this LLVMBasicBlockRef basicBlock) =>
        basicBlock.FirstInstruction.InstructionOpcode == LLVMOpcode.LLVMPHI;

    public static bool HasNoSideEffects(this LLVMValueRef value)
    {
        switch (value.Kind)
        {
            case LLVMValueKind.LLVMArgumentValueKind:
            case LLVMValueKind.LLVMConstantDataVectorValueKind:
            case LLVMValueKind.LLVMConstantIntValueKind:
            case LLVMValueKind.LLVMConstantPointerNullValueKind:
            case LLVMValueKind.LLVMPoisonValueValueKind:
                return true;

            case LLVMValueKind.LLVMGlobalVariableValueKind:
                return false;
        }

        if (value.Kind != LLVMValueKind.LLVMInstructionValueKind)
        {
            throw new ArgumentException($"Unexpected value kind {value.Kind}: {value}", nameof(value));
        }

        return value.InstructionOpcode switch
        {
            LLVMOpcode.LLVMAdd => value.GetOperand(0).HasNoSideEffects() && value.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMGetElementPtr => value.GetOperands().All(x => x.HasNoSideEffects()),
            LLVMOpcode.LLVMICmp => value.GetOperand(0).HasNoSideEffects() && value.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMInsertElement => value.GetOperands().All(x => x.HasNoSideEffects()),
            LLVMOpcode.LLVMLoad => true, // TODO: Is this correct?
            LLVMOpcode.LLVMPHI => true, // Because we load it from a local that is guaranteed not to change in the current block
            LLVMOpcode.LLVMSExt => value.GetOperand(0).HasNoSideEffects(),
            LLVMOpcode.LLVMShuffleVector => value.GetOperands().All(x => x.HasNoSideEffects()),
            LLVMOpcode.LLVMZExt => value.GetOperand(0).HasNoSideEffects(),
            _ => false,
        };
    }
}
