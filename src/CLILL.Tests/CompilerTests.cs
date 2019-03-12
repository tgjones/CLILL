using System;
using System.IO;
using Xunit;

namespace CLILL.Tests
{
    public class CompilerTests
    {
        [Theory]
        [InlineData("fibonacci")]
        public void CanCompileLlvmIrToMsil(string testName)
        {
            var fullPath = Path.Combine(Environment.CurrentDirectory, "TestPrograms", "C", testName + ".ll");

            using (var compiler = new Compiler())
            using (var source = LLVMSourceCode.FromFile(fullPath))
            {
                compiler.Compile(source, testName);
            }

            // TODO: Run program and compare to original C program output.
        }
    }
}
