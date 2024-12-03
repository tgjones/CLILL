﻿using System.Runtime.InteropServices;

namespace CLILL.Runtime;

[StructLayout(LayoutKind.Sequential, Size = Vector16.Size)]
public readonly struct Vector16<T>
    where T : unmanaged
{
    public static unsafe int Count => Vector16.Size / sizeof(T);

    public static Vector16<T> Zero => default;
}