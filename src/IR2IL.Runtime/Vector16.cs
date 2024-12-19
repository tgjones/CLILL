using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace IR2IL.Runtime;

public static class Vector16
{
    internal const int Size = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<T> Add<T>(Vector16<T> left, Vector16<T> right)
        where T : unmanaged, INumber<T>
    {
        Unsafe.SkipInit(out Vector16<T> result);

        for (var index = 0; index < Vector16<T>.Count; index++)
        {
            T value = left.GetElementUnsafe(index) + right.GetElementUnsafe(index);
            result.SetElementUnsafe(index, value);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<TTo> As<TFrom, TTo>(this Vector16<TFrom> vector)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        return Unsafe.BitCast<Vector16<TFrom>, Vector16<TTo>>(vector);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<byte> AsByte<T>(this Vector16<T> vector)
        where T : unmanaged
    {
        return vector.As<T, byte>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<sbyte> AsSByte<T>(this Vector16<T> vector)
        where T : unmanaged
    {
        return vector.As<T, sbyte>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector64<float> ConvertToSingle(Vector16<byte> vector)
    {
        return Vector64.Create(
            (float)vector.GetElementUnsafe(0),
            (float)vector.GetElementUnsafe(1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<byte> Create(byte e0, byte e1)
    {
        Unsafe.SkipInit(out Vector16<byte> result);
        result.SetElementUnsafe(0, e0);
        result.SetElementUnsafe(1, e1);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<sbyte> Create(sbyte e0, sbyte e1)
    {
        Unsafe.SkipInit(out Vector16<sbyte> result);
        result.SetElementUnsafe(0, e0);
        result.SetElementUnsafe(1, e1);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<T> Equals<T>(Vector16<T> left, Vector16<T> right)
        where T : unmanaged, INumber<T>
    {
        Unsafe.SkipInit(out Vector16<T> result);

        for (int index = 0; index < Vector16<T>.Count; index++)
        {
            T value = left.GetElementUnsafe(index) == right.GetElementUnsafe(index)
                ? Scalar<T>.AllBitsSet
                : default!;
            result.SetElementUnsafe(index, value);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetElement<T>(this Vector16<T> vector, int index)
        where T : unmanaged
    {
        if ((uint)index >= (uint)Vector16<T>.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return vector.GetElementUnsafe(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector16<T> WithElement<T>(this Vector16<T> vector, int index, T value)
        where T : unmanaged
    {
        Vector16<T> result = vector;
        result.SetElementUnsafe(index, value);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T GetElementUnsafe<T>(in this Vector16<T> vector, int index)
        where T : unmanaged
    {
        Debug.Assert((index >= 0) && (index < Vector16<T>.Count));
        ref T address = ref Unsafe.As<Vector16<T>, T>(ref Unsafe.AsRef(in vector));
        return Unsafe.Add(ref address, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetElementUnsafe<T>(in this Vector16<T> vector, int index, T value)
        where T : unmanaged
    {
        Debug.Assert((index >= 0) && (index < Vector16<T>.Count));
        ref T address = ref Unsafe.As<Vector16<T>, T>(ref Unsafe.AsRef(in vector));
        Unsafe.Add(ref address, index) = value;
    }
}
