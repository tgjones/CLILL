// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace IR2IL.Runtime;

// Copied from https://github.com/dotnet/runtime/blob/208b974f93c0ee35a171eba374ca36aa9a78e930/src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/Scalar.cs
// because isn't a public API.
internal static class Scalar<T>
{
    public static T AllBitsSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)byte.MaxValue;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)BitConverter.Int64BitsToDouble(-1);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)-1;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)-1;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)-1;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)(-1);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)nuint.MaxValue;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)-1;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)BitConverter.Int32BitsToSingle(-1);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)ushort.MaxValue;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)uint.MaxValue;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.MaxValue;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
