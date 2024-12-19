using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LLVMSharp.Interop;

namespace IR2IL;

internal static partial class LLVMExtensions
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

    public static string? GetParameterName(this LLVMValueRef function, int parameterIndex)
    {
        foreach (var basicBlock in function.BasicBlocks)
        {
            foreach (var instruction in basicBlock.GetInstructions())
            {
                if (instruction.InstructionOpcode == LLVMOpcode.LLVMCall)
                {
                    var operands = instruction.GetOperands().ToList();

                    switch (operands[^1].Name)
                    {
                        case "llvm.dbg.declare":
                        case "llvm.dbg.value":
                            var diLocalVariable = instruction.GetOperand(1);
                            var diLocalVariableArg = diLocalVariable.GetDILocalVariableArg();
                            if (diLocalVariableArg == parameterIndex)
                            {
                                return diLocalVariable.GetDILocalVariableName();
                            }
                            break;
                    }
                }
            }
        }

        return null;
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

    public static unsafe bool AllocaHasConstantNumElements(this LLVMValueRef instruction)
    {
        if (instruction.Kind != LLVMValueKind.LLVMInstructionValueKind
            || instruction.InstructionOpcode != LLVMOpcode.LLVMAlloca)
        {
            throw new ArgumentException("Not an alloca instruction", nameof(instruction));
        }

        return instruction.GetOperand(0).Kind == LLVMValueKind.LLVMConstantIntValueKind;
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

            // An `undef` mask index is returned from `GetMaskValue` as -1.
            // That creates problems with our indexing so we replace those
            // values with 0.
            if (result[i] == -1)
            {
                result[i] = 0;
            }
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
            LLVMOpcode.LLVMAlloca => true,
            LLVMOpcode.LLVMCall => value.IsAIntrinsicInst != null, // TODO: Not every intrinsic has no side effects.
            LLVMOpcode.LLVMFCmp => value.GetOperand(0).HasNoSideEffects() && value.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMFDiv => value.GetOperand(0).HasNoSideEffects() && value.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMFreeze => value.GetOperand(0).HasNoSideEffects(),
            LLVMOpcode.LLVMGetElementPtr => value.GetOperands().All(x => x.HasNoSideEffects()),
            LLVMOpcode.LLVMICmp => value.GetOperand(0).HasNoSideEffects() && value.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMInsertElement => value.GetOperands().All(x => x.HasNoSideEffects()),
            LLVMOpcode.LLVMLoad => true, // TODO: Is this correct?
            LLVMOpcode.LLVMPHI => true, // Because we load it from a local that is guaranteed not to change in the current block
            LLVMOpcode.LLVMSDiv => value.GetOperand(0).HasNoSideEffects() && value.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMSExt => value.GetOperand(0).HasNoSideEffects(),
            LLVMOpcode.LLVMShuffleVector => value.GetOperands().All(x => x.HasNoSideEffects()),
            LLVMOpcode.LLVMUDiv => value.GetOperand(0).HasNoSideEffects() && value.GetOperand(1).HasNoSideEffects(),
            LLVMOpcode.LLVMZExt => value.GetOperand(0).HasNoSideEffects(),
            _ => false,
        };
    }

    public static unsafe LLVMMetadataRef AsMetadata(this LLVMValueRef value)
    {
        if (value.Kind != LLVMValueKind.LLVMMetadataAsValueValueKind)
        {
            throw new InvalidOperationException();
        }

        return LLVM.ValueAsMetadata(value);
    }

    public static unsafe LLVMMetadataRef GetMetadata(this LLVMValueRef value, string name)
    {
        using var marshaledName = new MarshaledString(name);
        var kindID = LLVM.GetMDKindID(marshaledName.Value, (uint)marshaledName.Length);
        return value.GetMetadata(kindID).AsMetadata();
    }

    public static unsafe LLVMMetadataKind GetMetadataKind(this LLVMMetadataRef metadata)
        => (LLVMMetadataKind)LLVM.GetMetadataKind(metadata);

    public static unsafe LLVMMetadataRef GetDebugLoc(this LLVMValueRef instruction)
    {
        if (instruction.Kind != LLVMValueKind.LLVMInstructionValueKind)
        {
            throw new InvalidOperationException();
        }

        return (LLVMMetadataRef)LLVM.InstructionGetDebugLoc(instruction);
    }

    public static unsafe uint GetDILocationLine(this LLVMMetadataRef metadata)
    {
        if (metadata.GetMetadataKind() != LLVMMetadataKind.LLVMDILocationMetadataKind)
        {
            throw new InvalidOperationException();
        }

        return LLVM.DILocationGetLine(metadata);
    }

    public static unsafe uint GetDILocationColumn(this LLVMMetadataRef metadata)
    {
        if (metadata.GetMetadataKind() != LLVMMetadataKind.LLVMDILocationMetadataKind)
        {
            throw new InvalidOperationException();
        }

        return LLVM.DILocationGetColumn(metadata);
    }

    public static unsafe LLVMMetadataRef GetDILocationScope(this LLVMMetadataRef metadata)
    {
        if (metadata.GetMetadataKind() != LLVMMetadataKind.LLVMDILocationMetadataKind)
        {
            throw new InvalidOperationException();
        }

        return LLVM.DILocationGetScope(metadata);
    }

    public static unsafe LLVMMetadataRef GetDIScopeFile(this LLVMMetadataRef metadata)
    {
        var metadataKind = metadata.GetMetadataKind();
        switch (metadataKind)
        {
            case LLVMMetadataKind.LLVMDISubprogramMetadataKind:
            case LLVMMetadataKind.LLVMDILexicalBlockMetadataKind:
                break;

            default:
                throw new InvalidOperationException();
        }

        return LLVM.DIScopeGetFile(metadata);
    }

    public static unsafe string GetDIFileDirectory(this LLVMMetadataRef metadata)
    {
        if (metadata.GetMetadataKind() != LLVMMetadataKind.LLVMDIFileMetadataKind)
        {
            throw new InvalidOperationException();
        }

        uint len;
        var directoryBytes = LLVM.DIFileGetDirectory(metadata, &len);

        return SpanExtensions.AsString(directoryBytes);
    }

    public static unsafe string GetDIFileFilename(this LLVMMetadataRef metadata)
    {
        if (metadata.GetMetadataKind() != LLVMMetadataKind.LLVMDIFileMetadataKind)
        {
            throw new InvalidOperationException();
        }

        uint len;
        var filenameBytes = LLVM.DIFileGetFilename(metadata, &len);

        return SpanExtensions.AsString(filenameBytes);
    }

    public static unsafe string GetDILocalVariableName(this LLVMValueRef value)
    {
        if (value.Kind != LLVMValueKind.LLVMMetadataAsValueValueKind)
        {
            throw new InvalidOperationException();
        }

        if (value.AsMetadata().GetMetadataKind() != LLVMMetadataKind.LLVMDILocalVariableMetadataKind)
        {
            throw new InvalidOperationException();
        }

        return value.MDNodeOperands[1].GetMDString(out _);
    }

    [GeneratedRegex("arg: (\\d+),")]
    private static partial Regex ArgRegex();

    public static unsafe int? GetDILocalVariableArg(this LLVMValueRef value)
    {
        if (value.Kind != LLVMValueKind.LLVMMetadataAsValueValueKind)
        {
            throw new InvalidOperationException();
        }

        if (value.AsMetadata().GetMetadataKind() != LLVMMetadataKind.LLVMDILocalVariableMetadataKind)
        {
            throw new InvalidOperationException();
        }

        // There's no LLVM-C API for this, so we do it the hard way.

        var diLocalVariableString = value.ToString();
        var diLocalVariableArgMatch = ArgRegex().Match(diLocalVariableString);
        if (diLocalVariableArgMatch.Success && int.TryParse(diLocalVariableArgMatch.Groups[1].Value, out var diLocalVariableArg) && diLocalVariableArg > 0)
        {
            return diLocalVariableArg;
        }
        else
        {
            return null;
        }
    }
}