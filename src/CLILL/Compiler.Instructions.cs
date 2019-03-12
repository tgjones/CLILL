using LLVMSharp.API;
using LLVMSharp.API.Values;
using LLVMSharp.API.Values.Constants;
using LLVMSharp.API.Values.Constants.GlobalValues.GlobalObjects;
using LLVMSharp.API.Values.Instructions;
using LLVMSharp.API.Values.Instructions.Binary;
using LLVMSharp.API.Values.Instructions.Cmp;
using LLVMSharp.API.Values.Instructions.Terminator;
using LLVMSharp.API.Values.Instructions.Unary;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CLILL
{
    partial class Compiler
    {
        private static void CompileInstruction(
            Instruction instruction,
            FunctionCompilationContext context)
        {
            var ilGenerator = context.ILGenerator;

            switch (instruction)
            {
                case AllocaInst _:
                    {
                        var operand = instruction.Operands[0];
                        var local = ilGenerator.DeclareLocal(GetMsilType(operand.Type));
                        context.Locals.Add(instruction, local);
                        break;
                    }

                case StoreInst _:
                    {
                        EmitLoad(ilGenerator, instruction.Operands[0], context);
                        EmitStloc(ilGenerator, instruction.Operands[1], context);
                        break;
                    }

                case BranchInst i:
                    {
                        if (i.IsConditional)
                        {
                            EmitLoad(ilGenerator, i.Condition, context);
                            ilGenerator.Emit(OpCodes.Brtrue, context.GetOrCreateLabel(instruction.Operands[2]));
                            ilGenerator.Emit(OpCodes.Br, context.GetOrCreateLabel(instruction.Operands[1]));
                        }
                        else
                        {
                            var label = context.GetOrCreateLabel(instruction.Operands[0]);
                            ilGenerator.Emit(OpCodes.Br, label);
                        }
                        break;
                    }

                case LoadInst _:
                    {
                        EmitLoad(ilGenerator, instruction.Operands[0], context);
                        EmitStoreResult(ilGenerator, instruction, context);
                        break;
                    }

                case ICmpInst i:
                    {
                        EmitLoad(ilGenerator, instruction.Operands[0], context);
                        EmitLoad(ilGenerator, instruction.Operands[1], context);
                        switch (i.ICmpPredicate)
                        {
                            case IntPredicate.SLE:
                                ilGenerator.Emit(OpCodes.Cgt);
                                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                ilGenerator.Emit(OpCodes.Ceq);
                                break;

                            case IntPredicate.SLT:
                                ilGenerator.Emit(OpCodes.Clt);
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        EmitStoreResult(ilGenerator, instruction, context);
                        break;
                    }

                case Add _:
                    {
                        EmitLoad(ilGenerator, instruction.Operands[0], context);
                        EmitLoad(ilGenerator, instruction.Operands[1], context);
                        ilGenerator.Emit(OpCodes.Add);
                        EmitStoreResult(ilGenerator, instruction, context);
                        break;
                    }

                case CallInst _:
                    {
                        // TODO: This is totally hardcoded to printf call.
                        var fieldRef = instruction.Operands[0].Operands[0];
                        ilGenerator.Emit(OpCodes.Ldsfld, context.CompilationContext.Globals[fieldRef]);
                        EmitLoad(ilGenerator, instruction.Operands[1], context);
                        ilGenerator.EmitCall(
                            OpCodes.Call,
                            GetOrCreateMethod((Function)instruction.Operands[2], context.CompilationContext), 
                            new[] { typeof(int) });
                        ilGenerator.Emit(OpCodes.Pop);
                        break;
                    }

                case ReturnInst _:
                    {
                        if (instruction.Operands.Count > 0)
                        {
                            var returnOperand = instruction.Operands[0];
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
            Value valueRef,
            FunctionCompilationContext context)
        {
            if (valueRef is Constant c)
            {
                if (valueRef is ConstantInt i)
                {
                    ilGenerator.Emit(OpCodes.Ldc_I4, (int)i.SExtValue);
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
            Value instruction, 
            FunctionCompilationContext context)
        {
            var local = ilGenerator.DeclareLocal(GetMsilType(instruction.Type));
            context.Locals.Add(instruction, local);
            EmitStloc(ilGenerator, instruction, context);
        }

        private static void EmitStloc(
            ILGenerator ilGenerator, 
            Value valueRef, 
            FunctionCompilationContext context)
        {
            var local = context.Locals[valueRef];
            ilGenerator.Emit(OpCodes.Stloc, local);
        }
    }
}
