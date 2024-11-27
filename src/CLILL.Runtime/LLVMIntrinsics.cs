using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CLILL.Runtime;

public static unsafe class LLVMIntrinsics
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assume(bool cond) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FAbsF32(float val) => MathF.Abs(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FMulAddF32(float a, float b, float c) => MathF.FusedMultiplyAdd(a, b, c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double FMulAddF64(double a, double b, double c) => Math.FusedMultiplyAdd(a, b, c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<float> FMulAddV2F32(Vector64<float> a, Vector64<float> b, Vector64<float> c) => Vector64.FusedMultiplyAdd(a, b, c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<double> FMulAddV2F64(Vector128<double> a, Vector128<double> b, Vector128<double> c) => Vector128.FusedMultiplyAdd(a, b, c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LifetimeEndP0(long size, void* ptr)
    {
        // TODO: Don't emit this method at all
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LifetimeStartP0(long size, void* ptr)
    {
        // TODO: Don't emit this method at all
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemCpyI64(void* dest, void* src, long length, bool isVolatile) => NativeMemory.Copy(src, dest, (nuint)length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemSetI64(void* dest, byte val, long length, bool isVolatile)
    {
        // TODO: isVolatile
        NativeMemory.Fill(dest, (nuint)length, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SMaxI32(int val1, int val2) => Math.Max(val1, val2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> SMaxV4I32(Vector128<int> val1, Vector128<int> val2) => Vector128.Max(val1, val2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrtF32(float val) => MathF.Sqrt(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double SqrtF64(double val) => Math.Sqrt(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StackRestore(void* ptr)
    {
        // TODO: We don't really support this. Ideally emit a warning.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* StackSave()
    {
        // TODO: We don't really support this. Ideally emit a warning.
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int USubSatI32(int a, int b) => Math.Max(a - b, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int VectorReduceAddV4I32(Vector128<int> vector) => Vector128.Sum(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int VectorReduceMulV4I32(Vector128<int> vector)
    {
        if (Sse41.IsSupported)
        {
            // Multiply pairs of elements
            var temp = Sse41.MultiplyLow(vector, Sse41.Shuffle(vector, 0b_10_11_00_01));
            temp = Sse41.MultiplyLow(temp, Sse41.Shuffle(temp, 0b_01_00_11_10));

            // Extract the scalar result
            return Sse41.Extract(temp, 0);
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int VectorReduceSMaxV4I32(Vector128<int> vector)
    {
        if (Sse41.IsSupported)
        {
            // Perform horizontal max operations.
            var temp = Sse41.Max(vector, Sse41.Shuffle(vector, 0b_10_11_00_01));
            temp = Sse41.Max(temp, Sse41.Shuffle(temp, 0b_01_00_11_10));
            temp = Sse41.Max(temp, Sse41.Shuffle(temp, 0b_00_01_10_11));

            // Extract the maximum value.
            return Sse2.ConvertToInt32(temp);
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }
}