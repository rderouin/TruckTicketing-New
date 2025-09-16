using System;

namespace SE.Shared.Common.Extensions;

public static class ObjectExtensions
{
    public static bool IsNullOrDefault<T>(this T instance)
    {
        return instance == null || instance.Equals(default(T));
    }

    public static T With<T>(this T instance, Action<T> action)
    {
        action(instance);
        return instance;
    }
}
