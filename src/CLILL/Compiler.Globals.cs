using LLVMSharp.API;
using LLVMSharp.API.Types;
using LLVMSharp.API.Types.Composite.SequentialTypes;
using LLVMSharp.API.Values.Constants;
using LLVMSharp.API.Values.Constants.ConstantDataSequentials;
using LLVMSharp.API.Values.Constants.GlobalValues.GlobalObjects;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CLILL
{
    partial class Compiler
    {
        private static void CompileGlobals(CompilationContext context)
        {
            var constructor = context.TypeBuilder.DefineTypeInitializer();
            var ilGenerator = constructor.GetILGenerator();

            var global = (GlobalVariable)context.LLVMModule.GetFirstGlobal();
            while (global != null)
            {
                // TODO
                var fieldType = GetMsilType(global.Type);
                var globalField = context.TypeBuilder.DefineField(
                    global.Name.Replace(".", string.Empty), 
                    fieldType, 
                    FieldAttributes.Private | FieldAttributes.Static);

                switch (global.Operands[0])
                {
                    case ConstantDataArray v:
                        var arrayType = (ArrayType)v.Type;
                        ilGenerator.Emit(OpCodes.Ldc_I4, (int) arrayType.Length);
                        ilGenerator.Emit(OpCodes.Newarr, GetMsilType(arrayType.ElementType));
                        for (var i = 0u; i < arrayType.Length; i++)
                        {
                            ilGenerator.Emit(OpCodes.Dup);
                            ilGenerator.Emit(OpCodes.Ldc_I4, i);
                            EmitLoadConstantAndStoreElement(ilGenerator, v.GetElementAsConstant(i));
                        }
                        break;
                }

                ilGenerator.Emit(OpCodes.Stsfld, globalField);
                ilGenerator.Emit(OpCodes.Ret);

                context.Globals.Add(global, globalField);

                global = global.NextGlobal;
            }
        }

        private static void EmitLoadConstantAndStoreElement(ILGenerator ilGenerator, Value value)
        {
            switch (value)
            {
                case ConstantInt c:
                    var integerType = (IntegerType)c.Type;
                    switch (integerType.BitWidth)
                    {
                        case 8:
                            ilGenerator.Emit(OpCodes.Ldc_I4, (int)c.SExtValue);
                            ilGenerator.Emit(OpCodes.Stelem_I1);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
