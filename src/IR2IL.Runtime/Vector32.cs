using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace IR2IL.Runtime;

public static class Vector32
{
    internal const int Size = 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<T> Add<T>(Vector32<T> left, Vector32<T> right)
        where T : unmanaged, INumber<T>
    {
        Unsafe.SkipInit(out Vector32<T> result);

        for (var index = 0; index < Vector32<T>.Count; index++)
        {
            T value = left.GetElementUnsafe(index) + right.GetElementUnsafe(index);
            result.SetElementUnsafe(index, value);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<TTo> As<TFrom, TTo>(this Vector32<TFrom> vector)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        return Unsafe.BitCast<Vector32<TFrom>, Vector32<TTo>>(vector);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<short> AsInt16<T>(this Vector32<T> vector)
        where T : unmanaged
    {
        return vector.As<T, short>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<ushort> AsUInt16<T>(this Vector32<T> vector)
        where T : unmanaged
    {
        return vector.As<T, ushort>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<short> Create(short e0, short e1)
    {
        Unsafe.SkipInit(out Vector32<short> result);
        result.SetElementUnsafe(0, e0);
        result.SetElementUnsafe(1, e1);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<ushort> Create(ushort e0, ushort e1)
    {
        Unsafe.SkipInit(out Vector32<ushort> result);
        result.SetElementUnsafe(0, e0);
        result.SetElementUnsafe(1, e1);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetElement<T>(this Vector32<T> vector, int index)
        where T : unmanaged
    {
        if ((uint)index >= (uint)Vector32<T>.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return vector.GetElementUnsafe(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<T> Multiply<T>(Vector32<T> left, Vector32<T> right)
        where T : unmanaged, INumber<T>
    {
        Unsafe.SkipInit(out Vector32<T> result);

        for (var index = 0; index < Vector32<T>.Count; index++)
        {
            T value = left.GetElementUnsafe(index) * right.GetElementUnsafe(index);
            result.SetElementUnsafe(index, value);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<short> ShiftLeft(Vector32<short> value, int shiftCount)
    {
        return Create((short)(value[0] << shiftCount), (short)(value[1] << shiftCount));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<T> Subtract<T>(Vector32<T> left, Vector32<T> right)
        where T : unmanaged, INumber<T>
    {
        Unsafe.SkipInit(out Vector32<T> result);

        for (var index = 0; index < Vector32<T>.Count; index++)
        {
            T value = left.GetElementUnsafe(index) - right.GetElementUnsafe(index);
            result.SetElementUnsafe(index, value);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector32<T> WithElement<T>(this Vector32<T> vector, int index, T value)
        where T : unmanaged
    {
        Vector32<T> result = vector;
        result.SetElementUnsafe(index, value);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T GetElementUnsafe<T>(in this Vector32<T> vector, int index)
        where T : unmanaged
    {
        Debug.Assert((index >= 0) && (index < Vector32<T>.Count));
        ref T address = ref Unsafe.As<Vector32<T>, T>(ref Unsafe.AsRef(in vector));
        return Unsafe.Add(ref address, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetElementUnsafe<T>(in this Vector32<T> vector, int index, T value)
        where T : unmanaged
    {
        Debug.Assert((index >= 0) && (index < Vector32<T>.Count));
        ref T address = ref Unsafe.As<Vector32<T>, T>(ref Unsafe.AsRef(in vector));
        Unsafe.Add(ref address, index) = value;
    }
}
