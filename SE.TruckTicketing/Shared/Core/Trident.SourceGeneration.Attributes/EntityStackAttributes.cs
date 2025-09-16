using System;

namespace Trident.SourceGeneration.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateManagerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateProviderAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateRepositoryAttribute : Attribute
    {
    }
}
