using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CLILL.Tests
{
    [TestClass]
    public class CompilerTests
    {
        private static IEnumerable<object[]> TestFiles(IEnumerable<string> testFiles)
        {
            var optimizationLevels = new string[]
            {
                "O0",
                "O3",
            };

            return from testFile in testFiles
                   from optimizationLevel in optimizationLevels
                   select new object[] { testFile, optimizationLevel };
        }

        private static IEnumerable<object[]> TestDataArbitrary()
        {
            return TestFiles(Directory
                .GetFiles(Path.Combine("TestPrograms", "arbitrary"), "*.*", SearchOption.AllDirectories));
        }

        private static IEnumerable<object[]> TestDataCTestSuite()
        {
            return TestFiles(Directory
                .GetFiles(Path.Combine("TestPrograms", "c-testsuite"), "*.c", SearchOption.AllDirectories)
                .Where(x =>
                {
                    switch (Path.GetFileNameWithoutExtension(x))
                    {
                        // These tests are not supported on Windows because they use `extern int printf(...)`
                        // which isn't compatible with Microsoft's C runtime.
                        case "00210":
                        case "00211":
                        case "00213":
                        case "00214":
                        case "00215":
                        case "00217":
                        case "00218":
                            return false;

                        // These tests are not supported on Windows because they produce different
                        // output than the expected output.
                        case "00212":
                        case "00216":
                            return false;

                        default:
                            return true;
                    }
                }));
        }

        public static string TestDataDisplayName(MethodInfo methodInfo, object[] values)
        {
            return $"{methodInfo.Name}({Path.GetFileName((string)values[0])}, {values[1]})";
        }

        [TestMethod]
        [DynamicData(nameof(TestDataArbitrary), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestDataDisplayName))]
        public void Arbitrary(string testName, string optimizationLevel)
        {
            CompileAndExecuteManaged(
                testName,
                optimizationLevel,
                out var managedExitCode,
                out var managedStandardOutput,
                out var managedStandardError);

            // Compile to executable binary.
            var binaryPath = GetFullTestName(testName, optimizationLevel) + "_native.exe";
            RunClang([GetSourceFilePath(testName), "-o", binaryPath, $"-{optimizationLevel}"]);

            RunProgram(
                binaryPath,
                [],
                out var llvmExitCode,
                out var llvmStandardOutput,
                out var llvmStandardError);

            Assert.AreEqual(llvmStandardError, managedStandardError);
            Assert.AreEqual(llvmStandardOutput, managedStandardOutput);
            Assert.AreEqual(llvmExitCode, managedExitCode);

            Console.WriteLine($"ExitCode: {managedExitCode}");
            Console.WriteLine($"Stdout: {managedStandardOutput}");
        }

        [TestMethod]
        [DynamicData(nameof(TestDataCTestSuite), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestDataDisplayName))]
        public void CTestSuite(string testName, string optimizationLevel)
        {
            CompileAndExecuteManaged(
                testName, 
                optimizationLevel,
                out var managedExitCode,
                out var managedStandardOutput,
                out var managedStandardError);

            var nativeStandardOutput = File
                .ReadAllText(GetSourceFilePath(testName) + ".expected")
                .ReplaceLineEndings();

            Assert.AreEqual("", managedStandardError);
            Assert.AreEqual(nativeStandardOutput, managedStandardOutput);
            Assert.AreEqual(0, managedExitCode);

            Console.WriteLine($"Stdout: {managedStandardOutput}");
        }

        //[TestMethod]
        //[DataRow("O0")]
        //[DataRow("O3")]
        //public void BenchmarkMandelbrot(string optimizationLevel)
        //{
        //    var sourceFilePath = Path.Combine(Environment.CurrentDirectory, "TestPrograms", "mandelbrot", "benchmarks.c");

        //    var fullTestName = $"{Path.GetFileNameWithoutExtension(sourceFilePath)}_{optimizationLevel}";

        //    var irPath = fullTestName + ".ll";
        //    var binaryPath = fullTestName + "_native.exe";

        //    // Compile to LLVM IR.
        //    RunClang([sourceFilePath, "-o", irPath, "-emit-llvm", "-S", $"-{optimizationLevel}"]);

        //    // Compile to executable binary.
        //    RunClang([sourceFilePath, "-o", binaryPath, $"-{optimizationLevel}"]);

        //    var outputPath = $"{fullTestName}.exe";

        //    using (var compiler = new Compiler())
        //    using (var source = LLVMSourceCode.FromFile(irPath))
        //    {
        //        compiler.Compile(source, outputPath);
        //    }

        //    var stopwatch = Stopwatch.StartNew();

        //    RunProgram(
        //        "dotnet",
        //        [outputPath],
        //        out var managedExitCode,
        //        out var managedStandardOutput,
        //        out var managedStandardError);

        //    Console.WriteLine($"Managed: {stopwatch.Elapsed}");

        //    stopwatch.Restart();

        //    RunProgram(
        //        binaryPath,
        //        [],
        //        out var llvmExitCode,
        //        out var llvmStandardOutput,
        //        out var llvmStandardError);

        //    Console.WriteLine($"Native:  {stopwatch.Elapsed}");

        //    Assert.AreEqual(llvmExitCode, managedExitCode, managedStandardError);

        //    Console.WriteLine($"Managed Stdout: {managedStandardOutput}");
        //    Console.WriteLine($"Native  Stdout: {llvmStandardOutput}");
        //}

        private static string GetFullTestName(string testName, string optimizationLevel) => $"{testName}_{optimizationLevel}";

        private static string GetSourceFilePath(string testName) => Path.Combine(Environment.CurrentDirectory, testName);

        private static void CompileAndExecuteManaged(
            string testName, 
            string optimizationLevel,
            out int managedExitCode,
            out string managedStandardOutput,
            out string managedStandardError)
        {
            var fullTestName = GetFullTestName(testName, optimizationLevel);

            var irPath = fullTestName + ".ll";

            // Compile to LLVM IR.
            RunClang([GetSourceFilePath(testName), "-o", irPath, "-emit-llvm", "-S", $"-{optimizationLevel}"]);

            var outputPath = $"{fullTestName}.exe";
            Compiler.Compile(irPath, outputPath);

            RunProgram(
                "dotnet",
                [outputPath],
                out managedExitCode,
                out managedStandardOutput,
                out managedStandardError);
        }

        private static void RunProgram(
            string executablePath, 
            string[] arguments, 
            out int exitCode, 
            out string standardOutput,
            out string standardError)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process
            {
                StartInfo = startInfo,
            };

            if (!process.Start())
            {
                throw new InvalidOperationException();
            }

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            exitCode = process.ExitCode;

            standardOutput = standardOutputTask.Result;
            standardError = standardErrorTask.Result;
        }

        private static void RunClang(string[] arguments)
        {
            RunProgram(
                @"..\..\..\..\..\lib\llvm\win-x64\clang.exe",
                arguments,
                out var exitCode,
                out _,
                out var standardError);

            if (exitCode != 0)
            {
                throw new InvalidOperationException(standardError);
            }
        }
    }
}
