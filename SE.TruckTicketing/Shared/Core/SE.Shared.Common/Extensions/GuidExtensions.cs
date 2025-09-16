using System;

namespace SE.Shared.Common.Extensions;

public static class GuidExtensions
{
    public static string ToReferenceId(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        return $"{bytes[3]:X2}{bytes[2]:X2}";
    }
}
