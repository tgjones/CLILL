namespace CLILL.Runtime;

public readonly struct Vector16<T>
    where T : unmanaged
{
    private readonly ushort _00;

    public static unsafe int Count => Vector16.Size / sizeof(T);

    public static Vector16<T> Zero => default;
}