using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace IR2IL.Runtime;

public static unsafe class LLVMIntrinsics
{
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