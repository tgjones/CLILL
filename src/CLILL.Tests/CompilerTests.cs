using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CLILL.Tests;

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

    public static string TestDataDisplayName(MethodInfo methodInfo, object[] values)
    {
        return $"{methodInfo.Name}({Path.GetFileName((string)values[0])}, {values[1]})";
    }

    private static IEnumerable<object[]> TestDataArbitrary() => TestFiles(
        Directory.GetFiles(Path.Combine("TestPrograms", "arbitrary"), "*.c", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles(Path.Combine("TestPrograms", "arbitrary"), "*.cpp", SearchOption.AllDirectories)));

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

    private static IEnumerable<object[]> TestDataCTestSuite() => TestFiles(
        Directory
        .GetFiles(Path.Combine("TestPrograms", "c-testsuite"), "*.c", SearchOption.AllDirectories)
        .Where(x => Path.GetFileNameWithoutExtension(x) switch
        {
            // These tests are not supported on Windows because they use `extern int printf(...)`
            // which isn't compatible with Microsoft's C runtime.
            "00210" or "00211" or "00213" or "00214" or "00215" or "00217" or "00218" => false,

            // These tests are not supported on Windows because they produce different
            // output than the expected output.
            "00212" or "00216" => false,

            _ => true,
        }));

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

    private static IEnumerable<object[]> TestDataBenchmarks() => TestFiles(
        Directory
        .GetFiles(Path.Combine("TestPrograms", "benchmarks"), "*.c", SearchOption.AllDirectories));

    [TestMethod]
    [DynamicData(nameof(TestDataBenchmarks), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestDataDisplayName))]
    public void Benchmark(string testName, string optimizationLevel)
    {
        var managedExePath = CompileManaged(testName, optimizationLevel);

        // Compile to executable binary.
        var binaryPath = GetFullTestName(testName, optimizationLevel) + "_native.exe";
        RunClang([GetSourceFilePath(testName), "-o", binaryPath, $"-{optimizationLevel}"]);

        var stopwatch = Stopwatch.StartNew();

        RunProgram(
            "dotnet",
            [managedExePath],
            out var managedExitCode,
            out var managedStandardOutput,
            out var managedStandardError);

        Console.WriteLine($"Managed: {stopwatch.Elapsed}");

        stopwatch.Restart();

        RunProgram(
            binaryPath,
            [],
            out var llvmExitCode,
            out var llvmStandardOutput,
            out _);

        Console.WriteLine($"Native:  {stopwatch.Elapsed}");

        Assert.AreEqual(llvmExitCode, managedExitCode, managedStandardError);
        Assert.AreEqual(llvmStandardOutput, managedStandardOutput);

        Console.WriteLine($"Stdout: {managedStandardOutput}");
    }

    private static string GetFullTestName(string testName, string optimizationLevel) => $"{testName}_{optimizationLevel}";

    private static string GetSourceFilePath(string testName) => Path.Combine(Environment.CurrentDirectory, testName);

    private static void CompileAndExecuteManaged(
        string testName,
        string optimizationLevel,
        out int managedExitCode,
        out string managedStandardOutput,
        out string managedStandardError)
    {
        var outputPath = CompileManaged(
            testName,
            optimizationLevel);

        ExecuteManaged(
            outputPath,
            out managedExitCode,
            out managedStandardOutput,
            out managedStandardError);
    }

    private static string CompileManaged(string testName, string optimizationLevel)
    {
        var fullTestName = GetFullTestName(testName, optimizationLevel);

        var irPath = fullTestName + ".ll";

        // Compile to LLVM IR.
        RunClang([GetSourceFilePath(testName), "-g", "-o", irPath, "-emit-llvm", "-S", $"-{optimizationLevel}"]);

        var outputPath = $"{fullTestName}.exe";
        Compiler.Compile(irPath, outputPath);

        return outputPath;
    }

    private static void ExecuteManaged(
        string managedExePath,
        out int managedExitCode,
        out string managedStandardOutput,
        out string managedStandardError)
    {
        RunProgram(
            "dotnet",
            [managedExePath],
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
            "clang.exe",
            arguments,
            out var exitCode,
            out var standardOutput,
            out var standardError);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(standardOutput + Environment.NewLine + standardError);
        }
    }
}