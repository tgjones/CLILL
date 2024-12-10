using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace IR2IL.Runtime;

public static class VectorUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<float> ConvertV2I8ToV2F32(Vector16<byte> vector) => Vector16.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<float> ConvertV2I32ToV2F32(Vector64<int> vector) => Vector64.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<double> ConvertV2I32ToV2F64(Vector64<int> vector)
    {
        var (lower, upper) = Vector64.Widen(vector);
        return Vector128.ConvertToDouble(Vector128.Create(lower, upper));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> ConvertV4F32ToV4F64(Vector128<float> vector)
    {
        var (lower, upper) = Vector128.Widen(vector);
        return Vector256.Create(lower, upper);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> ConvertV4F64ToV4F32(Vector256<double> vector) => Vector128.Narrow(vector.GetLower(), vector.GetUpper());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> ConvertV4I32ToV4F32(Vector128<int> vector) => Vector128.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ConvertV8I32ToV8F32(Vector256<int> vector) => Vector256.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<float> ConvertV16I32ToV16F32(Vector512<int> vector) => Vector512.ConvertToSingle(vector);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> SignedRemainderV4I32(Vector128<int> left, Vector128<int> right)
    {
        // TODO: Optimize this.
        return Vector128.Create(
            left[0] % right[0],
            left[1] % right[1],
            left[2] % right[2],
            left[3] % right[3]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> UnsignedRemainderV4I32(Vector128<int> left, Vector128<int> right)
    {
        // TODO: Optimize this.

        var leftUnsigned = left.AsUInt32();
        var rightUnsigned = right.AsUInt32();

        var signedResult = Vector128.Create(
            leftUnsigned[0] % rightUnsigned[0],
            leftUnsigned[1] % rightUnsigned[1],
            leftUnsigned[2] % rightUnsigned[2],
            leftUnsigned[3] % rightUnsigned[3]);

        return signedResult.AsInt32();
    }
}