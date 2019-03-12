using LLVMSharp.API;
using System;

namespace CLILL
{
    public sealed class LLVMSourceCode : IDisposable
    {
        internal readonly MemoryBuffer MemoryBuffer;

        private LLVMSourceCode(MemoryBuffer memoryBuffer)
        {
            MemoryBuffer = memoryBuffer;

            // Can't actually Dispose, or even finalize, MemoryBuffer 
            // because of a bug (?) in LLVM.
            GC.SuppressFinalize(memoryBuffer);
        }

        public static LLVMSourceCode FromFile(string filePath)
        {
            return new LLVMSourceCode(MemoryBuffer.CreateMemoryBufferWithContentsOfFile(filePath));
        }

        public void Dispose()
        {
            
        }
    }
}
