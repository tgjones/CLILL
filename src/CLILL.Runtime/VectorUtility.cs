using System.Runtime.Intrinsics;

namespace CLILL.Runtime;

public static class VectorUtility
{
    public static Vector64<int> Narrow(Vector128<long> vector)
    {
        var lower = vector.GetLower();
        var upper = vector.GetUpper();

        return Vector64.Narrow(lower, upper);
    }

    public static Vector128<int> Narrow(Vector256<long> vector)
    {
        var lower = vector.GetLower();
        var upper = vector.GetUpper();

        return Vector128.Narrow(lower, upper);
    }
}