using System;

namespace SE.Shared.Domain.Processors;

public class EntityProcessorForAttribute : Attribute
{
    public EntityProcessorForAttribute(string entityType)
    {
        EntityType = entityType;
    }

    public string EntityType { get; }
}
