using System.Runtime.InteropServices;

namespace IR2IL.Runtime;

[StructLayout(LayoutKind.Sequential, Size = Vector1024.Size)]
public readonly struct Vector1024<T>
    where T : unmanaged
{
    public static unsafe int Count => Vector1024.Size / sizeof(T);

    public static Vector32<T> Zero => default;

    public T this[int index] => this.GetElementUnsafe(index);
}