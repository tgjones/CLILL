using System;
using System.Reflection.Emit;
using LLVMSharp.Interop;

namespace CLILL
{
    partial class Compiler
    {
        private static void CompileInstruction(
            LLVMValueRef instruction,
            FunctionCompilationContext context)
        {
            var ilGenerator = context.ILGenerator;

            switch (instruction.InstructionOpcode)
            {
                case LLVMOpcode.LLVMAlloca:
                    {
                        var operand = instruction.GetOperand(0);
                        var local = ilGenerator.DeclareLocal(GetMsilType(operand.TypeOf));
                        context.Locals.Add(instruction, local);
                        break;
                    }

                case LLVMOpcode.LLVMStore:
                    {
                        EmitLoad(ilGenerator, instruction.GetOperand(0), context);
                        EmitStloc(ilGenerator, instruction.GetOperand(1), context);
                        break;
                    }

                case LLVMOpcode.LLVMBr:
                    {
                        if (instruction.IsConditional)
                        {
                            EmitLoad(ilGenerator, instruction.Condition, context);
                            ilGenerator.Emit(OpCodes.Brtrue, context.GetOrCreateLabel(instruction.GetOperand(2)));
                            ilGenerator.Emit(OpCodes.Br, context.GetOrCreateLabel(instruction.GetOperand(1)));
                        }
                        else
                        {
                            var label = context.GetOrCreateLabel(instruction.GetOperand(0));
                            ilGenerator.Emit(OpCodes.Br, label);
                        }
                        break;
                    }

                case LLVMOpcode.LLVMLoad:
                    {
                        EmitLoad(ilGenerator, instruction.GetOperand(0), context);
                        EmitStoreResult(ilGenerator, instruction, context);
                        break;
                    }

                case LLVMOpcode.LLVMICmp:
                    {
                        EmitLoad(ilGenerator, instruction.GetOperand(0), context);
                        EmitLoad(ilGenerator, instruction.GetOperand(1), context);
                        switch (instruction.ICmpPredicate)
                        {
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
                        EmitStoreResult(ilGenerator, instruction, context);
                        break;
                    }

                case LLVMOpcode.LLVMAdd:
                    {
                        EmitLoad(ilGenerator, instruction.GetOperand(0), context);
                        EmitLoad(ilGenerator, instruction.GetOperand(1), context);
                        ilGenerator.Emit(OpCodes.Add);
                        EmitStoreResult(ilGenerator, instruction, context);
                        break;
                    }

                case LLVMOpcode.LLVMCall:
                    {
                        // TODO: This is totally hardcoded to printf call.
                        var fieldRef = instruction.GetOperand(0);
                        var staticField = context.CompilationContext.Globals[fieldRef];
                        ilGenerator.Emit(OpCodes.Ldsfld, staticField);
                        ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        ilGenerator.Emit(OpCodes.Ldelema, staticField.FieldType.GetElementType());
                        ilGenerator.Emit(OpCodes.Conv_U);
                        EmitLoad(ilGenerator, instruction.GetOperand(1), context);
                        ilGenerator.EmitCall(
                            OpCodes.Call,
                            GetOrCreateMethod(instruction.GetOperand(2), context.CompilationContext), 
                            new[] { typeof(int) });
                        ilGenerator.Emit(OpCodes.Pop);
                        break;
                    }

                case LLVMOpcode.LLVMRet:
                    {
                        if (instruction.OperandCount > 0)
                        {
                            var returnOperand = instruction.GetOperand(0);
                            EmitLoad(ilGenerator, returnOperand, context);
                        }
                        ilGenerator.Emit(OpCodes.Ret);
                        break;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        private static void EmitLoad(
            ILGenerator ilGenerator,
            LLVMValueRef valueRef,
            FunctionCompilationContext context)
        {
            if (valueRef.IsConstant)
            {
                var constantInt = valueRef.IsAConstantInt;
                if (constantInt.Handle != IntPtr.Zero)
                {
                    ilGenerator.Emit(OpCodes.Ldc_I4, (int)constantInt.ConstIntSExt);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                var local = context.Locals[valueRef];
                ilGenerator.Emit(OpCodes.Ldloc, local);
            }
        }

        private static void EmitStoreResult(
            ILGenerator ilGenerator, 
            LLVMValueRef instruction, 
            FunctionCompilationContext context)
        {
            var local = ilGenerator.DeclareLocal(GetMsilType(instruction.TypeOf));
            context.Locals.Add(instruction, local);
            EmitStloc(ilGenerator, instruction, context);
        }

        private static void EmitStloc(
            ILGenerator ilGenerator,
            LLVMValueRef valueRef, 
            FunctionCompilationContext context)
        {
            var local = context.Locals[valueRef];
            ilGenerator.Emit(OpCodes.Stloc, local);
        }
    }
}
