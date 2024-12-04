using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CLILL.Runtime;

public static class Vector32
{
    internal const int Size = 4;

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
