using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace IR2IL.Runtime;

public static class VectorUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<float> ConvertV2I8ToV2F32(Vector16<byte> vector) => Vector16.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<int> ConvertV2I16ToV2I32(Vector32<short> vector) => Vector64.Create(vector[0], vector[1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<long> ConvertV2I16ToV2I64(Vector32<short> vector) => Vector128.Create(vector[0], vector[1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<float> ConvertV2I32ToV2F32(Vector64<int> vector) => Vector64.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<double> ConvertV2I32ToV2F64(Vector64<int> vector)
    {
        var (lower, upper) = Vector64.Widen(vector);
        return Vector128.ConvertToDouble(Vector128.Create(lower, upper));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<short> ConvertV2I64ToV2I16(Vector128<long> vector) => Vector32.Create((short)vector[0], (short)vector[1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<int> ConvertV2I64ToV2I32(Vector128<long> vector) => Vector64.Narrow(vector.GetLower(), vector.GetUpper());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> ConvertV4F32ToV4F64(Vector128<float> vector)
    {
        var (lower, upper) = Vector128.Widen(vector);
        return Vector256.Create(lower, upper);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> ConvertV4F64ToV4F32(Vector256<double> vector) => Vector128.Narrow(vector.GetLower(), vector.GetUpper());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> ConvertV4I16ToV4I32(Vector64<short> vector)
    {
        var (lower, upper) = Vector64.Widen(vector);
        return Vector128.Create(lower, upper);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> ConvertV4I32ToV4F32(Vector128<int> vector) => Vector128.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<int> ConvertV4I64ToV4I32(Vector256<long> vector) => Vector128.Narrow(vector.GetLower(), vector.GetUpper());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ConvertV8I32ToV8F32(Vector256<int> vector) => Vector256.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<float> ConvertV16I32ToV16F32(Vector512<int> vector) => Vector512.ConvertToSingle(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<sbyte> SignedRemainderV2I8(Vector16<sbyte> left, Vector16<sbyte> right)
    {
        // TODO: Optimize this.
        return Vector16.Create(
            (sbyte)(left[0] % right[0]),
            (sbyte)(left[1] % right[1]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<short> SignedRemainderV2I16(Vector32<short> left, Vector32<short> right)
    {
        // TODO: Optimize this.
        return Vector32.Create(
            (short)(left[0] % right[0]),
            (short)(left[1] % right[1]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<short> SignedRemainderV4I16(Vector64<short> left, Vector64<short> right)
    {
        // TODO: Optimize this.
        return Vector64.Create(
            (short)(left[0] % right[0]),
            (short)(left[1] % right[1]),
            (short)(left[2] % right[2]),
            (short)(left[3] % right[3]));
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
    public static Vector16<sbyte> UnsignedRemainderV2I8(Vector16<sbyte> left, Vector16<sbyte> right)
    {
        // TODO: Optimize this.

        var leftUnsigned = left.AsByte();
        var rightUnsigned = right.AsByte();

        var signedResult = Vector16.Create(
            (byte)(leftUnsigned[0] % rightUnsigned[0]),
            (byte)(leftUnsigned[1] % rightUnsigned[1]));

        return signedResult.AsSByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<short> UnsignedRemainderV2I16(Vector32<short> left, Vector32<int> right)
    {
        // TODO: Optimize this.

        var leftUnsigned = left.AsUInt16();
        var rightUnsigned = right.AsUInt16();

        var signedResult = Vector32.Create(
            (ushort)(leftUnsigned[0] % rightUnsigned[0]),
            (ushort)(leftUnsigned[1] % rightUnsigned[1]));

        return signedResult.AsInt16();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector64<short> UnsignedRemainderV4I16(Vector64<short> left, Vector64<int> right)
    {
        // TODO: Optimize this.

        var leftUnsigned = left.AsUInt16();
        var rightUnsigned = right.AsUInt16();

        var signedResult = Vector64.Create(
            (ushort)(leftUnsigned[0] % rightUnsigned[0]),
            (ushort)(leftUnsigned[1] % rightUnsigned[1]),
            (ushort)(leftUnsigned[2] % rightUnsigned[2]),
            (ushort)(leftUnsigned[3] % rightUnsigned[3]));

        return signedResult.AsInt16();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> UnsignedRemainderV8I16(Vector128<short> left, Vector128<short> right)
    {
        // TODO: Optimize this.

        var leftUnsigned = left.AsUInt16();
        var rightUnsigned = right.AsUInt16();

        var signedResult = Vector128.Create(
            (ushort)(leftUnsigned[0] % rightUnsigned[0]),
            (ushort)(leftUnsigned[1] % rightUnsigned[1]),
            (ushort)(leftUnsigned[2] % rightUnsigned[2]),
            (ushort)(leftUnsigned[3] % rightUnsigned[3]),
            (ushort)(leftUnsigned[4] % rightUnsigned[4]),
            (ushort)(leftUnsigned[5] % rightUnsigned[5]),
            (ushort)(leftUnsigned[6] % rightUnsigned[6]),
            (ushort)(leftUnsigned[7] % rightUnsigned[7]));

        return signedResult.AsInt16();
    }
}