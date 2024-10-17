using System;
using LLVMSharp.Interop;

namespace CLILL
{
    partial class Compiler
    {
        private static Type GetMsilType(LLVMTypeRef typeRef)
        {
            switch (typeRef.Kind)
            {
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    var intTypeWidth = typeRef.IntWidth;
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

                case LLVMTypeKind.LLVMVoidTypeKind:
                    return typeof(void);

                case LLVMTypeKind.LLVMPointerTypeKind:
                    return typeof(IntPtr);

                case LLVMTypeKind.LLVMArrayTypeKind:
                    return GetMsilType(typeRef.ElementType).MakeArrayType();

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
