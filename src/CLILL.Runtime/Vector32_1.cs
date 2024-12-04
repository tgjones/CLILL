using System.Runtime.InteropServices;

namespace CLILL.Runtime;

[StructLayout(LayoutKind.Sequential, Size = Vector32.Size)]
public readonly struct Vector32<T>
    where T : unmanaged
{
    public static unsafe int Count => Vector32.Size / sizeof(T);

    public static Vector32<T> Zero => default;
}