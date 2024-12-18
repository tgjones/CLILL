using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace IR2IL.Runtime;

public static class Vector16
{
    internal const int Size = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector64<float> ConvertToSingle(Vector16<byte> vector)
    {
        return Vector64.Create(
            (float)vector.GetElementUnsafe(0),
            (float)vector.GetElementUnsafe(1));
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
