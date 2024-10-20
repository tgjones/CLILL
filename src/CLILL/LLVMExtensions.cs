using System;
using System.Collections.Generic;
using LLVMSharp.Interop;

namespace CLILL;

internal static class LLVMExtensions
{
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

    public static bool HasNoSideEffects(this LLVMValueRef instruction)
    {
        switch (instruction.Kind)
        {
            case LLVMValueKind.LLVMConstantIntValueKind:
                return true;
        }

        if (instruction.Kind != LLVMValueKind.LLVMInstructionValueKind)
        {
            throw new ArgumentException("Not an instruction", nameof(instruction));
        }

        return instruction.InstructionOpcode switch
        {
            LLVMOpcode.LLVMAdd => instruction.GetOperand(0).HasNoSideEffects() && instruction.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMICmp => instruction.GetOperand(0).HasNoSideEffects() && instruction.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMLoad => true, // TODO: Is this correct?
            _ => false,
        };
    }
}
