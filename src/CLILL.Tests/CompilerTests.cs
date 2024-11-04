using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;
using Xunit;

namespace CLILL.Tests
{
    public class CompilerTests
    {
        [Theory]
        [InlineData("class")]
        [InlineData("control_flow")]
        [InlineData("fibonacci")]
        [InlineData("sum_array.O0")]
        [InlineData("sum_array.O3")]
        [InlineData("switch")]
        public void CanCompileLlvmIrToMsil(string testName)
        {
            var fullPath = Path.Combine(Environment.CurrentDirectory, "TestPrograms", "C", testName + ".ll");

            int managedResult, llvmResult;

            using (var compiler = new Compiler())
            using (var source = LLVMSourceCode.FromFile(fullPath))
            {
                var outputPath = $"{testName}.exe";
                compiler.Compile(source, outputPath);

                managedResult = GetManagedResult(outputPath);
            }

            using (var source = LLVMSourceCode.FromFile(fullPath))
            {
                llvmResult = ExecuteIRModule(source);
            }

            Assert.Equal(llvmResult, managedResult);
        }

        private static int GetManagedResult(string outputPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
            };

            startInfo.ArgumentList.Add(outputPath);

            using var process = new Process
            {
                StartInfo = startInfo,
            };

            if (!process.Start())
            {
                throw new InvalidOperationException();
            }

            process.WaitForExit();

            return process.ExitCode;
        }

        private static unsafe int ExecuteIRModule(LLVMSourceCode source)
        {
            LLVM.InitializeNativeTarget();
            LLVM.InitializeNativeAsmPrinter();

            using var context = LLVMContextRef.Create();

            /*using*/ var module = context.ParseIR(source.MemoryBuffer);

            var jitBuilder = LLVM.OrcCreateLLJITBuilder();

            LLVMOrcOpaqueLLJIT* jit;
            CheckError(LLVM.OrcCreateLLJIT(&jit, jitBuilder));

            var function = module.FirstFunction;
            while (function != null)
            {
                var name = function.Name;

                function = function.NextFunction;
            }

            var threadSafeContext = LLVM.OrcCreateNewThreadSafeContext();
            var threadSafeModule = LLVM.OrcCreateNewThreadSafeModule(module, threadSafeContext);
            var jitDylib = LLVM.OrcLLJITGetMainJITDylib(jit);

            var pool = LLVM.OrcExecutionSessionGetSymbolStringPool(LLVM.OrcLLJITGetExecutionSession(jit));

            using var printf = new MarshaledString("printf");
            var printfEntry = LLVM.OrcLLJITMangleAndIntern(jit, printf);

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            //var msvcrt = NativeLibrary.Load("msvcr120.dll");
            //var printfAddress = NativeLibrary.GetExport(msvcrt, "printf");

            //delegate* unmanaged[Cdecl]<byte*, IntPtr, int> printfAddress = &PrintFormat;

            var printfAddress = Marshal.GetFunctionPointerForDelegate<PrintFormatDelegate>(PrintFormat2);

            var symbolPair = new LLVMOrcCSymbolMapPair
            {
                Name = printfEntry,
                Sym = new LLVMJITEvaluatedSymbol
                {
                    Address = (ulong)printfAddress,
                    Flags = new LLVMJITSymbolFlags { }
                }
            };

            var materializationUnit = LLVM.OrcAbsoluteSymbols(&symbolPair, 1);

            CheckError(LLVM.OrcJITDylibDefine(jitDylib, materializationUnit));

            CheckError(LLVM.OrcLLJITAddLLVMIRModule(jit, jitDylib, threadSafeModule));

            ulong result;
            using var marshaledName = new MarshaledString("main");
            CheckError(LLVM.OrcLLJITLookup(jit, &result, marshaledName.Value));

            delegate* unmanaged[Cdecl]<int> mainFunction = (delegate* unmanaged[Cdecl]<int>)result;

            var result2 = mainFunction();

            //LLVM.OrcDisposeThreadSafeModule(threadSafeModule);
            LLVM.OrcDisposeThreadSafeContext(threadSafeContext);
            LLVM.OrcDisposeLLJIT(jit);
            //LLVM.OrcDisposeLLJITBuilder(jitBuilder);

            //LLVM.DisposeMessage(errorMessage);

            //LLVM.DisposeTargetMachine(targetMachine);

            return result2;
        }

        private static unsafe void CheckError(LLVMOpaqueError* error)
        {
            if (error != null)
            {
                var errorMessage = SpanExtensions.AsString(LLVM.GetErrorMessage(error));
                throw new InvalidOperationException(errorMessage);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate int PrintFormatDelegate(byte* format, ArgIterator argsAddresses);

        private static unsafe int PrintFormat2(byte* format, ArgIterator argsAddresses)
        {
            var formatString = Marshal.PtrToStringUTF8((IntPtr)format);
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe int PrintFormat(byte* format, IntPtr argsAddresses)
        {
            var formatString = Marshal.PtrToStringUTF8((IntPtr)format);
            return 0;
        }
    }
}
