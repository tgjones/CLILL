using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace CLILL.Runtime;

public static class VectorUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<float> ConvertV2I8ToF32(Vector16<byte> vector) => Vector16.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<float> ConvertV2I32ToF32(Vector64<int> vector) => Vector64.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> ConvertV4I32ToF32(Vector128<int> vector) => Vector128.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ConvertV8I32ToF32(Vector256<int> vector) => Vector256.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<float> ConvertV16I32ToF32(Vector512<int> vector) => Vector512.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<int> Narrow(Vector128<long> vector)
    {
        var lower = vector.GetLower();
        var upper = vector.GetUpper();

        return Vector64.Narrow(lower, upper);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> Narrow(Vector256<long> vector)
    {
        var lower = vector.GetLower();
        var upper = vector.GetUpper();

        return Vector128.Narrow(lower, upper);
    }
}