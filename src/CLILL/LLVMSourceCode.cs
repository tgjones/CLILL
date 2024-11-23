using System;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;

namespace CLILL;

public sealed unsafe class LLVMSourceCode : IDisposable
{
    public readonly LLVMMemoryBufferRef MemoryBuffer;

    private LLVMSourceCode(LLVMMemoryBufferRef memoryBuffer)
    {
        MemoryBuffer = memoryBuffer;
    }

    public static LLVMSourceCode FromFile(string filePath)
    {
        using var marshaledFilePath = new MarshaledString(filePath);

        LLVMMemoryBufferRef memoryBuffer;
        sbyte* messagePtr = null;

        var result = LLVM.CreateMemoryBufferWithContentsOfFile(
            marshaledFilePath,
            (LLVMOpaqueMemoryBuffer**)&memoryBuffer,
            &messagePtr);

        if (result != 0)
        {
            var message = SpanExtensions.AsString(messagePtr);
            throw new ExternalException(message);
        }

        return new LLVMSourceCode(memoryBuffer);
    }

    public void Dispose()
    {
        // TODO: It crashes when we do this. Why?
        //LLVM.DisposeMemoryBuffer(MemoryBuffer);
    }
}