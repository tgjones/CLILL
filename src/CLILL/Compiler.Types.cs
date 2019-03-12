using LLVMSharp.API.Types;
using LLVMSharp.API.Types.Composite.SequentialTypes;
using System;

namespace CLILL
{
    partial class Compiler
    {
        private static Type GetMsilType(LLVMSharp.API.Type typeRef)
        {
            switch (typeRef)
            {
                case IntegerType t:
                    var intTypeWidth = t.BitWidth;
                    switch (intTypeWidth)
                    {
                        case 1:
                        case 32:
                            return typeof(int);

                        case 8:
                            return typeof(byte);

                        default:
                            throw new NotImplementedException();
                    }

                case VoidType _:
                    return typeof(void);

                case PointerType t:
                    var elementType = GetMsilType(t.ElementType);
                    return elementType.IsArray
                        ? elementType
                        : elementType.MakeArrayType();

                case ArrayType t:
                    return GetMsilType(t.ElementType).MakeArrayType();

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
