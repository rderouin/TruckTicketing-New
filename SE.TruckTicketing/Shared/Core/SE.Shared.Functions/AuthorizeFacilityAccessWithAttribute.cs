using System;

namespace SE.Shared.Functions;

[AttributeUsage(AttributeTargets.Method)]
public class AuthorizeFacilityAccessWithAttribute : Attribute
{
    public AuthorizeFacilityAccessWithAttribute(Type type)
    {
        Type = type;
    }

    public Type Type { get; }
}
