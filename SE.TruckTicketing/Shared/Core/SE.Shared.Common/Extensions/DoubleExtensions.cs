using System;

namespace SE.Shared.Common.Extensions;

public static class DoubleExtensions
{
    public static bool Eq(this double a, double b, double value)
    {
        return a == b || Math.Abs(a - b) < value;
    }
}
